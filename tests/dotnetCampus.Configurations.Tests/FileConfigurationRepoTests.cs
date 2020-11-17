﻿using dotnetCampus.Configurations.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotnetCampus.Configurations.Tests
{
    [TestClass]
    public class FileConfigurationRepoTests
    {
        /// <summary>
        /// 当监听的文件在外部发生改变的时候。
        /// </summary>
        [ContractTestCase]
        public void 监视文件改变()
        {
//            "监听后，文件内容发生了改变，可以读到文件的新值。".Test(async () =>
//            {
//                const string dcc = "configs.01.dcc";
//                IAppConfigurator configs = ConfigurationFactory.FromFile(dcc).CreateAppConfigurator();
//                var fake = configs.Of<FakeConfiguration>();
//                var oldValue = fake.Key;
//                Assert.AreEqual("Value", oldValue);
//                File.WriteAllText(dcc, @">
//Key
//NewValue
//>");
//                await configs.ReloadExternalChangesAsync().ConfigureAwait(false);
//                var newValue = fake.Key;
//                Assert.AreEqual("NewValue", newValue);
//            });

//            "监听后，文件才被创建，可以读到文件中一开始就存放的值。".Test(async () =>
//            {
//                const string dcc = "configs.new.dcc";
//                IAppConfigurator configs = ConfigurationFactory.FromFile(dcc).CreateAppConfigurator();
//                var fake = configs.Of<FakeConfiguration>();
//                var oldValue = fake.Key;
//                Assert.AreEqual("", oldValue);
//                File.WriteAllText(dcc, @">
//Key
//NewValue
//>");
//                await configs.ReloadExternalChangesAsync().ConfigureAwait(false);
//                var newValue = fake.Key;
//                Assert.AreEqual("NewValue", newValue);
//            });

            "监听后，文件被删除，相当于所有未保存的值全部被删除。".Test(async () =>
            {
                const string dcc = "configs.02.dcc";
                IAppConfigurator configs = ConfigurationFactory.FromFile(dcc).CreateAppConfigurator();
                var fake = configs.Of<FakeConfiguration>();
                var oldValue = fake.Key;
                Assert.AreEqual("Value", oldValue);
                File.Delete(dcc);
                await configs.ReloadExternalChangesAsync().ConfigureAwait(false);
                var newValue = fake.Key;
                Assert.AreEqual("", newValue);
            });
        }

        /// <summary>
        /// 当监听的文件在外部发生改变的时候。
        /// </summary>
        [ContractTestCase]
        public void 多进程同时读写配置不丢失()
        {
            "A 进程写 A 配置，同时 B 进程写 B 配置；随后检查文件，两个配置均在。".Test(async () =>
            {
                const string dcc = "configs.03.dcc";
                var repoA = CreateIndependentRepo(dcc);
                var repoB = CreateIndependentRepo(dcc);
                var configs = CreateIndependentRepo(dcc).CreateAppConfigurator();
                var fakeA = repoA.CreateAppConfigurator().Of<FakeConfiguration>();
                var fakeB = repoB.CreateAppConfigurator().Of<FakeConfiguration>();
                var fake = configs.Of<FakeConfiguration>();

                fakeA.A = "A";
                fakeB.B = "B";
                await Task.WhenAll(repoA.SaveAsync(), repoB.SaveAsync()).ConfigureAwait(false);
                await configs.ReloadExternalChangesAsync().ConfigureAwait(false);

                Assert.AreEqual("A", fake.A);
                Assert.AreEqual("B", fake.B);
            });
        }

        /// <summary>
        /// 创建相互独立的 <see cref="FileConfigurationRepo"/> 的实例。
        /// 正常不应该用这种方式创建配置读写，因为这种方式线程不安全；但这里我们要测跨进程的读写安全，所以采用此方式用跨线程访问全来模拟跨进程访问。
        /// </summary>
        /// <param name="fileName">文件名，相对于测试路径。</param>
        /// <returns>用于读写配置的 <see cref="FileConfigurationRepo"/> 的新实例。</returns>
        private FileConfigurationRepo CreateIndependentRepo(string fileName) =>
#pragma warning disable CS0618 // 类型或成员已过时
            new FileConfigurationRepo(fileName);
#pragma warning restore CS0618 // 类型或成员已过时
    }
}
