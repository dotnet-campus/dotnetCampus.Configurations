using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using dotnetCampus.Configurations.Concurrent;
using dotnetCampus.Configurations.Utils;
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
            var value = KeyValues.TryGetValue(key, out var v) ? v.Value : null;
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
            KeyValues[key] = new CommentedValue<string>(value);
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
        private ProcessConcurrentDictionary<string, CommentedValue<string>> KeyValues { get; }
            = new ProcessConcurrentDictionary<string, CommentedValue<string>>();

        private Task LoadFromFileTask { get; set; }

        private readonly PartialAwaitableRetry _saveLoop;
        private readonly FileInfo _file;
        private bool _isPendingReread;
        private bool _isPendingRereadReentered;
        private DateTimeOffset _lastDeserializeTime = DateTimeOffset.MinValue;
        private readonly FileWatcher _watcher;

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
            LoadFromFileTask = Task.Run(async () => await Synchronize().ConfigureAwait(false));
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
            await Synchronize().ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// 将文件与内存模型进行同步。
        /// </summary>
        /// <returns>可异步等待的对象。</returns>
        private Task Synchronize()
        {
            KeyValues.UpdateValuesFromExternal(_file, context =>
            {
                // 此处代码是跨进程安全的。
                var random = new Random();
                for (var i = 0; i < 4; i++)
                {
                    try
                    {
                        SynchronizeCore(context);
                        return;
                    }
                    catch (IOException)
                    {
                        // 可能存在某些旧版本的代码通过非进程安全的方式读写文件。
                        var waitMilliseconds = random.Next(50, 150);
                        Thread.Sleep(waitMilliseconds);
                    }
                }
            });
#if NETFRAMEWORK
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        private void SynchronizeCore(ICriticalReadWriteContext<string, CommentedValue<string>> context)
        {
            // 获取文件的外部更新时间。
            _file.Refresh();

            // 同步并比较新旧时间。
            var lastWriteTime = _file.Exists ? _file.LastWriteTimeUtc : DateTimeOffset.UtcNow;
            var newLastWriteTime = SynchronizeFileCore(context, lastWriteTime);

            // 如果时间改变，就将时间写入。
            if (lastWriteTime != newLastWriteTime)
            {
                _file.LastWriteTimeUtc = newLastWriteTime.DateTime;
            }
        }

        private DateTimeOffset SynchronizeFileCore(ICriticalReadWriteContext<string, CommentedValue<string>> context, DateTimeOffset lastWriteTime)
        {
            // 读取文件。
            using var fs = new FileStream(
                _file.FullName, FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None,
                0x1000, FileOptions.SequentialScan | FileOptions.WriteThrough);
            using var reader = new StreamReader(fs, Encoding.UTF8, true, 0x1000, true);
            var text = reader.ReadToEnd();

            // 将文件中的键值集合与内存中的键值集合合并。
            var externalKeyValues = CoinConfigurationSerializer.Deserialize(text)
                .ToDictionary(x => x.Key, x => new CommentedValue<string>(x.Value, ""), StringComparer.Ordinal);
            var timedMerging = context.MergeExternalKeyValues(externalKeyValues, lastWriteTime);
            var mergedKeyValues = timedMerging.KeyValues.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);

            // 将合并后的键值集合写回文件。
            var newText = CoinConfigurationSerializer.Serialize(mergedKeyValues
                .ToDictionary(x => x.Key, x => x.Value.Value, StringComparer.Ordinal));
            var areTheSame = string.Equals(text, newText, StringComparison.Ordinal);
            if (!areTheSame)
            {
                using var writer = new StreamWriter(fs, new UTF8Encoding(false, false), 0x1000, true);
                fs.Position = 0;
                writer.Write(newText);
                writer.Flush();
                fs.SetLength(fs.Position);
            }
            return timedMerging.Time;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetFileTime(IntPtr hFile, in ulong lpCreationTime, in ulong lpLastAccessTime, in ulong lpLastWriteTime);
    }
}
