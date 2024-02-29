using dotnetCampus.Configurations.Core;
using dotnetCampus.Configurations.Tests.Fakes;
using dotnetCampus.Configurations.Tests.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace dotnetCampus.Configurations.Tests
{
    [TestClass]
    public class FileConfigurationRepoTests
    {
        [ContractTestCase]
        public void WriteAsync()
        {
            "传入的 key 为空或空字符串，抛出异常。".Test(async () =>
            {
                // Arrange
                var coin = TestUtil.GetTempFile(null, ".coin");
                var repo = CreateIndependentRepo(coin);

                // Act && Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                    () => repo.WriteAsync(null!, "123")).ConfigureAwait(false);
                _ = repo.WriteAsync("", "123");
            });

            "写入空白的值，清空这一个值。".Test(async () =>
            {
                // Arrange
                var coin = TestUtil.GetTempFile(null, ".coin");
                var repo = CreateIndependentRepo(coin);

                // Act
                await repo.WriteAsync("123", "123").ConfigureAwait(false);
                await repo.WriteAsync("123", null).ConfigureAwait(false);
                await repo.SaveAsync().ConfigureAwait(false);

                // Assert
                var repo2 = CreateIndependentRepo(coin);
                var test = await repo.TryReadAsync("123", "默认").ConfigureAwait(false);
                Assert.AreEqual("默认", test);
            });

            "如果文件存在重复的值，最后一个值生效。".Test(async () =>
            {
                // Arrange
                var coin = TestUtil.GetTempFile("configs.sim.coin");
                var repo = CreateIndependentRepo(coin);

                // Act
                var value = await repo.TryReadAsync("Value").ConfigureAwait(false);

                // Assert
                Assert.AreEqual("2", value);
            });

            "写入重复的值，最后一个值生效。".Test(async () =>
            {
                // Arrange
                var coin = TestUtil.GetTempFile(null, ".coin");
                var repo = CreateIndependentRepo(coin);
                var keyvalueList = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>(">正常的值.Foo", ">123"),
                    new KeyValuePair<string, string>(">正常的值.Foo", "新的值"),
                    new KeyValuePair<string, string>("正常的值.Foo", "123"),
                    new KeyValuePair<string, string>("?正常的值.Foo", "?123"),
                    new KeyValuePair<string, string>("??正常的值.Foo", "?123"),
                };

                // Act
                foreach (var keyvalue in keyvalueList)
                {
                    await repo.WriteAsync(keyvalue.Key, keyvalue.Value).ConfigureAwait(false);
                }
                await repo.SaveAsync().ConfigureAwait(false);

                // Assert
                var repo2 = CreateIndependentRepo(coin);
                var test = await repo2.TryReadAsync(">正常的值.Foo").ConfigureAwait(false);
                Assert.AreEqual("新的值", test);
            });

            "写入需要转义的值，存放的文件是转义后的字符。".Test(async () =>
            {
                // Arrange
                var coin = TestUtil.GetTempFile(null, ".coin");
                var repo = CreateIndependentRepo(coin);
                var keyvalueList = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>(">正常的值.Foo", ">123"),
                    new KeyValuePair<string, string>(">>正常的值.Foo", ">123"),
                    new KeyValuePair<string, string>("正常的值.Foo", "123"),
                    new KeyValuePair<string, string>("?正常的值.Foo", "?123"),
                    new KeyValuePair<string, string>("??正常的值.Foo", "?123"),
                };

                // Act
                foreach (var keyvalue in keyvalueList)
                {
                    await repo.WriteAsync(keyvalue.Key, keyvalue.Value).ConfigureAwait(false);
                }
                await repo.SaveAsync().ConfigureAwait(false);

                // Assert
                var repo2 = CreateIndependentRepo(coin);
                foreach (var keyvalue in keyvalueList)
                {
                    var test = await repo2.TryReadAsync(keyvalue.Key).ConfigureAwait(false);
                    Assert.AreEqual(keyvalue.Value, test);
                }
            });

            "写入正常的值，正常存储，可以读出存放的值。".Test(async () =>
            {
                // Arrange
                var coin = TestUtil.GetTempFile(null, ".coin");
                var repo = CreateIndependentRepo(coin);

                // Act
                await repo.WriteAsync("正常的值.Foo", "123").ConfigureAwait(false);
                await repo.SaveAsync().ConfigureAwait(false);

                // Assert
                var repo2 = CreateIndependentRepo(coin);
                var test = await repo2.TryReadAsync("正常的值.Foo").ConfigureAwait(false);
                Assert.AreEqual("123", test);
            });

            "多行存储，可以多行读出。".Test(async () =>
            {
                // Arrange
                var coin = TestUtil.GetTempFile(null, ".coin");
                var repo = CreateIndependentRepo(coin);

                // Act
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                repo.WriteAsync("Foo.MultilineValue", @"1
2
3");
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                await repo.SaveAsync().ConfigureAwait(false);

                // Assert
                var repo2 = CreateIndependentRepo(coin);
                var str = await repo2.TryReadAsync("Foo.MultilineValue").ConfigureAwait(false);
                Assert.AreEqual("1\n2\n3", str);
            });

            "如果将值设置为默认值，则配置项会删除。".Test(async () =>
            {
                // Arrange
                var coin = TestUtil.GetTempFile("configs.sim.coin");
                var repo = CreateIndependentRepo(coin);

                // Act
                var value = await repo.TryReadAsync("Test").ConfigureAwait(false);
                Assert.AreEqual("True", value);
                await repo.WriteAsync("Test", null).ConfigureAwait(false);
                await repo.SaveAsync().ConfigureAwait(false);

                // Assert
                var value2 = await CreateIndependentRepo(coin).TryReadAsync("Test").ConfigureAwait(false);
                Assert.AreEqual("", value2);
            });
        }

        [ContractTestCase]
        public void SaveAsync()
        {
            "如果没有文件且不需要存储数据，那么不会创建文件。".Test(async () =>
            {
                // 【注意】
                // 此单元测试仅适用于 FileConfigurationRepo 初始化时，相等策略被指定成 FileEqualsComparison.KeyValueEquals 的情况。
                // 如果指定为 FileEqualsComparison.WholeTextEquals，因为 coin 格式在空集合时也有内容，所以一定会创建文件。

                // Arrange
                var coin = TestUtil.GetTempFile(null, ".coin");
                var repo = CreateIndependentRepo(coin);

                // Act
                await Task.Delay(100).ConfigureAwait(false);

                // Assert
                Assert.IsFalse(File.Exists(coin.FullName));
            });

            "如果没有文件但需要存储数据，那么会创建文件。".Test(async () =>
            {
                // Arrange
                var coin = TestUtil.GetTempFile(null, ".coin");
                var repo = CreateIndependentRepo(coin);

                // Act
                await repo.WriteAsync("Test.Create", "True").ConfigureAwait(false);
                await repo.SaveAsync().ConfigureAwait(false);

                // Assert
                Assert.IsTrue(File.Exists(coin.FullName));
            });

            "如果没有文件甚至连文件夹也不存在但需要存储数据，那么会创建文件夹和文件。".Test(async () =>
            {
                // Arrange
                var coin = TestUtil.GetTempFile(null, ".coin", "Configs");
                var directory = new DirectoryInfo(coin.DirectoryName!);
                if (Directory.Exists(directory.FullName))
                {
                    directory.Delete(true);
                }
                var repo = CreateIndependentRepo(coin);

                // Act
                await repo.WriteAsync("Test.Create", "True").ConfigureAwait(false);
                await repo.SaveAsync().ConfigureAwait(false);

                // Assert
                Assert.IsTrue(File.Exists(coin.FullName));

                // Clean
                directory.Delete(true);
            });
        }

        [ContractTestCase]
        public void ReadAsync()
        {
            "如果配置文件存在且配置存在，那么能读取到配置。".Test(async () =>
            {
                // Arrange
                var coin = TestUtil.GetTempFile("configs.sim.coin");
                var repo = CreateIndependentRepo(coin);

                // Act
                var value = await repo.TryReadAsync("Test").ConfigureAwait(false);

                // Assert
                Assert.AreEqual("True", value);
            });

            "如果配置文件存在但配置不存在，那么能读取到默认值。".Test(async () =>
            {
                // Arrange
                var coin = TestUtil.GetTempFile("configs.sim.coin");
                var repo = CreateIndependentRepo(coin);

                // Act
                var value = await repo.TryReadAsync("NotExist").ConfigureAwait(false);

                // Assert
                Assert.AreEqual("", value);
            });
        }

        /// <summary>
        /// 当监听的文件在外部发生改变的时候。
        /// </summary>
        [ContractTestCase]
        public void 监视文件改变()
        {
            "监听后，文件内容发生了改变，可以读到文件的新值。".Test(async () =>
            {
                var coin = TestUtil.GetTempFile("configs.coin");
                var configs = CreateIndependentRepo(coin).CreateAppConfigurator();
                var fake = configs.Of<FakeConfiguration>();
                var oldValue = fake.Key;
                Assert.AreEqual("Value", oldValue);
                File.WriteAllText(coin.FullName, @">
Key
NewValue
>");
                await configs.ReloadExternalChangesAsync().ConfigureAwait(false);
                var newValue = fake.Key;
                Assert.AreEqual("NewValue", newValue);
            });

            "监听后，文件才被创建，可以读到文件中一开始就存放的值。".Test(async () =>
            {
                var coin = TestUtil.GetTempFile(null, ".coin");
                var configs = CreateIndependentRepo(coin).CreateAppConfigurator();
                var fake = configs.Of<FakeConfiguration>();
                var oldValue = fake.Key;
                Assert.AreEqual("", oldValue);
                File.WriteAllText(coin.FullName, @">
Key
NewValue
>");
                await configs.ReloadExternalChangesAsync().ConfigureAwait(false);
                var newValue = fake.Key;
                Assert.AreEqual("NewValue", newValue);
            });

            "监听后，文件被删除，相当于所有未保存的值全部被删除。".Test(async () =>
            {
                var coin = TestUtil.GetTempFile("configs.coin");
                var configs = CreateIndependentRepo(coin).CreateAppConfigurator();
                var fake = configs.Of<FakeConfiguration>();
                var oldValue = fake.Key;
                Assert.AreEqual("Value", oldValue);
                File.Delete(coin.FullName);
                Thread.Sleep(500);
                await configs.ReloadExternalChangesAsync().ConfigureAwait(false);
                var newValue = fake.Key;
                Assert.AreEqual("", newValue);
            });
        }

        /// <summary>
        /// 并发读写配置。
        /// </summary>
        [ContractTestCase]
        public void 多进程同时读写配置不丢失()
        {
            "A 进程写 A 配置，同时 B 进程写 B 配置；随后检查文件，两个配置均在。".Test(async () =>
            {
                var coin = TestUtil.GetTempFile("configs.coin");
                var repoA = CreateIndependentRepo(coin);
                var repoB = CreateIndependentRepo(coin);
                var repo = CreateIndependentRepo(coin);
                var fakeA = repoA.CreateAppConfigurator().Of<FakeConfiguration>();
                var fakeB = repoB.CreateAppConfigurator().Of<FakeConfiguration>();
                var fake = repo.CreateAppConfigurator().Of<FakeConfiguration>();

                fakeA.A = "A";
                fakeB.B = "B";
                await Task.WhenAll(repoA.SaveAsync(), repoB.SaveAsync()).ConfigureAwait(false);
                await repo.ReloadExternalChangesAsync().ConfigureAwait(false);

                Assert.AreEqual("A", fake.A);
                Assert.AreEqual("B", fake.B);

                // 因为文件读写已加锁，所以理论上不应存在读写失败。
                try
                {
                    Assert.AreEqual(0, repoA.FileSyncingErrorCount);
                    Assert.AreEqual(0, repoB.FileSyncingErrorCount);
                    Assert.AreEqual(0, repo.FileSyncingErrorCount);
                }
                finally
                {
                    Debug.WriteLine(FormatSyncingCount(repoA, repoB, repo));
                }
            });

            "A 进程和 B 进程同时写一个已存在的配置；随后检查文件，两个配置值均有可能，但一定不是原来的值。".Test(async () =>
            {
                var coin = TestUtil.GetTempFile("configs.coin");
                var repoA = CreateIndependentRepo(coin);
                var repoB = CreateIndependentRepo(coin);
                var repo = CreateIndependentRepo(coin);
                var fakeA = repoA.CreateAppConfigurator().Of<FakeConfiguration>();
                var fakeB = repoB.CreateAppConfigurator().Of<FakeConfiguration>();
                var fake = repo.CreateAppConfigurator().Of<FakeConfiguration>();

                fakeA.Key = "A";
                fakeB.Key = "B";
                await Task.WhenAll(repoA.SaveAsync(), repoB.SaveAsync()).ConfigureAwait(false);
                await repo.ReloadExternalChangesAsync().ConfigureAwait(false);

                Assert.IsTrue(
                    string.Equals(fake.Key, "A", StringComparison.Ordinal)
                    || string.Equals(fake.Key, "B", StringComparison.Ordinal),
                    $"实际值：{fake.Key}。");

                // 因为文件读写已加锁，所以理论上不应存在读写失败。
                try
                {
                    Assert.AreEqual(0, repoA.FileSyncingErrorCount);
                    Assert.AreEqual(0, repoB.FileSyncingErrorCount);
                    Assert.AreEqual(0, repo.FileSyncingErrorCount);
                }
                finally
                {
                    Debug.WriteLine(FormatSyncingCount(repoA, repoB, repo));
                }
            });
        }

        /// <summary>
        /// 控制配置文件读写次数避免过于浪费资源。
        /// </summary>
        [ContractTestCase]
        public void 控制配置文件读写次数避免过于浪费资源()
        {
            "初始化，仅同步一次。".Test(() =>
            {
                var coin = TestUtil.GetTempFile("configs.coin");
                var repo = CreateIndependentRepo(coin);
                var fake = repo.CreateAppConfigurator().Of<FakeConfiguration>();

                Assert.AreEqual("", fake.A);

                try
                {
                    Assert.AreEqual(1, repo.FileSyncingCount);
                    Assert.AreEqual(0, repo.FileSyncingErrorCount);
                    //Assert.AreEqual(0, repo.SyncWaitingCount);
                }
                finally
                {
                    Debug.WriteLine(FormatSyncingCount(repo));
                }
            });

            "初始化后，写入配置，共同步两次。".Test(async () =>
            {
                var coin = TestUtil.GetTempFile("configs.coin");
                var repo = CreateIndependentRepo(coin);
                var fake = repo.CreateAppConfigurator().Of<FakeConfiguration>();
                fake.A = "A";
                await repo.SaveAsync().ConfigureAwait(false);

                try
                {
                    AssertFileSyncingCount(repo, 2, 3);
                    Assert.AreEqual(0, repo.FileSyncingErrorCount);
                    //Assert.AreEqual(0, repo.SyncWaitingCount);
                }
                finally
                {
                    Debug.WriteLine(FormatSyncingCount(repo));
                }
            });

            "初始化后，外部文件改变，共同步两次。".Test(async () =>
            {
                var coin = TestUtil.GetTempFile("configs.coin");
                var repo = CreateIndependentRepo(coin);
                var fake = repo.CreateAppConfigurator().Of<FakeConfiguration>();
                Assert.AreEqual("", fake.A);

                File.WriteAllText(coin.FullName, "> 配置文件\n> 版本 1.0\nKey\nValue\n>\n> 配置文件结束");
                await repo.ReloadExternalChangesAsync().ConfigureAwait(false);

                try
                {
                    AssertFileSyncingCount(repo, 2, 3);
                    Assert.AreEqual(0, repo.FileSyncingErrorCount);
                    //Assert.AreEqual(0, repo.SyncWaitingCount);
                }
                finally
                {
                    Debug.WriteLine(FormatSyncingCount(repo));
                }
            });
        }

        /// <summary>
        /// 当监听的文件在外部发生改变的时候。
        /// </summary>
        [ContractTestCase]
        public void 不监视文件改变()
        {
            "不监听，文件内容发生了改变，也读不到文件的新值。".Test(async () =>
            {
                var coin = TestUtil.GetTempFile("configs.coin");
                var configs = CreateIndependentRepo(coin, RepoSyncingBehavior.Static).CreateAppConfigurator();
                var fake = configs.Of<FakeConfiguration>();
                var oldValue = fake.Key;
                Assert.AreEqual("Value", oldValue);
                File.WriteAllText(coin.FullName, @">
Key
NewValue
>");
                await Task.Delay(500).ConfigureAwait(false);
                var newValue = fake.Key;
                Assert.AreEqual("Value", newValue);
            });

            "不监听，后续文件才被创建，无法读到文件中存放的值。".Test(async () =>
            {
                var coin = TestUtil.GetTempFile(null, ".coin");
                var configs = CreateIndependentRepo(coin, RepoSyncingBehavior.Static).CreateAppConfigurator();
                var fake = configs.Of<FakeConfiguration>();
                var oldValue = fake.Key;
                Assert.AreEqual("", oldValue);
                File.WriteAllText(coin.FullName, @">
Key
NewValue
>");
                await Task.Delay(500).ConfigureAwait(false);
                var newValue = fake.Key;
                Assert.AreEqual("", newValue);
            });

            "不监听，文件被删除，所有的值仍然保留未被删除。".Test(async () =>
            {
                var coin = TestUtil.GetTempFile("configs.coin");
                var configs = CreateIndependentRepo(coin, RepoSyncingBehavior.Static).CreateAppConfigurator();
                var fake = configs.Of<FakeConfiguration>();
                var oldValue = fake.Key;
                Assert.AreEqual("Value", oldValue);
                File.Delete(coin.FullName);
                Thread.Sleep(500);
                await Task.Delay(500).ConfigureAwait(false);
                var newValue = fake.Key;
                Assert.AreEqual("Value", newValue);
            });
        }

        /// <summary>
        /// 断言文件同步次数。
        /// </summary>
        /// <param name="repo">配置仓库。</param>
        /// <param name="countForHighResolutionFileSystem">在支持高精度时间的文件系统上的同步次数（严格相等）。</param>
        /// <param name="countForOtherFileSystems">在不支持高精度时间的文件系统上的同步次数（不大于此值）。</param>
        private static void AssertFileSyncingCount(FileConfigurationRepo repo, int countForHighResolutionFileSystem, int countForOtherFileSystems)
        {
            if (repo.SupportsHighResolutionFileTime)
            {
                // 支持高精度时间的文件系统上，同步次数固定。
                Assert.AreEqual(countForHighResolutionFileSystem, repo.FileSyncingCount);
            }
            else
            {
                // 在不支持高精度时间的文件系统上，同步次数不大于值（多出的一次是因为并发时机发生时，无法确定文件的新旧）。
                Assert.IsTrue(repo.FileSyncingCount <= countForOtherFileSystems);
            }
        }

        /// <summary>
        /// 创建相互独立的 <see cref="FileConfigurationRepo"/> 的实例。
        /// 正常不应该用这种方式创建配置读写，因为这种方式线程不安全；但这里我们要测跨进程的读写安全，所以采用此方式用跨线程访问全来模拟跨进程访问。
        /// </summary>
        /// <param name="file">请使用 <see cref="TestUtil.GetTempFile"/> 获取要传入的文件。</param>
        /// <param name="syncingBehavior">指定应如何读取数据。是实时监听文件变更，还是只读一次，后续不再监听变更。后者性能更好。</param>
        /// <returns>用于读写配置的 <see cref="FileConfigurationRepo"/> 的新实例。</returns>
        private static FileConfigurationRepo CreateIndependentRepo(FileInfo file, RepoSyncingBehavior syncingBehavior = RepoSyncingBehavior.Sync) =>
            new(file.FullName, syncingBehavior);

        private static string FormatSyncingCount(params FileConfigurationRepo[] repos)
        {
            return $"文件同步次数：{string.Join("; ", repos.Select(x => $"{x.FileSyncingCount}({x.FileSyncingErrorCount})"))}";
        }
    }
}
