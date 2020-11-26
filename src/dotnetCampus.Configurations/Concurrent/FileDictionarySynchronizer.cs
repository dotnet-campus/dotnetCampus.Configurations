using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using CT = dotnetCampus.Configurations.Core.ConfigTracer;

namespace dotnetCampus.Configurations.Concurrent
{
    /// <summary>
    /// 将文件与 <see cref="ProcessConcurrentDictionary{TKey, TValue}"/> 进行跨进程安全同步的辅助工具。
    /// </summary>
    internal class FileDictionarySynchronizer<TKey, TValue>
    {
        /// <summary>
        /// 获取当前已知具有高精度文件时间的文件系统名称（不区分大小写）。
        /// https://en.wikipedia.org/wiki/Comparison_of_file_systems
        /// </summary>
        private static readonly HashSet<string> KnownHighResolutionDriveFormats = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Windows NTFS, 100ns
            "NTFS",

            // macOS APFS, 1ns
            "APFS",

            // Linux, 1ms
            "EXT4",

            // 其他已知非高精度时间的文件系统：
            // FAT8: 不支持记录时间，且 Windows 系统不支持
            // FAT12, FAT16, FAT16B 和 FAT32:
            //  * 修改时间 2s
            //  * 创建时间 10ms
            //  * 访问时间 1d
            //  * 删除时间 2s
            // exFAT: 10ms
            // Live File System (UDF): 1μs
            // XFS 和 EXT3: 1s
        };

        private readonly FileInfo _file;
        private readonly Func<IReadOnlyDictionary<TKey, TValue>, string> _serializer;
        private readonly Func<string, IReadOnlyDictionary<TKey, TValue>> _deserializer;

        /// <summary>
        /// 如果此文件所在的分区支持高精度时间，则此值为 true，否则为 false。
        /// 当此值为 false 时，将不能依赖于时间判定文件内容的改变；当为 true 时，大概率可以依赖时间来判定文件内容的改变。
        /// </summary>
        private readonly bool _supportHighResolutionFileTime;

        /// <summary>
        /// 上次同步文件时，文件的修改时间。如果时间相同，我们就认为文件没有更改过。
        /// </summary>
        private DateTimeOffset _fileLastWriteTime = DateTimeOffset.MinValue;

        /// <summary>
        /// 上次同步文件时，文件的全文内容。
        /// </summary>
        private string _lastSyncedFileContent = "";

        [ContractPublicPropertyName(nameof(FileSyncingCount))]
        private long _fileSyncingCount;

        [ContractPublicPropertyName(nameof(FileSyncingErrorCount))]
        private long _fileSyncingErrorCount;

