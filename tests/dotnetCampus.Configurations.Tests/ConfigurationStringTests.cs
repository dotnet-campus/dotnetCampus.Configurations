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
            "支持从 long? 隐式转换为 ConfigurationString? 类".Test(() =>
            {
                // Arrange
                long? value = 10;

                // Action
                ConfigurationString? configurationString = value;

                // Assert
                Assert.AreEqual(true, configurationString != null);
            });

            "支持从 int? 隐式转换为 ConfigurationString? 类".Test(() =>
            {
                // Arrange
                int? value = 10;

                // Action
                ConfigurationString? configurationString = value;

                // Assert
                Assert.AreEqual(true, configurationString != null);
            });


            "支持从 float? 隐式转换为 ConfigurationString? 类".Test(() =>
            {
                // Arrange
                float? value = 10;

                // Action
                ConfigurationString? configurationString = value;

                // Assert
                Assert.AreEqual(true, configurationString != null);
            });

            "支持从 double? 隐式转换为 ConfigurationString? 类".Test(() =>
            {
                // Arrange
                double? value = 10;

                // Action
                ConfigurationString? configurationString = value;

                // Assert
                Assert.AreEqual(true, configurationString != null);
            });

            "支持从 decimal? 隐式转换为 ConfigurationString? 类".Test(() =>
            {
                // Arrange
                decimal? value = 10;

                // Action
                ConfigurationString? configurationString = value;

                // Assert
                Assert.AreEqual(true, configurationString != null);
            });

            "支持从 bool? 隐式转换为 ConfigurationString? 类".Test(() =>
            {
                // Arrange
                bool? value = false;

                // Action
                ConfigurationString? configurationString = value;

                // Assert
                Assert.AreEqual(true, configurationString != null);
            });
        }
    }
}