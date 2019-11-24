using System;

namespace dotnetCampus.Configurations.Core
{
    internal readonly struct ConfigurationValueEntry
    {
        public ConfigurationValueEntry(string value)
        {
            Value = value;
            _updatedTime = DateTimeOffset.Now;
        }

        public string Value { get; }

        private readonly DateTimeOffset _updatedTime;

        public static ConfigurationValueEntry GetLatest(ConfigurationValueEntry value1, ConfigurationValueEntry value2)
            => value1._updatedTime > value2._updatedTime ? value1 : value2;

        [Obsolete("不含时间戳的字符串无法当作配置项比较新旧。", true)]
        public static ConfigurationValueEntry GetLatest(string value1, string value2)
            => throw new NotSupportedException("不含时间戳的字符串无法当作配置项比较新旧。");

        [Obsolete("不含时间戳的字符串无法当作配置项比较新旧。", true)]
        public static ConfigurationValueEntry GetLatest(string value1, ConfigurationValueEntry value2)
            => throw new NotSupportedException("不含时间戳的字符串无法当作配置项比较新旧。");

        [Obsolete("不含时间戳的字符串无法当作配置项比较新旧。", true)]
        public static ConfigurationValueEntry GetLatest(ConfigurationValueEntry value1, string value2)
            => throw new NotSupportedException("不含时间戳的字符串无法当作配置项比较新旧。");

        public static implicit operator ConfigurationValueEntry(string value) => new ConfigurationValueEntry(value);
        public static implicit operator string(ConfigurationValueEntry entry) => entry.Value;
    }
}
