using System;
using System.Collections.Generic;
using System.Linq;
using dotnetCampus.Configurations.Core;
using dotnetCampus.Configurations.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MSTest.Extensions.Contracts;

namespace dotnetCampus.Configurations.Tests
{
    [TestClass]
    public class ConfigurationTests
    {
        [ContractTestCase]
        public void GetValueAndGetValue()
        {
            "综合测试，用于断点调试配置项执行流程。".Test(() =>
            {
                // Arrange
                var dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
                var configuration = CreateConfiguration(
                    key => dictionary.TryGetValue(key, out var value) ? value : string.Empty,
                    (key, value) => dictionary[key] = value);
                var fake = configuration.Of<DebugConfiguration>();

                // Act
                var isTested = fake.IsTested;
                var amount = fake.Amount;
                var offsetX = fake.OffsetX;
                var sizeX = fake.SizeX;
                var count = fake.Count;
                var length = fake.Length;
                var message = fake.Message;
                //var bounds = fake.Bounds;
                //var color = fake.Color;

                // Assert
                Assert.AreEqual(null, isTested);
                Assert.AreEqual(null, amount);
                Assert.AreEqual(null, offsetX);
                Assert.AreEqual(null, sizeX);
                Assert.AreEqual(null, count);
                Assert.AreEqual(null, length);
                Assert.AreEqual("", message);
                //Assert.AreEqual(new Rect(10, 20, 100, 200), bounds);
                //Assert.AreEqual(null, color);
                Assert.AreEqual(0, dictionary.Count);

                // Act
                fake.IsTested = true;
                fake.Amount = 123.51243634523452345123514251m;
                fake.OffsetX = 100;
                fake.SizeX = 69.5f;
                fake.Count = 50;
                fake.Length = 1230004132413241;
                fake.Message = "ABC";
                //fake.Bounds = new Rect(10, 20, 200, 100);
                //fake.Color = Colors.Teal;

                // Assert
                Assert.AreEqual(true, fake.IsTested);
                Assert.AreEqual(123.51243634523452345123514251m, fake.Amount);
                Assert.AreEqual(100, fake.OffsetX);
                Assert.AreEqual(69.5f, fake.SizeX);
                Assert.AreEqual(50, fake.Count);
                Assert.AreEqual(1230004132413241, fake.Length);
                Assert.AreEqual("ABC", fake.Message);
                //Assert.AreEqual(new Rect(10, 20, 200, 100), fake.Bounds);
                Assert.AreEqual("True", dictionary["Debug.IsTested"]);
                Assert.AreEqual("123.51243634523452345123514251", dictionary["Debug.Amount"]);
                Assert.AreEqual("100", dictionary["Debug.OffsetX"]);
                Assert.AreEqual("69.5", dictionary["Debug.SizeX"]);
                Assert.AreEqual("50", dictionary["Debug.Count"]);
                Assert.AreEqual("1230004132413241", dictionary["Debug.Length"]);
                Assert.AreEqual("ABC", dictionary["Debug.Message"]);
                //Assert.AreEqual("10,20,200,100", dictionary["Debug.Bounds"]);
                //Assert.AreEqual("#FF008080", dictionary["Debug.Color"]);

                // Act
                //fake.Bounds = new Rect(10, 20, 100, 200);
                //Assert.AreEqual(null, dictionary["Debug.Bounds"]);
            });

            "不允许使用 new 创建 Configuration，如果使用，则后续使用抛出异常。".Test(() =>
            {
                // Arrange
                var configuration = new DebugConfiguration();

                // Act & Assert
                // ReSharper disable once ExplicitCallerInfoArgument
                Assert.ThrowsException<InvalidOperationException>(() => configuration.GetValue("Foo"));
                // ReSharper disable once ExplicitCallerInfoArgument
                Assert.ThrowsException<InvalidOperationException>(() => configuration.SetValue("Bar", "Foo"));
            });

            "默认的配置不带前缀。".Test(() =>
            {
                // Arrange
                var dictionary = new Dictionary<string, string>();
                var configuration = CreateConfiguration(
                    key => dictionary.TryGetValue(key, out var value) ? value : string.Empty,
                    (key, value) => dictionary[key] = value);
                var @default = configuration.Default;

                // Act
                string defaultFoo = @default["Foo"];
                @default["Foo"] = "Bar";

                // Assert
                Assert.AreEqual("", defaultFoo);
                Assert.AreEqual("Bar", dictionary["Foo"]);
            });

            "同一份配置组只会有一个实例。".Test(() =>
            {
                // Arrange
                // ReSharper disable once RedundantArgumentDefaultValue
                // ReSharper disable once RedundantArgumentDefaultValue
                var configuration = CreateConfiguration(null, null);

                // Act
                var @default0 = configuration.Of<DefaultConfiguration>();
                var @default1 = configuration.Of<DefaultConfiguration>();

                // Assert
                Assert.AreSame(default0, default1);
            });

            "配置允许引用类型存入 null 值。".Test(() =>
            {
                // Arrange
                var configuration = CreateConfiguration();
                var @default = configuration.Default;

                // Act & Assert
                @default["Foo"] = null;
            });

            "配置允许值类型存入 null 值，取出时也为 null 值。".Test(() =>
            {
                // Arrange
                var dictionary = new Dictionary<string, string>();
                var configuration = CreateConfiguration(
                    key => dictionary.TryGetValue(key, out var value) ? value : string.Empty,
                    (key, value) => dictionary[key] = value);
                var fake = configuration.Of<DebugConfiguration>();

                // Act
                fake.OffsetX = null;

                // Assert
                Assert.AreEqual(null, fake.OffsetX);
            });
        }

