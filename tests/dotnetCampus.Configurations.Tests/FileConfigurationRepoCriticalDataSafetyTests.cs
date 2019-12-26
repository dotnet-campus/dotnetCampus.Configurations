using dotnetCampus.Configurations.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace dotnetCampus.Configurations.Tests
{
    /// <summary>
    /// 专门为多个配置文件进程读写同一份配置文件的正确性进行测试。
    /// </summary>
    [TestClass]
    public class FileConfigurationRepoCriticalDataSafetyTests
    {
        /// <summary>
        /// 读写同一份配置文件的进程必须是同样的数据安全模式。
        /// </summary>
        [ContractTestCase]
        public void MustBeTheSameSefetyMode()
        {
            "两个进程都使用不安全方式读写同一份配置文件，不会出现问题。".Test(() =>
            {

            });

            "两个进程都使用安全方式读写同一份配置文件，不会出现问题。".Test(() =>
            {

            });

            "A 进程先使用不安全方式读写配置，B 进程使用安全的方式读写配置，B 进程将抛出异常提示不安全。".Test(() =>
            {

            });

            "A 进程先使用安全方式读写配置，B 进程使用不安全的方式读写配置，B 进程将抛出异常提示会导致 A 不安全。".Test(() =>
            {

            });
        }

        /// <summary>
        /// 进程读写配置文件的安全性级别可以升级或者降级。
        /// </summary>
        [ContractTestCase]
        public void CompatibleSefetyMode()
        {
            "A 进程以优先不安全方式先读写配置，B 进程以安全的方式读写配置，A 进程将自动切换为安全的读写方式。".Test(() =>
            {

            });

            "A 进程以优先不安全方式先读写配置，B 进程以不安全的方式读写配置，A、B 进程都将继续以不安全的方式读写。".Test(() =>
            {

            });

            "A 进程以优先不安全方式先读写配置，B 进程以优先不安全的方式读写配置，A、B 进程都将继续以不安全的方式读写。".Test(() =>
            {

            });

            // 安全的优先级大于不安全的优先级。即只要有一个进程优先安全，那么整体就会采用安全的方式读写。
            "A 进程以优先不安全方式先读写配置，B 进程以优先安全的方式读写配置，A、B 进程都将以安全的方式读写。".Test(() =>
            {

            });

            "A 进程以优先安全方式先读写配置，B 进程以不安全的方式读写配置，A 进程将自动切换为不安全的读写方式。".Test(() =>
            {

            });

            "A 进程以优先安全方式先读写配置，B 进程以安全的方式读写配置，A、B 进程都将继续以安全的方式读写。".Test(() =>
            {

            });

            // 安全的优先级大于不安全的优先级。即只要有一个进程优先安全，那么整体就会采用安全的方式读写。
            "A 进程以优先安全方式先读写配置，B 进程以优先不安全的方式读写配置，A、B 进程都将以安全的方式读写。".Test(() =>
            {

            });

            "A 进程以优先安全方式先读写配置，B 进程以优先安全的方式读写配置，A、B 进程都将继续以安全的方式读写。".Test(() =>
            {

            });
        }

        /// <summary>
        /// 当文件存在时。
        /// </summary>
        [ContractTestCase]
        public void FileExists()
        {
            "两个进程竞争读写，1 进程写入后 2 进程立刻读，可以读到数据。".Test(() =>
            {
                var config1 = new FileConfigurationRepo("exist.dcc").CreateAppConfigurator().Of<FakeConfiguration>();
                var config2 = new FileConfigurationRepo("exist.dcc").CreateAppConfigurator().Of<FakeConfiguration>();

                var value1 = "1";
                config1.Key = value1;
                var value2 = config2.Key;

                Assert.AreEqual(value1, value2);
            });

            "两个进程竞争读写，1、2 进程分别写入后 1、2 进程立刻读，都可以读到数据。".Test(() =>
            {

            });
        }

        /// <summary>
        /// 当文件刚创建时。
        /// </summary>
        [ContractTestCase]
        public void FileCreating()
        {
            "单个进程读写，刚外部创建完带有配置信息的文件后，此进程可以读到数据。".Test(() =>
            {

            });

            "两个进程竞争读写，1 进程写入配置后 2 进程立刻读，可以读到数据。".Test(() =>
            {

            });
        }

        /// <summary>
        /// 当文件被删除时。
        /// </summary>
        [ContractTestCase]
        public void FileDeleting()
        {
            "单个进程读写，刚删除完文件后，此进程所有已保存的数据被清空。".Test(() =>
            {

            });

            "单个进程读写，刚删除完文件后，此进程所有未保存的数据会被写入到文件中。".Test(() =>
            {

            });
        }
    }
}
