using dotnetCampus.Configurations.Core;
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
        public void WhenFileChanged()
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

            "监听后，文件才被创建，可以读到文件中一开始就存放的值。".Test(async () =>
            {
                const string dcc = "configs.new.dcc";
                IAppConfigurator configs = ConfigurationFactory.FromFile(dcc).CreateAppConfigurator();
                var fake = configs.Of<FakeConfiguration>();
                var oldValue = fake.Key;
                Assert.AreEqual("", oldValue);
                File.WriteAllText(dcc, @">
Key
NewValue
>");
                await configs.ReloadExternalChangesAsync().ConfigureAwait(false);
                var newValue = fake.Key;
                Assert.AreEqual("NewValue", newValue);
            });

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
    }
}
