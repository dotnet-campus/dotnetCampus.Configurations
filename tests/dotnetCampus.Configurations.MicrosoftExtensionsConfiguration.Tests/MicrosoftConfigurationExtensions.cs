using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace dotnetCampus.Configurations.MicrosoftExtensionsConfiguration.Tests
{
    [TestClass]
    public class MicrosoftConfigurationExtensions
    {
        [ContractTestCase]
        public void ConfigurationBuilderToAppConfigurator()
        {
            "加入配置构建的内容，可以通过 AppConfigurator 设置，设置之后可以读取到".Test(() =>
            {
                // Arrange
                const string key = "LindexiIsDoubi";
                const string value = "doubi";

                IConfigurationBuilder builder = new ConfigurationBuilder();

                // Action
                var appConfigurator = builder.ToAppConfigurator();
                appConfigurator.Default[key] = value;

                var configurationRoot = builder.Build();

                // Assert
                Assert.AreEqual(value, configurationRoot[key]);
            });
        }

        [ContractTestCase]
        public void ConfigurationToAppConfigurator()
        {
            "原本在 IConfiguration 存放的内容，可以通过 AppConfigurator 创建出来的配置读取到".Test(() =>
            {
                // Arrange
                const string key = "LindexiIsDoubi";
                const string value = "doubi";
                var memoryConfigurationSource = new MemoryConfigurationSource()
                {
                    InitialData = new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>(key, value)
                    }
                };
                IConfigurationProvider configurationProvider=new MemoryConfigurationProvider(memoryConfigurationSource);
                IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(){ configurationProvider });

                // Action
                var appConfigurator = configuration.ToAppConfigurator();
                var configurationValue = appConfigurator.Default[key];

                // Assert
                Assert.IsNotNull(configurationValue);
                Assert.AreEqual(value, configurationValue.ToString());
            });

        }
    }
}
