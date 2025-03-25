using System;
using System.Globalization;
using System.Runtime.InteropServices;

#pragma warning disable CA2225

namespace dotnetCampus.Configurations
{
    /// <summary>
    /// 表示从 <see cref="Configuration.GetString"/> 中读取出来的配置项字符串的值。
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct ConfigurationString : IEquatable<ConfigurationString>, IEquatable<string>
    {
        private readonly string? _value;

        /// <summary>
        /// 创建 <see cref="ConfigurationString"/> 的新实例。
        /// </summary>
        /// <param name="value">原始字符串的值，允许为 null。</param>
        private ConfigurationString(string value)
        {
            _value = value;
        }

        /// <summary>
        /// 将字符串转换为 <see cref="ConfigurationString"/> 以获取可空值类型和非空字符串两者的使用体验优势。
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator ConfigurationString?(string? value)
        {
            return Convert(value);
        }

        /// <summary>
        /// 内部的获取值方法，用于在内部获取值，过程中没有转换和判断，提升一点性能
        /// </summary>
        /// <returns></returns>
        internal string? InternalGetValue() => _value;

        private static ConfigurationString? Convert(string? value) => value == null || string.IsNullOrEmpty(value) ? (ConfigurationString?)null : new ConfigurationString(value);

        /// <summary>
        /// 调用 <see cref="ToString"/> 方法以便将 <see cref="ConfigurationString"/> 转换为非 null 字符串。
        /// </summary>
        /// <param name="configurationValue">从 <see cref="Configuration.GetString"/> 中读取出来的配置项字符串的值。</param>
        public static implicit operator string(ConfigurationString? configurationValue)
        {
            return configurationValue?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// 转换为非 null 字符串。如果原始值为 null，将得到 <see cref="string.Empty"/>。<para />
        /// 注意：<para />
        ///  - value.ToString() 可以拿到一定非 null 的字符串；<para />
        ///  - value?.ToString() 则可以在字符串为 null/"" 时拿到 null。<para />
        /// </summary>
        /// <returns>非 null 字符串。</returns>
        public override string ToString()
        {
            return _value is null || string.IsNullOrEmpty(_value) ? string.Empty : _value;
        }

        /// <inheritdoc />
        public override bool Equals(object? other)
        {
            if (other is ConfigurationString cs)
            {
                return Equals(cs);
            }

            if (other is string s)
            {
                return Equals(s);
            }

            return false;
        }

        /// <inheritdoc />
        public bool Equals(ConfigurationString other) => _value == other._value;

        /// <inheritdoc />
        public bool Equals(string other) => _value == other;

        /// <inheritdoc />
        public override int GetHashCode() => string.IsNullOrEmpty(_value) ? 0 : StringComparer.Ordinal.GetHashCode(_value);

        /// <summary>
        /// 判断两个 <see cref="ConfigurationString"/> 的字符串值是否相等。
        /// 注意，空字符串和 null 会被视为相等，因为这两者在 <see cref="ConfigurationString"/> 中表示的是相同含义。
        /// </summary>
        public static bool operator ==(ConfigurationString left, ConfigurationString right) => left.Equals(right);

        /// <summary>
        /// 判断两个 <see cref="ConfigurationString"/> 的字符串值是否不相等。
        /// 注意，空字符串和 null 会被视为相等，因为这两者在 <see cref="ConfigurationString"/> 中表示的是相同含义。
        /// </summary>
        public static bool operator !=(ConfigurationString left, ConfigurationString right) => !(left == right);
    }
}
