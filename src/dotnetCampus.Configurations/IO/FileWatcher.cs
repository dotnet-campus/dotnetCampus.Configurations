﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using dotnetCampus.Configurations.IO;
using dotnetCampus.Configurations.Utils;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable

namespace dotnetCampus.IO
{
    /// <summary>
    /// 监视特定文件内容的改变。
    /// 即使文件或文件夹不存在也能在其将来被创建的时候监视；或者如果删除，将在其下次创建的时候监视。
    /// </summary>
    public sealed class FileWatcher
    {
        /// <summary>监视的文件。</summary>
        private readonly FileInfo _file;

        /// <summary>获取当前监视的文件夹监视器。可能会因为文件（夹）不存在而改变。</summary>
        private FileSystemWatcher? _watcher;

        /// <summary>当前正在等待的异步任务。</summary>
        private Task? _waitingAsyncAction;

        /// <summary>
        /// 创建用于监视 <paramref name="fileName"/> 的 <see cref="FileWatcher"/>。
        /// </summary>
        /// <param name="fileName">要监视文件的完全限定路径。</param>
        public FileWatcher(string fileName) =>
            _file = new FileInfo(fileName ?? throw new ArgumentNullException(nameof(fileName)));

        /// <summary>
        /// 创建用于监视 <paramref name="file"/> 的 <see cref="FileWatcher"/>。
        /// </summary>
        /// <param name="file">要监视的文件。</param>
        public FileWatcher(FileInfo file) => _file = file ?? throw new ArgumentNullException(nameof(file));

        /// <summary>
        /// 当要监视的文件的内容改变的时候发生事件。
        /// </summary>
        public event EventHandler? Changed;

        private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// 异步开始监视文件的改变。
        /// </summary>
        /// <remarks>
        /// 此方法可以被重复调用，不会引发异常或导致重复监视。
        /// </remarks>
        public async Task WatchAsync()
        {
            await WaitPendingTaskAsync().ConfigureAwait(false);
            _waitingAsyncAction = Task.Run(Watch);
            await _waitingAsyncAction.ConfigureAwait(false);
        }

        /// <summary>
        /// 异步开始监视文件的改变。
        /// </summary>
        /// <remarks>
        /// 此方法可以被重复调用，不会引发异常或导致重复监视。
        /// </remarks>
        public async Task StopAsync()
        {
            await WaitPendingTaskAsync().ConfigureAwait(false);
            _waitingAsyncAction = null;
            Stop();
        }

        /// <summary>
        /// 监视文件的改变。
        /// </summary>
        /// <remarks>
        /// 此方法可以被重复调用，不会引发异常或导致重复监视。
        /// </remarks>
        private void Watch()
        {
            Stop();

            var pair = FindWatchableLevel();
            var directory = pair._directory;
            var file = pair._file;

            if (string.IsNullOrEmpty(directory))
            {
                //文件的上级目录找不到，放弃了
                //比如文件所在的盘符不存在了
                return;
            }

            if (File.Exists(_file.FullName))
            {
                // 如果文件存在，说明这是最终的文件。
                // 注意使用 File.Exists 判断已存在的同名文件夹时会返回 false。
                _watcher = new FileSystemWatcher(directory, file)
                {
                    EnableRaisingEvents = true,
                    NotifyFilter =
                        // 文件被修改
                        NotifyFilters.LastWrite
                        // 文件被删除
                        | NotifyFilters.FileName,
                };
                var weakEvent = new FileSystemWatcherWeakEventRelay(_watcher);
                weakEvent.Changed += FinalFile_Changed;
                weakEvent.Deleted += FileOrDirectory_CreatedOrDeleted;
            }
            else
            {
                try
                {
                    // 注意这里的 file 可能是文件也可能是文件夹。
                    _watcher = new FileSystemWatcher(directory, file)
                    {
                        EnableRaisingEvents = true,
                    };
                    var weakEvent = new FileSystemWatcherWeakEventRelay(_watcher);
                    weakEvent.Created += FileOrDirectory_CreatedOrDeleted;
                    weakEvent.Renamed += FileOrDirectory_CreatedOrDeleted;
                    weakEvent.Deleted += FileOrDirectory_CreatedOrDeleted;
                }
                catch (FileNotFoundException)
                {
                    //在FindWatchableLevel找到上级目录之后，到执行到这里上级目录又被删除了，重试
                    Watch();
                }
            }
        }

        /// <summary>
        /// 停止监视文件的改变。
        /// </summary>
        /// <remarks>
        /// 此方法可以被重复调用。
        /// </remarks>
        private void Stop()
        {
            // 文件 / 文件夹已经创建，所以之前的监视不需要了。
            // 文件 / 文件夹被删除了，所以之前的监视没法儿用了。
            // Dispose 之后，这个对象就没用了，事件也不会再引发，所以不需要注销事件。
            _watcher?.Dispose();
            _watcher = null;
        }

        private void FileOrDirectory_CreatedOrDeleted(object sender, FileSystemEventArgs e)
        {
            CT.Log($"[文件 {e.ChangeType}]", _file.Name);

            // 当文件创建或删除之后，需要重新设置监听方式。
            Watch();

            // 文件创建或删除也是文件内容改变的一种（0 字节变多或者文件内容变 0 字节），通知调用者文件内容已经发生改变。
            OnChanged();
        }

        private void FinalFile_Changed(object sender, FileSystemEventArgs e)
        {
            CT.Log($"[文件 Changed]", _file.Name);
            OnChanged();
        }

        /// <summary>
        /// 从 <see cref="_file"/> 开始寻找第一层存在的文件夹，返回里面的文件。
        /// </summary>
        /// <returns></returns>
        private FolderPair FindWatchableLevel()
        {
            var path = _file.FullName;

            // 如果文件存在，就返回文件所在的文件夹和文件本身。
            if (File.Exists(path))
            {
                return new FolderPair(Path.GetDirectoryName(path), Path.GetFileName(path));
            }

            // 如果文件不存在，但文件夹存在，也是返回文件夹和文件本身。
            // 这一点在下面的第一层循环中体现。

            // 对于每一层循环。
            while (true)
            {
                var directory = Path.GetDirectoryName(path);
                var file = Path.GetFileName(path);

                // 如果找不到上级目录了，直接返回
                if (string.IsNullOrEmpty(directory))
                {
                    return new FolderPair(directory, file);
                }

                // 检查文件夹是否存在，只要文件夹存在，那么就可以返回。
                if (JunctionPoint.CheckDirectoryExistsWithJunctionPoint(directory))
                {
                    return new FolderPair(directory, file);
                }

                // 如果连文件夹都不存在，那么就需要查找上一层文件夹。
                path = directory;
            }
        }

        /// <summary>
        /// 等待当前未完成的任务直到完成。
        /// </summary>
        private async Task WaitPendingTaskAsync()
        {
            if (_waitingAsyncAction != null)
            {
                await _waitingAsyncAction.ConfigureAwait(false);
            }
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly struct FolderPair
        {
            internal readonly string? _directory;
            internal readonly string? _file;

            public FolderPair(string? directory, string? file)
            {
                _directory = directory;
                _file = file;
            }
        }
    }
}
