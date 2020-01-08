using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using dotnetCampus.IO;
using dotnetCampus.Threading;
using CT = dotnetCampus.Configurations.Core.ConfigTracer;

namespace dotnetCampus.Configurations.Core
{
    /// <summary>
    /// 提供一个基于文件的配置管理器。
    /// </summary>
    public class FileConfigurationRepo : AsynchronousConfigurationRepo
    {
        /// <summary>
        /// 在文件改变后的延迟读取时间。
        /// </summary>
        public TimeSpan DelayReadTime { get; set; } = TimeSpan.FromMilliseconds(300);

        /// <summary>
        /// 延迟保存的时间
        /// </summary>
        public TimeSpan DelaySaveTime { get; set; } = TimeSpan.FromMilliseconds(300);

        /// <summary>
        /// 初始化使用 <paramref name="fileName"/> 作为配置文件的 <see cref="FileConfigurationRepo"/> 的新实例。
        /// </summary>
        /// <param name="fileName">配置文件的文件路径。</param>
        [Obsolete("请改用线程安全的 ConfigurationFactory 来创建实例。")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public FileConfigurationRepo(string fileName)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));

            var fullPath = Path.GetFullPath(fileName);
            _file = new FileInfo(fullPath);
            _saveLoop = new PartialAwaitableRetry(SaveCoreAsync);

