using System.IO;
using Walterlv.WeakEvents;

namespace dotnetCampus.Configurations.IO
{
    /// <summary>
    /// 此类型由机器生成，有关此类型生成的详细信息，请参阅 <see cref="WeakEventRelay{TEventSource}"/>，
    /// 或者阅读文档：https://blog.walterlv.com/post/implement-custom-dotnet-weak-event-relay.html
    /// </summary>
    internal sealed class FileSystemWatcherWeakEventRelay : WeakEventRelay<FileSystemWatcher>
    {
        public FileSystemWatcherWeakEventRelay(FileSystemWatcher eventSource) : base(eventSource) { }

        private readonly WeakEvent<FileSystemEventArgs> _created = new WeakEvent<FileSystemEventArgs>();
        private readonly WeakEvent<FileSystemEventArgs> _changed = new WeakEvent<FileSystemEventArgs>();
        private readonly WeakEvent<RenamedEventArgs> _renamed = new WeakEvent<RenamedEventArgs>();
        private readonly WeakEvent<FileSystemEventArgs> _deleted = new WeakEvent<FileSystemEventArgs>();

        public event FileSystemEventHandler Created
        {
            add => Subscribe(o => o.Created += OnCreated, () => _created.Add(value, value.Invoke));
            remove => _created.Remove(value);
        }

        public event FileSystemEventHandler Changed
        {
            add => Subscribe(o => o.Changed += OnChanged, () => _changed.Add(value, value.Invoke));
            remove => _changed.Remove(value);
        }

        public event RenamedEventHandler Renamed
        {
            add => Subscribe(o => o.Renamed += OnRenamed, () => _renamed.Add(value, value.Invoke));
            remove => _renamed.Remove(value);
        }

        public event FileSystemEventHandler Deleted
        {
            add => Subscribe(o => o.Deleted += OnDeleted, () => _deleted.Add(value, value.Invoke));
            remove => _deleted.Remove(value);
        }

        private void OnCreated(object sender, FileSystemEventArgs e) => TryInvoke(_created, sender, e);
        private void OnChanged(object sender, FileSystemEventArgs e) => TryInvoke(_changed, sender, e);
        private void OnRenamed(object sender, RenamedEventArgs e) => TryInvoke(_renamed, sender, e);
        private void OnDeleted(object sender, FileSystemEventArgs e) => TryInvoke(_deleted, sender, e);

        protected override void OnReferenceLost(FileSystemWatcher source)
        {
            source.Created -= OnCreated;
            source.Changed -= OnChanged;
            source.Renamed -= OnRenamed;
            source.Deleted -= OnDeleted;
            source.Dispose();
        }
    }
}
