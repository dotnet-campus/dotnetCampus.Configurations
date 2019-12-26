using System;
using System.IO;
using System.Threading;

namespace Walterlv.Demo
{
    internal class SafeFileReadWriteToken : IDisposable
    {
        public static SafeFileReadWriteToken GetForFile(string fileName) => GetForFile(new FileInfo(fileName));

        public static SafeFileReadWriteToken GetForFile(FileInfo file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var path = file.FullName.Replace(Path.DirectorySeparatorChar, '-');
            var mutex = new Mutex(false, $"dotnet-campus-configuration-{path}");
            mutex.WaitOne();
            return new SafeFileReadWriteToken(mutex);
        }

        private readonly Mutex _mutex;
        private bool _disposedValue = false;

        private SafeFileReadWriteToken(Mutex mutex)
        {
            _mutex = mutex ?? throw new ArgumentNullException(nameof(mutex));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _mutex.ReleaseMutex();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