        /// <summary>
        /// 创建 <see cref="FileDictionarySynchronizer{TKey, TValue}"/> 的新实例，这个实例将帮助同步一个文件和一个内存中的跨进程安全的字典。
        /// </summary>
        /// <param name="file">要同步的文件。</param>
        /// <param name="serializer">指定如何从键值集合序列化成一个字符串。</param>
        /// <param name="deserializer">指定如何从一个字符串反序列化成一个键值集合。</param>
        public FileDictionarySynchronizer(FileInfo file,
            Func<IReadOnlyDictionary<TKey, TValue>, string> serializer,
            Func<string, IReadOnlyDictionary<TKey, TValue>> deserializer)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));

            try
            {
                var drive = new DriveInfo(file.FullName);
                _supportHighResolutionFileTime = KnownHighResolutionDriveFormats.Contains(drive.DriveFormat);
            }
            catch (Exception)
            {
                // 可能连本地驱动器都不是。
                _supportHighResolutionFileTime = false;
            }
        }

        /// <summary>
        /// 获取此配置与文件同步的总尝试次数（包含失败的尝试）。
        /// </summary>
        public long FileSyncingCount => _fileSyncingCount;

        /// <summary>
        /// 获取此配置与文件的同步失败次数。
        /// </summary>
        public long FileSyncingErrorCount => _fileSyncingErrorCount;

        /// <summary>
        /// 存储运行时保存的键值对。
        /// </summary>
        public ProcessConcurrentDictionary<TKey, TValue> Dictionary { get; }
            = new ProcessConcurrentDictionary<TKey, TValue>();

        /// <summary>
        /// 将文件与内存模型进行同步。
        /// </summary>
        /// <returns>可异步等待的对象。</returns>
        public void Synchronize()
        {
            Dictionary.UpdateValuesFromExternal(_file, context =>
            {
                // 此处代码是跨进程安全的。
                CT.Debug($"同步中，正在进程安全区...", "File");
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
        }

        /// <summary>
        /// 将内存中的键值集合同步到文件中，会根据键值的修改时间来修改文件的修改时间。
        /// 此方法保证跨进程/线程的安全执行。
        /// </summary>
        /// <param name="context">用于合并文件与内存中的键值集合。</param>
        private void SynchronizeCore(ICriticalReadWriteContext<TKey, TValue> context)
        {
            // 获取文件的外部更新时间。
            _file.Refresh();
            var utcNow = DateTimeOffset.UtcNow;
            var lastWriteTime = _file.Exists ? FixFileTime(_file.LastWriteTimeUtc, utcNow) : utcNow;
            if (_supportHighResolutionFileTime && lastWriteTime == _fileLastWriteTime)
            {
                // 在支持高精度时间的文件系统上：
                // 自上次同步文件以来，文件从未发生过更改（无需提前打开文件）。
                var newLastWriteTime = WriteFileOrDoNothing(context, lastWriteTime);
                _file.LastWriteTimeUtc = newLastWriteTime.UtcDateTime;
                _fileLastWriteTime = newLastWriteTime;
            }
            else
            {
                // 文件已经发生了更改。
                var newLastWriteTime = ReadWriteFile(context, lastWriteTime);
                _file.LastWriteTimeUtc = newLastWriteTime.UtcDateTime;
                _fileLastWriteTime = newLastWriteTime;
            }
        }

        /// <summary>
        /// 打开文件进行读写，以将内存中的键值集合同步到文件中。
        /// 此方法保证跨进程/线程的安全执行。
        /// </summary>
        /// <param name="context">用于合并文件与内存中的键值集合。</param>
        /// <param name="lastWriteTime">文件的上一次修改时间。</param>
        /// <returns>修改了文件后，新的文件修改时间（如果内容不变，则时间也不变）。</returns>
        private DateTimeOffset ReadWriteFile(ICriticalReadWriteContext<TKey, TValue> context, DateTimeOffset lastWriteTime)
        {
            // 在会打开文件流的地方自增。
            Interlocked.Increment(ref _fileSyncingCount);
            CT.Debug($"正在读取和写入文件...", "File");

            // 读取文件。
            using var fs = new FileStream(
                _file.FullName, FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None,
                0x1000, FileOptions.SequentialScan | FileOptions.WriteThrough);
            using var reader = new StreamReader(fs, Encoding.UTF8, true, 0x1000, true);
            var text = reader.ReadToEnd();
            _lastSyncedFileContent = text;

            // 将文件中的键值集合与内存中的键值集合合并。
            var newText = MergeFileTextAndKeyValueText(context, lastWriteTime, _lastSyncedFileContent, out var updatedTime);

            // 将合并后的键值集合写回文件。
            var areTheSame = string.Equals(text, newText, StringComparison.Ordinal);
            if (!areTheSame)
            {
                WriteOrDoNothing(fs, newText);
                return updatedTime;
            }
            else
            {
                return lastWriteTime;
            }
        }

        /// <summary>
        /// 将内存中的键值集合与之前打开过的文件内容进行合并，如果导致文件更改，则将更改写入到文件中，否则不做任何事情。
        /// 此方法保证跨进程/线程的安全执行。
        /// </summary>
        /// <param name="context">用于合并文件与内存中的键值集合。</param>
        /// <param name="lastWriteTime">文件的上一次修改时间。</param>
        /// <returns>修改了文件后，新的文件修改时间（如果内容不变，则时间也不变）。</returns>
        /// <returns></returns>
        private DateTimeOffset WriteFileOrDoNothing(ICriticalReadWriteContext<TKey, TValue> context, DateTimeOffset lastWriteTime)
        {
            // 将文件中的键值集合与内存中的键值集合合并。
            var text = _lastSyncedFileContent;
            var newText = MergeFileTextAndKeyValueText(context, lastWriteTime, _lastSyncedFileContent, out var updatedTime);

            // 将合并后的键值集合写回文件。
            var areTheSame = string.Equals(text, newText, StringComparison.Ordinal);
            if (!areTheSame)
            {
                // 在会打开文件流的地方自增。
                Interlocked.Increment(ref _fileSyncingCount);
                CT.Debug($"正在写入文件...", "File");

                // 写入。
                using var fs = new FileStream(
                    _file.FullName, FileMode.OpenOrCreate,
                    FileAccess.Write, FileShare.None,
                    0x1000, FileOptions.WriteThrough);
                WriteOrDoNothing(fs, newText);
                _lastSyncedFileContent = newText;
                return updatedTime;
            }
            else
            {
                return lastWriteTime;
            }
        }

        private static void WriteOrDoNothing(FileStream fileStream, string text)
        {
            using var writer = new StreamWriter(fileStream, new UTF8Encoding(false, false), 0x1000, true);
            fileStream.Position = 0;
            writer.Write(text);
            writer.Flush();
            fileStream.SetLength(fileStream.Position);
        }

        private string MergeFileTextAndKeyValueText(
            ICriticalReadWriteContext<TKey, TValue> context,
            DateTimeOffset lastWriteTime,
            string text, out DateTimeOffset updatedWriteTime)
        {
            var externalKeyValues = _deserializer(text);
            var timedMerging = context.MergeExternalKeyValues(externalKeyValues, lastWriteTime);
            var mergedKeyValues = timedMerging.KeyValues.ToDictionary(x => x.Key, x => x.Value);
            var newText = _serializer(mergedKeyValues);
            updatedWriteTime = timedMerging.Time;
            return newText;
        }

        /// <summary>
        /// 如果文件的时间更新，则说明文件是从未来穿越过来的。
        /// 来自未来的文件总是更新，这会导致内存中的所有值都无法更新到文件中。
        /// 于是，我们需要把来自未来的文件拖下水，让它适配古代的时间。
        /// </summary>
        /// <param name="time">文件的最近修改时间。</param>
        /// <param name="utcNow">当前时间。</param>
        /// <returns>应该视为的新文件时间。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DateTimeOffset FixFileTime(DateTimeOffset time, DateTimeOffset utcNow) =>
            // 如果文件的时间更新，则说明文件是从未来穿越过来的。
            // 来自未来的文件总是更新，这会导致内存中的所有值都无法更新到文件中。
            // 于是，我们需要把来自未来的文件拖下水，让它适配古代的时间。
            time > utcNow ? utcNow : time;
    }
}