        [ContractTestCase]
        public void GetStringWithDefaultValue()
        {
            "如果获取 String 时，获取到了空字符串，那么可以通过 ?? 转换成默认值。".Test(() =>
            {
                // Arrange
                var configuration = CreateConfiguration(key => string.Empty);
                var fake = configuration.Of<DebugConfiguration>();

                // Act
                var host = fake.Host;

                // Assert
                Assert.AreEqual("https://localhost:17134", host);
            });
        }

        [ContractTestCase]
        public void ClearValues()
        {
            "如果清除了配置项，那么之前存的所有键值就都恢复默认值。".Test(() =>
            {
                // Arrange
                var dictionary = new Dictionary<string, string>();
                var configuration = CreateConfiguration(
                    key => dictionary.TryGetValue(key, out var value) ? value : string.Empty,
                    (key, value) => dictionary[key] = value,
                    keyFilter => RemoveKeys(dictionary, keyFilter));
                dictionary["遗留项"] = "随便";

                // Act
                var fake = configuration.Of<DebugConfiguration>();
                fake.Length = 100;
                fake.Count = 2;

                // Assert
                Assert.AreEqual(3, dictionary.Count);

                // Act
                fake.Clear();

                // Assert
                Assert.AreEqual(null, fake.Length);
                Assert.AreEqual(null, fake.Count);
            });
        }

        /// <summary>
        /// 初始化一个 <see cref="IAppConfigurator"/> 的模拟实例。
        /// </summary>
        /// <param name="getValue">模拟 <see cref="IConfigurationRepo.GetValue"/> 方法。</param>
        /// <param name="setValue">模拟 <see cref="IConfigurationRepo.SetValue"/> 方法。</param>
        /// <param name="clearValues">模拟 <see cref="IConfigurationRepo.ClearValues"/> 方法。</param>
        /// <returns><see cref="IAppConfigurator"/> 的模拟实例。</returns>
        private IAppConfigurator CreateConfiguration(
            Func<string, string> getValue = null, Action<string, string> setValue = null,
            Action<Predicate<string>> clearValues = null)
        {
            var managerMock = new Mock<IConfigurationRepo>();
            managerMock.Setup(m => m.CreateAppConfigurator())
                .Returns(new ConcurrentAppConfigurator(managerMock.Object));
            managerMock.Setup(m => m.GetValue(It.IsAny<string>()))
                .Returns<string>(key => getValue?.Invoke(key));
            managerMock.Setup(m => m.SetValue(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((key, value) => setValue?.Invoke(key, value));
            managerMock.Setup(m => m.ClearValues(It.IsAny<Predicate<string>>()))
                .Callback<Predicate<string>>(keyFilter => clearValues?.Invoke(keyFilter));
            return managerMock.Object.CreateAppConfigurator();
        }

        private static void RemoveKeys(Dictionary<string, string> dictionary, Predicate<string> keyFilter)
        {
            if (keyFilter == null)
            {
                dictionary.Clear();
                return;
            }

            foreach (var key in dictionary.Keys.Where(keyFilter.Invoke).ToList())
            {
                dictionary.Remove(key);
            }
        }
    }
}
