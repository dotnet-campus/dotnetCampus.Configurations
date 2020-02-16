using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace dotnetCampus.Configurations.Tests
{
    [TestClass]
    public class DefaultConfigurationTests
    {
        [ContractTestCase]
        public void SetValue()
        {
            "可以直接给 DefaultConfiguration 的值给基础类型赋值，不需要做转换".Test(() =>
            {
                // Arrange
                const string dcc = "configs.01.dcc";
                var defaultConfiguration = DefaultConfiguration.FromFile(dcc);

                // Action
                defaultConfiguration["key"] = true;

                // Assert
                Assert.AreEqual(true, defaultConfiguration["key"] != null);
                Assert.AreEqual(true.ToString(CultureInfo.InvariantCulture), defaultConfiguration["key"].ToString());
            });
        }
    }
}