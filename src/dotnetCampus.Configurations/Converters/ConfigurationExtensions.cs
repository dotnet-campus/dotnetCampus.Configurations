using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace dotnetCampus.Configurations.Converters
{
    /// <summary>
    /// 为 <see cref="Configuration"/> 提供包含配置项值转换的扩展方法。
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// 在 <see cref="Configuration"/> 的派生类中为非基本类型属性的 get 访问器提供获取配置值的方法。
        /// 此方法会返回可空值类型，如果配置项不存在，则指为 null。
        /// </summary>
        /// <typeparam name="T">值类型。</typeparam>
        /// <param name="this">配置项组派生类的实例。</param>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        /// <returns>配置项的值。</returns>
        public static T? GetValue<T>(this Configuration @this, [CallerMemberName] string? key = null)
            where T : struct
        {
            if (@this == null)
            {
                throw new ArgumentNullException(nameof(@this));
            }

            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var type = typeof(T);
            var value = @this.GetValue(key);

            // 如果读取到的配置值为 null，则返回 null。
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            // 如果读取到的配置值不为 null，则尝试转换。
            var typeConverter = TypeDescriptor.GetConverter(type);
            var convertedValue = typeConverter.ConvertFromInvariantString(value);

            if (convertedValue == null)
            {
                // 此分支不可能进入。
                throw new NotSupportedException(
                    $"出现了不可能的情况，从字符串 {value} 转换为值类型 {typeof(T).FullName} 时，转换后的值为 null。");
            }

            return (T) convertedValue;
        }

        /// <summary>
        /// 在派生类中为非基本类型属性的 set 访问器提供设置配置值的方法。
        /// </summary>
        /// <param name="this">需要设置非基本类型值的配置项组。</param>
        /// <param name="value">配置项的值。</param>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue<T>(this Configuration @this,
            T value, [CallerMemberName] string? key = null) where T : struct
            => SetValue(@this, (T?)value, key);

        /// <summary>
        /// 在派生类中为非基本类型属性的 set 访问器提供设置配置值的方法。
        /// </summary>
        /// <param name="this">需要设置非基本类型值的配置项组。</param>
        /// <param name="value">配置项的值。</param>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        public static void SetValue<T>(this Configuration @this,
            T? value, [CallerMemberName] string? key = null) where T : struct
        {
            if (@this == null)
            {
                throw new ArgumentNullException(nameof(@this));
            }

            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var type = typeof(T);

            if (value == null)
            {
                @this.SetValue(string.Empty, key);
            }
            else
            {
                // 如果需要设置的配置值不为 null，则尝试转换。
                var typeConverter = TypeDescriptor.GetConverter(type);
                var convertedValue = typeConverter.ConvertToInvariantString(value.Value);
                if (convertedValue == null)
                {
                    throw new NotSupportedException($"无法从类型 {type.FullName} 将值 {value.Value} 转换成字符串。");
                }

                @this.SetValue(convertedValue, key);
            }
        }

        /// <summary>
        /// 在派生类中为属性的 set 访问器提供设置配置值的方法。
        /// </summary>
        /// <param name="this">需要设置非基本类型值的配置项组。</param>
        /// <param name="value">配置项的值。</param>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        [Obsolete("请改用 DateTimeOffset 类型，因为 DateTime 在存储和传输过程中会丢失时区信息导致值在读写后发生变化。")]
        public static void SetValue(this Configuration @this,
            DateTime value, [CallerMemberName] string? key = null)
            => SetValueCore(@this, key, value.ToString("O", CultureInfo.InvariantCulture));

        /// <summary>
        /// 在派生类中为属性的 set 访问器提供设置配置值的方法。
        /// </summary>
        /// <param name="this">需要设置非基本类型值的配置项组。</param>
        /// <param name="value">配置项的值。</param>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        public static void SetValue(this Configuration @this,
            DateTimeOffset value, [CallerMemberName] string? key = null)
            => SetValueCore(@this, key, value.ToString("O", CultureInfo.InvariantCulture));

        /// <summary>
        /// 在派生类中为属性的 set 访问器提供设置配置值的方法。
        /// </summary>
        /// <param name="this">需要设置非基本类型值的配置项组。</param>
        /// <param name="value">配置项的值。</param>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        public static void SetValue(this Configuration @this,
            DateTimeOffset? value, [CallerMemberName] string? key = null)
            => SetValueCore(@this, key, value?.ToString("O", CultureInfo.InvariantCulture));

        /// <summary>
        /// 在转换器的扩展方法中用于简化设置值的扩展方法。
        /// </summary>
        /// <param name="configs">需要设置非基本类型值的配置项组。</param>
        /// <param name="key">配置项的标识符。</param>
        /// <param name="value">已经转换好的字符串。</param>
        private static void SetValueCore(Configuration configs, string? key, string? value)
        {
            if (configs == null)
            {
                throw new ArgumentNullException(nameof(configs));
            }

            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            configs.SetValue(value, key);
        }
    }
}
