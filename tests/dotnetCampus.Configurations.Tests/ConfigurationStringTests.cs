using dotnetCampus.Configurations.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace dotnetCampus.Configurations.Tests
{
    [TestClass]
    public class ConfigurationStringTests
    {
        /// <summary>
        /// 用于存储提供给单元测试的信息。
        /// </summary>
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            CT.OutputRedirector = text => TestContext.WriteLine(text);
        }

        [ContractTestCase]
        public void Convert()
        {
            "支持从 string 隐式转换为 ConfigurationString? 类".Test(() =>
            {
                // Arrange
                string value = "lindexi";

                // Act
                ConfigurationString? configurationString = value;

                // Assert
                Assert.AreEqual(true, configurationString != null);
            });
        }
    }
}