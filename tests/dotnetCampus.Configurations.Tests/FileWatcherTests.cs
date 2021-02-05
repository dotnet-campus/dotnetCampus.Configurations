using dotnetCampus.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MSTest.Extensions.Contracts;

using Walterlv.IO.PackageManagement;

namespace dotnetCampus.Configurations.Tests
{
    [TestClass]
    public class FileWatcherTests
    {
        /// <summary>
        /// 当遇到目录联接文件夹时。
        /// </summary>
        [ContractTestCase]
        public void 目录联接()
        {
            "监视一个目标已不存在的目录联接，不会堆栈溢出。".Test(async () =>
            {
                PackageDirectory.Create("1.0.0");
                PackageDirectory.Link("current", "1.0.0", true);
                PackageDirectory.Delete("1.0.0");

                var watcher = new FileWatcher(@"current\a.txt");
                await watcher.WatchAsync().ConfigureAwait(false);
            });
        }
    }
}
