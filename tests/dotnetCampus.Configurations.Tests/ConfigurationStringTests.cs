using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace dotnetCampus.Configurations.Tests
{
    [TestClass]
    public class ConfigurationStringTests
    {
        [ContractTestCase]
        public void Convert()
        {
            "支持从 string 隐式转换为 ConfigurationString? 类".Test(() =>
            {
                // Arrange
                string value = "lindexi";

                // Action
                ConfigurationString? configurationString = value;

                // Assert
                Assert.AreEqual(true, configurationString != null);
            });
        }
    }
}