            // 监视文件改变。
            _watcher = new FileWatcher(_file);
            _watcher.Changed += OnFileChanged;
            _ = _watcher.WatchAsync();
            LoadFromFileTask = RequestReloadingFile();
        }

        /// <summary>
        /// 获取所有目前已经存储的 Key 的集合。
        /// </summary>
        protected override ICollection<string> GetKeys() => KeyValues.Keys;

        /// <summary>
        /// 获取指定 Key 的值，如果不存在，需要返回 null。
        /// </summary>
        /// <param name="key">指定项的 Key。</param>
        /// <returns>
        /// 执行项的 Key，如果不存在，则为 null / Task&lt;string&gt;.FromResult(null)"/>。
        /// </returns>
        protected override async Task<string?> ReadValueCoreAsync(string key)
        {
            await LoadFromFileTask.ConfigureAwait(false);
            var value = KeyValues.TryGetValue(key, out var v) ? v : null;
            CT.Debug($"{key} = {value ?? "null"}", "Get");
            return value;
        }

        /// <summary>
        /// 为指定的 Key 存储指定的值。
        /// </summary>
        /// <param name="key">指定项的 Key。</param>
        /// <param name="value">要存储的值。</param>
        protected override async Task WriteValueCoreAsync(string key, string value)
        {
            value = value ?? throw new ArgumentNullException(nameof(value));
            value = value.Replace(Environment.NewLine, "\n");
            await LoadFromFileTask.ConfigureAwait(false);
            CT.Debug($"{key} = {value}", "Set");
            KeyValues[key] = value;
        }

        /// <summary>
        /// 将为指定的 Key 清除。
        /// </summary>
        /// <param name="key">指定项的 Key。</param>
        protected override async Task RemoveValueCoreAsync(string key)
        {
            await LoadFromFileTask.ConfigureAwait(false);
            CT.Debug($"{key} = null", "Set");
            KeyValues.TryRemove(key, out _);
        }

        /// <summary>
        /// 在每次有键值改变后触发，在此处将配置进行持久化。
        /// </summary>
        protected override void OnChanged(AsynchronousConfigurationChangeContext context)
        {
            context = context ?? throw new ArgumentNullException(nameof(context));
            var task = SaveAsync(-1);
            context.TrackAsyncAction(task);
        }

        /// <summary>
        /// 保存到文件
        /// </summary>
        /// <returns></returns>
        public async Task SaveAsync(int tryCount = 10)
        {
            await LoadFromFileTask.ConfigureAwait(false);
            await _saveLoop.JoinAsync(tryCount);
        }

        /// <summary>
        /// 尝试重新加载此配置文件的外部修改（例如使用其他编辑器或其他客户端修改的部分）。
        /// <para>外部修改会自动同步到此配置中，但此同步不会立刻发生，所以如果你明确知道外部修改了文件后需要立刻重新加载外部修改，才需要调用此方法。</para>
        /// </summary>
        public async Task ReloadExternalChangesAsync()
        {
            // 如果之前正在读取文件，则等待文件读取完成。
            await LoadFromFileTask.ConfigureAwait(false);
            // 现在，强制要求重新读取文件。
            LoadFromFileTask = RequestReloadingFile();
            // 然后，等待重新读取完成。
            await LoadFromFileTask.ConfigureAwait(false);
        }

        /// <summary>
        /// 存储运行时保存的键值对。
        /// </summary>
        private ConcurrentDictionary<string, string> KeyValues { get; }
            = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 保存刚刚反序列化文件后的键值对备份。这份备份用于比较外部程序对配置文件的修改差量。
        /// </summary>
        private Dictionary<string, string> OriginalKeyValues { get; set; }
            = new Dictionary<string, string>();

        private Task LoadFromFileTask { get; set; }

        private readonly PartialAwaitableRetry _saveLoop;
        private readonly FileInfo _file;
        private bool _isPendingReread;
        private bool _isPendingRereadReentered;
        private DateTimeOffset _lastDeserializeTime = DateTimeOffset.MinValue;
        private readonly FileWatcher _watcher;
        private static string _splitString = ">";
        private static string _escapeString = "?";

        /// <summary>
        /// 要求重新读取外部文件，以更新内存中的缓存。
        /// 如果文件没有改变，则不会更新缓存。
        /// </summary>
        private Task RequestReloadingFile()
        {
            var lastWriteTime = new FileInfo(_file.FullName).LastWriteTimeUtc;
            if (lastWriteTime == _lastDeserializeTime && LoadFromFileTask != null)
            {
                return Task.FromResult<object?>(null);
            }
            LoadFromFileTask = Task.Run(async () => await DeserializeFile(_file).ConfigureAwait(false));
            return LoadFromFileTask;
        }

        /// <summary>
        /// 在配置文件改变的时候，重新读取文件。
        /// </summary>
        private async void OnFileChanged(object? sender, EventArgs e)
        {
            CT.Debug($"检测到文件被改变...", "File");
            var isPending = _isPendingReread;
            if (isPending)
            {
                // 如果发现已经在准备读取文件了，那么就告诉他又进来了一次，他可能还需要读。
                _isPendingRereadReentered = true;
                return;
            }

            _isPendingReread = true;

            try
            {
                do
                {
                    _isPendingRereadReentered = false;
                    // 等待时间为预期等待时间的 1/2，因为多数情况下，一次文件的改变会收到两次 Change 事件。
                    // 第一次是文件内容的写入，第二次是文件信息（如最近写入时间）的写入。
                    await Task.Delay((int) DelayReadTime.TotalMilliseconds / 2).ConfigureAwait(false);
                } while (_isPendingRereadReentered);

                // 如果之前正在读取文件，则等待文件读取完成。
                await LoadFromFileTask.ConfigureAwait(false);

                // 现在重新读取。
                // - ~~重新读取文件时不影响对键值对的访问，所以不要求其他地方等待 LoadFromFileTask。~~
                // - 但是，如果正在序列化和保存文件，为了避免写入时覆盖未读取完的键值对，需要等待读取完毕。
                // ！特别注意！：外部写完文件后配置立刻读，读不到新值；需要调用 ReloadExternalChangesAsync 方法强制加载外部修改；否则将等待自动更新修改。
                _ = RequestReloadingFile();
            }
            finally
            {
                _isPendingReread = false;
            }
        }

        private async Task<OperationResult> SaveCoreAsync(PartialRetryContext context)
        {
            context.StepCount = 10;
            await Task.Delay(DelaySaveTime).ConfigureAwait(false);
            await Serialize().ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// 反序列化文件
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private async Task DeserializeFile(FileInfo file)
        {
            CT.Debug($"读取 {file.FullName}", "File");

            if (!File.Exists(file.FullName))
            {
                UpdateMemoryValuesFromExternalValues(KeyValues, OriginalKeyValues, new Dictionary<string, string>());
                OriginalKeyValues.Clear();
                return;
            }

            const int retryCount = 100;
            for (var i = 0; i < retryCount; i++)
            {
                try
                {
                    // 一次性读取完的性能最好
                    var str = File.ReadAllText(file.FullName);
                    Deserialize(str);
                    _lastDeserializeTime = DateTimeOffset.Now;
                    return;
                }
                catch (IOException)
                {
                    const int waitTime = 10;
                    // 读取配置文件出现异常，忽略所有异常
                    await Task.Delay(waitTime).ConfigureAwait(false);
                    // 通过测试发现在我的设备，写入平均时间是 6 毫秒，也就是如果存在多实例写入，也不会是 waitTime*retryCount 毫秒这么久，等待 waitTime*retryCount 毫秒也是可以接受最大的值
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    // 这里的代码因为从一个 IO 线程调进来，所以当调用方使用 await 等待，会使得这里抛出的异常回到 IO 线程，导致应用程序崩溃。
                    // 这里可能的异常有：
                    //   - UnauthorizedAccessException 在文件只读、文件实际上是一个文件夹、没有读权限或者平台不支持时引发。
                    const int waitTime = 10;
                    await Task.Delay(waitTime).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// 反序列化的核心实现，反序列化字符串
        /// </summary>
        /// <param name="str"></param>
        private void Deserialize(string str)
        {
            var keyValuePairList = str.Split('\n');
            var keyValue = new Dictionary<string, string>(StringComparer.Ordinal);
            string? key = null;
            var splitString = _splitString;

            foreach (var temp in keyValuePairList.Select(temp => temp.Trim()))
            {
                if (temp.StartsWith(splitString, StringComparison.Ordinal))
                {
                    // 分割，可以作为注释，这一行忽略
                    // 下一行必须是key
                    key = null;
                    continue;
                }

                var unescapedString = UnescapeString(temp);

                if (key == null)
                {
                    key = unescapedString;
                    
                    // 文件存在多个地方都记录相同的值
                    // 如果有多个地方记录相同的值，使用最后的值替换前面文件
                    if (keyValue.ContainsKey(key))
                    {
                        keyValue.Remove(key);
                    }
                }
                else
                {
                    if (keyValue.ContainsKey(key))
                    {
                        // key
                        // v1
                        // v2
                        // 返回 {"key","v1\nv2"}
                        keyValue[key] = keyValue[key] + "\n" + unescapedString;
                    }
                    else
                    {
                        keyValue.Add(key, unescapedString);
                    }
                }
            }

            UpdateMemoryValuesFromExternalValues(KeyValues, OriginalKeyValues, keyValue);

            OriginalKeyValues = keyValue;
        }

        /// <summary>
        /// 通过比较曾经读取过的文件键值对和当前读取的文件键值对比较差异，再根据差异修改内存中的键值对。
        /// 目前无法有效地比较新旧，于是这个差异几乎是决定性地影响到当前内存中的键值对。
        /// </summary>
        /// <param name="memoryKeyValues"></param>
        /// <param name="originalKeyValues"></param>
        /// <param name="externalKeyValues"></param>
        private static void UpdateMemoryValuesFromExternalValues(
            ConcurrentDictionary<string, string> memoryKeyValues,
            Dictionary<string, string> originalKeyValues,
            Dictionary<string, string> externalKeyValues)
        {
            var toBeAdded = new Dictionary<string, string>();
            var toBeRemoved = new Dictionary<string, string>();
            var toBeModified = new Dictionary<string, string>();

            foreach (var externalPair in externalKeyValues)
            {
                if (originalKeyValues.TryGetValue(externalPair.Key, out var originalValue))
                {
                    if (externalPair.Value != originalValue)
                    {
                        toBeModified[externalPair.Key] = externalPair.Value;
                    }
                }
                else
                {
                    toBeAdded[externalPair.Key] = externalPair.Value;
                }
            }

            foreach (var pair in originalKeyValues)
            {
                if (!externalKeyValues.ContainsKey(pair.Key))
                {
                    toBeRemoved[pair.Key] = pair.Value;
                }
            }

            // 以上代码是线程安全的。
            // 以下代码可能会因为多线程的问题导致数据不一致，但不会出现错误/异常。

            foreach (var pair in toBeAdded.Concat(toBeModified))
            {
                memoryKeyValues.AddOrUpdate(pair.Key, pair.Value, (key, existedValue) => pair.Value);
            }

            foreach (var pair in toBeRemoved)
            {
                memoryKeyValues.TryRemove(pair.Key, out _);
            }
        }

        /// <summary>
        /// <para/> ```fkv
        /// <para/> > 凡是 “>” 开头的行都是分隔符，如果后面有内容，将被忽略。
        /// <para/> > 于是这就可以写注释用以说明其含义
        /// <para/> > 多行的注释只需要打多行 “>” 即可
        /// <para/> key0
        /// <para/> value0
        /// <para/> >
        /// <para/> key1
        /// <para/> ?>value1
        /// <para/> ??value1
        /// <para/> > key 一定只有一行，在遇到下一个 “>” 之前，都是 value
        /// <para/> > key/value 存储时，每行一定不会 “>” 开头，如果遇到，则转义为 “?>”，原来的 “?” 转义为 “??”
        /// <para/> > 转义仅发生在行首。
        /// <para/> > 遇到空行，则依然识别为 key，或者 value 的一部分
        /// <para/> 
        /// <para/> value
        /// <para/> 
        /// <para/> > 以上 key 为空字符串，value 为 包含空行的 value（一般禁止写入空字符串作为 key）
        /// <para/> > 配置文件末尾不包含空行（因为这会识别为 value 的一部分）
        /// <para/> key0
        /// <para/> value9
        /// <para/> > 如果存在相同的 key，则处于文件后面的会覆盖文件前面的值。
        /// <para/> ```
        /// </summary>
        /// <returns></returns>
        private async Task Serialize()
        {
            // 重写尝试 10 次
            Exception? exception = null;
            const int retryWriteCount = 10;

            for (var i = 0; i < retryWriteCount; i++)
            {
                try
                {
                    // 如果文件夹不存在，则创建一个新的。
                    var directory = _file.Directory;
                    if (directory != null && !Directory.Exists(directory.FullName))
                    {
                        directory.Create();
                    }

                    try
                    {
                        if (_watcher != null)
                        {
                            await _watcher.StopAsync().ConfigureAwait(false);
                        }

                        // 在每次尝试写入到文件之前都将内存中的键值对序列化一次，避免过多的等待导致写入的数据过旧。
                        var text = Serialize(KeyValues);

                        // 将所有的配置写入文件。 
                        using (var fileStream = File.Open(_file.FullName, FileMode.Create, FileAccess.Write))
                        {
                            using var stream = new StreamWriter(fileStream, Encoding.UTF8);
                            await stream.WriteAsync(text).ConfigureAwait(false);
                        }

                        OriginalKeyValues = new Dictionary<string, string>(KeyValues);
                    }
                    finally
                    {
                        if (_watcher != null)
                        {
                            await _watcher.WatchAsync().ConfigureAwait(false);
                        }
                    }

                    return;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    // 如果这里吞掉了所有的异常，那么将没有任何途径可以得知为什么存储会失败。
                    exception = exception ?? ex;
                    Trace.WriteLine(exception);
                }

                // 在每次失败重试的时候，都需要等待指定的保存延迟时间。
                await Task.Delay(DelaySaveTime).ConfigureAwait(false);
            }

            // 记录保存失败时的异常，并抛出。
            if (exception != null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
        }

        /// <summary>
        /// 将键值对字典序列化为文本字符串。
        /// </summary>
        /// <param name="keyValue">要序列化的键值对字典。</param>
        /// <returns>序列化后的文本字符串。</returns>
        private static string Serialize(ConcurrentDictionary<string, string> keyValue)
        {
            var keyValuePairList = keyValue.ToArray().OrderBy(p => p.Key);

            var str = new StringBuilder();
            str.Append("> 配置文件\n");
            str.Append("> 版本 1.0\n");

            foreach (var temp in keyValuePairList)
            {
                // str.AppendLine 在一些地区使用的是 \r\n 所以不符合反序列化

                str.Append(EscapeString(temp.Key));
                str.Append("\n");
                str.Append(EscapeString(temp.Value));
                str.Append("\n>\n");
            }

            str.Append("> 配置文件结束");
            return str.ToString();
        }

        /// <summary>
        /// 存储的转义
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string EscapeString(string str)
        {
            // 如果开头是 `>` 就需要转换为 `?>`
            // 开头是 `?` 转换为 `??`

            var splitString = _splitString;
            var escapeString = _escapeString;

            if (str.StartsWith(splitString, StringComparison.Ordinal))
            {
                return _escapeString + str;
            }

            if (str.StartsWith(escapeString, StringComparison.Ordinal))
            {
                return _escapeString + str;
            }

            return str;
        }

        /// <summary>
        /// 存储的反转义
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string UnescapeString(string str)
        {
            var escapeString = _escapeString;

            if (str.StartsWith(escapeString, StringComparison.Ordinal))
            {
                return str.Substring(1);
            }

            return str;
        }
    }
}
