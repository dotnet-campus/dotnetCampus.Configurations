using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;
using ConfigurationManager = Microsoft.Extensions.Configuration.ConfigurationManager;

namespace dotnetCampus.Configurations.MicrosoftExtensionsConfiguration.Tests
{
    [TestClass]
    public class MicrosoftConfigurationExtensions
    {
        [ContractTestCase]
        public void WorkWithJson()
        {
            "通过读取 Json 配置，不会与 AppConfigurator 冲突".Test(() =>
            {
                // Arrange
                var configurationManager = new ConfigurationManager();
                configurationManager.AddJsonFile("appsettings.json");

                // Assert
                var logLevelConfigurationList = configurationManager.GetSection("Logging").GetSection("LogLevel").AsEnumerable(true).ToList();
                Assert.AreEqual(2,logLevelConfigurationList.Count);

                // Act
                // 接着对接上 AppConfigurator 之后，还能正常获取到配置内容
                var appConfigurator = configurationManager.ToAppConfigurator();

                Assert.IsNotNull(appConfigurator);

                // Assert
                // 期望能获取到和没有对接之前一样的值
                logLevelConfigurationList = configurationManager.GetSection("Logging").GetSection("LogLevel").AsEnumerable(true).ToList();
                Assert.AreEqual(2, logLevelConfigurationList.Count);
            });
        }

        [ContractTestCase]
        public void ConfigurationBuilderToAppConfigurator()
        {
            "加入配置构建的内容，可以通过 AppConfigurator 设置，设置之后可以读取到".Test(() =>
            {
                // Arrange
                const string key = "LindexiIsDoubi";
                const string value = "doubi";

                IConfigurationBuilder builder = new ConfigurationBuilder();

                // Act
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
                    InitialData = new List<KeyValuePair<string, string?>>()
                    {
                        new KeyValuePair<string, string?>(key, value)
                    }
                };
                IConfigurationProvider configurationProvider=new MemoryConfigurationProvider(memoryConfigurationSource);
                IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(){ configurationProvider });

                // Act
                var appConfigurator = configuration.ToAppConfigurator();
                var configurationValue = appConfigurator.Default[key];

                // Assert
                Assert.IsNotNull(configurationValue);
                Assert.AreEqual(value, configurationValue.ToString());
            });
        }
    }
}
