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
        private readonly PartialAwaitableRetry _saveLoop;
        private readonly FileInfo _file;

        /// <summary>
        /// 发现文件已改变，正在等待重新读取文件。
        /// </summary>
        private bool _isPendingReload;

        /// <summary>
        /// 发现文件改变，并且在读取的过程中又改变了，因此可能有必要重读。
        /// </summary>
        private bool _isPendingReloadReentered;

        private DateTimeOffset _lastDeserializeTime = DateTimeOffset.MinValue;
        private long _fileSyncingCount;
        private long _fileSyncingErrorCount;
        private readonly FileWatcher _watcher;

        /// <summary>
        /// 初始化使用 <paramref name="fileName"/> 作为配置文件的 <see cref="FileConfigurationRepo"/> 的新实例。
        /// </summary>
        /// <param name="fileName">配置文件的文件路径。</param>
        [Obsolete("请改用线程安全的 ConfigurationFactory 来创建实例。")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public FileConfigurationRepo(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            var fullPath = Path.GetFullPath(fileName);
            _file = new FileInfo(fullPath);
            _saveLoop = new PartialAwaitableRetry(LoopSyncTask);

            // 监视文件改变。
            _watcher = new FileWatcher(_file);
            _watcher.Changed += OnFileChanged;
            _ = _watcher.WatchAsync();
            ReadFromFileTask = SynchronizeAsync();
        }

        /// <summary>
        /// 在文件改变后的延迟读取时间。
        /// </summary>
        public TimeSpan DelayReadTime { get; set; } = TimeSpan.FromMilliseconds(1);

        /// <summary>
        /// 延迟保存的时间
        /// </summary>
        public TimeSpan DelaySaveTime { get; set; } = TimeSpan.FromMilliseconds(1);

        /// <summary>
        /// 获取此配置与文件同步的总尝试次数（包含失败的尝试）。
        /// </summary>
        public long FileSyncingCount => _fileSyncingCount;

        /// <summary>
        /// 获取此配置与文件的同步失败次数。
        /// </summary>
        public long FileSyncingErrorCount => _fileSyncingErrorCount;

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
            await ReadFromFileTask.ConfigureAwait(false);
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
            await ReadFromFileTask.ConfigureAwait(false);
            CT.Debug($"{key} = {value}", "Set");
            KeyValues[key] = new CommentedValue<string>(value);
        }

        /// <summary>
        /// 将为指定的 Key 清除。
        /// </summary>
        /// <param name="key">指定项的 Key。</param>
        protected override async Task RemoveValueCoreAsync(string key)
        {
            await ReadFromFileTask.ConfigureAwait(false);
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
        /// 请求将文件与内存模型进行同步。
        /// 当采用不安全的读写文件策略时，有可能发生文件读写冲突；而发生时，会尝试 <paramref name="tryCount"/> 次。
        /// </summary>
        /// <param name="tryCount">尝试次数。当失败时会尝试重新同步，此值表示算上失败后限制的同步总次数。</param>
        /// <returns>可异步等待的对象。</returns>
        public async Task SaveAsync(int tryCount = 10)
        {
            // 执行一次等待以便让代码中大量调用的同步（利用 PartialAwaitableRetry 的机制）共用同一个异步任务，节省资源。
            // 副作用是会慢一拍。
            await Task.Delay(DelaySaveTime).ConfigureAwait(false);

            // 执行同步。
            await SynchronizeAsync(tryCount).ConfigureAwait(false);
        }

        /// <summary>
        /// 尝试重新加载此配置文件的外部修改（例如使用其他编辑器或其他客户端修改的部分）。
        /// <para>外部修改会自动同步到此配置中，但此同步不会立刻发生，所以如果你明确知道外部修改了文件后需要立刻重新加载外部修改，才需要调用此方法。</para>
        /// </summary>
        public async Task ReloadExternalChangesAsync()
        {
            // 如果之前正在读取文件，则等待文件读取完成。
            await ReadFromFileTask.ConfigureAwait(false);
            // 现在，强制要求重新读取文件。
            ReadFromFileTask = SynchronizeAsync();
            // 然后，等待重新读取完成。
            await ReadFromFileTask.ConfigureAwait(false);
        }

        /// <summary>
        /// 存储运行时保存的键值对。
        /// </summary>
        private ProcessConcurrentDictionary<string, CommentedValue<string>> KeyValues { get; }
            = new ProcessConcurrentDictionary<string, CommentedValue<string>>();

        private Task ReadFromFileTask { get; set; }

        /// <summary>
        /// 在配置文件改变的时候，重新读取文件。
        /// </summary>
        private async void OnFileChanged(object? sender, EventArgs e)
        {
            CT.Debug($"检测到文件被改变...", "File");
            var isPending = _isPendingReload;
            if (isPending)
            {
                // 如果发现已经在准备读取文件了，那么就告诉他又进来了一次，他可能还需要读。
                _isPendingReloadReentered = true;
                return;
            }

            _isPendingReload = true;

            try
            {
                do
                {
                    _isPendingReloadReentered = false;
                    // 等待时间为预期等待时间的 1/2，因为多数情况下，一次文件的改变会收到两次 Change 事件。
                    // 第一次是文件内容的写入，第二次是文件信息（如最近写入时间）的写入。
                    await Task.Delay((int)DelayReadTime.TotalMilliseconds / 2).ConfigureAwait(false);
                } while (_isPendingReloadReentered);

                // 如果之前正在读取文件，则等待文件读取完成。
                await ReadFromFileTask.ConfigureAwait(false);

                // 现在重新读取。
                // - ~~重新读取文件时不影响对键值对的访问，所以不要求其他地方等待 ReadFromFileTask。~~
                // - 但是，如果正在序列化和保存文件，为了避免写入时覆盖未读取完的键值对，需要等待读取完毕。
                // ！特别注意！：外部写完文件后配置立刻读，读不到新值；需要调用 ReloadExternalChangesAsync 方法强制加载外部修改；否则将等待自动更新修改。
                _ = SynchronizeAsync();
            }
            finally
            {
                _isPendingReload = false;
            }
        }

        private async Task<OperationResult> LoopSyncTask(PartialRetryContext context)
        {
            context.StepCount = 10;
            await Synchronize().ConfigureAwait(false);
            await Task.Delay(DelaySaveTime).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// 请求将文件与内存模型进行同步。
        /// 在读文件时调用此方法后，请将返回值赋值给 <see cref="ReadFromFileTask"/> 以便让后续值的读取使用最新值。
        /// 在写入文件时调用此方法，请仅将返回值用于等待或忽视返回值，因为写入文件不影响后续读值。
        /// </summary>
        /// <param name="tryCount">尝试次数。当失败时会尝试重新同步，此值表示算上失败后限制的同步总次数。当设置为 -1 时表示无限次重试。</param>
        /// <returns>可异步等待的对象。</returns>
        private async Task SynchronizeAsync(int tryCount = -1)
        {
            // 在构造方法中执行时，可能为 null；因此需要判空（在构造函数中，不需要等待读取）。
            if (ReadFromFileTask != null)
            {
                await ReadFromFileTask.ConfigureAwait(false);
            }

            await _saveLoop.JoinAsync(tryCount);
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
                try
                {
                    SynchronizeCore(context);
                    return;
                }
                catch (IOException)
                {
                    // 可能存在某些旧版本的代码通过非进程安全的方式读写文件。
                    Interlocked.Increment(ref _fileSyncingErrorCount);
                    throw;
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
            Interlocked.Increment(ref _fileSyncingCount);

            // 获取文件的外部更新时间。
            _file.Refresh();
            var lastWriteTime = _file.Exists ? _file.LastWriteTimeUtc : DateTimeOffset.UtcNow;

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
            var newText = CoinConfigurationSerializer.Serialize(mergedKeyValues
                .ToDictionary(x => x.Key, x => x.Value.Value, StringComparer.Ordinal));

            // 将合并后的键值集合写回文件。
            var areTheSame = string.Equals(text, newText, StringComparison.Ordinal);
            var handle = fs.SafeFileHandle.DangerousGetHandle();
            long keepUnchanged = unchecked((long)0xFFFFFFFFFFFFFFFF);
            if (!areTheSame)
            {
                SetFileTime(handle, keepUnchanged, keepUnchanged, keepUnchanged);
                using var writer = new StreamWriter(fs, new UTF8Encoding(false, false), 0x1000, true);
                fs.Position = 0;
                writer.Write(newText);
                writer.Flush();
                fs.SetLength(fs.Position);
                long fileTime = timedMerging.Time.ToFileTime();
            }
            else
            {
                SetFileTime(handle, keepUnchanged, keepUnchanged, keepUnchanged);
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetFileTime(IntPtr hFile, in long lpCreationTime, in long lpLastAccessTime, in long lpLastWriteTime);
    }
}
