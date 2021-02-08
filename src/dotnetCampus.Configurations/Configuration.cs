using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using dotnetCampus.Configurations.Core;

#pragma warning disable CA1724

namespace dotnetCampus.Configurations
{
    /// <summary>
    /// 包含一组配置项。
    /// 为所有的应用程序配置项提供与 <see cref="IAppConfigurator"/> 的交互。
    /// 派生类继承此基类时，添加属性以存储配置。
    /// </summary>
    public abstract class Configuration
    {
        /// <summary>
        /// 创建 <see cref="Configuration"/> 类型的新实例。
        /// </summary>
        protected Configuration()
        {
            var name = this.GetClassNameWithoutSuffix();
            _section = name + ".";
        }

        /// <summary>
        /// 创建 <see cref="Configuration"/> 类型的新实例，当存储值时，其前准为 <paramref name="sectionName"/>。
        /// </summary>
        protected Configuration(string? sectionName)
            => _section = string.IsNullOrEmpty(sectionName) ? "" : $"{sectionName}.";

        /// <summary>
        /// 在派生类中为属性的 get 访问器提供获取配置值的方法。
        /// 此方法会返回可空值类型，如果配置项不存在或不是布尔类型，则值为 null。
        /// </summary>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        /// <returns>配置项的值。</returns>
        protected bool? GetBoolean([CallerMemberName] string? key = null)
        {
            var value = GetValue(key);
            return !string.IsNullOrWhiteSpace(value)
                && bool.TryParse(value, out var result)
                ? result
                : (bool?)null;
        }

        /// <summary>
        /// 在派生类中为属性的 get 访问器提供获取配置值的方法。
        /// 此方法会返回可空值类型，如果配置项不存在或不是数值类型，则值为 null。
        /// </summary>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        /// <returns>配置项的值。</returns>
        protected decimal? GetDecimal([CallerMemberName] string? key = null)
        {
            var value = GetValue(key);
            return !string.IsNullOrWhiteSpace(value)
                && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : (decimal?)null;
        }

        /// <summary>
        /// 在派生类中为属性的 get 访问器提供获取配置值的方法。
        /// 此方法会返回可空值类型，如果配置项不存在或不是数值类型，则值为 null。
        /// </summary>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        /// <returns>配置项的值。</returns>
        protected double? GetDouble([CallerMemberName] string? key = null)
        {
            var value = GetValue(key);
            return !string.IsNullOrWhiteSpace(value)
                && double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : (double?)null;
        }

        /// <summary>
        /// 在派生类中为属性的 get 访问器提供获取配置值的方法。
        /// 此方法会返回可空值类型，如果配置项不存在或不是数值类型，则值为 null。
        /// </summary>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        /// <returns>配置项的值。</returns>
        protected float? GetSingle([CallerMemberName] string? key = null)
        {
            var value = GetValue(key);
            return !string.IsNullOrWhiteSpace(value)
                && float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : (float?)null;
        }

        /// <summary>
        /// 在派生类中为属性的 get 访问器提供获取配置值的方法。
        /// 此方法会返回可空值类型，如果配置项不存在或不是整数类型，则值为 null。
        /// </summary>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        /// <returns>配置项的值。</returns>
        protected int? GetInt32([CallerMemberName] string? key = null)
        {
            var value = GetValue(key);
            return !string.IsNullOrWhiteSpace(value)
                && int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : (int?)null;
        }

        /// <summary>
        /// 在派生类中为属性的 get 访问器提供获取配置值的方法。
        /// 此方法会返回可空值类型，如果配置项不存在或不是整数类型，则值为 null。
        /// </summary>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        /// <returns>配置项的值。</returns>
        protected long? GetInt64([CallerMemberName] string? key = null)
        {
            var value = GetValue(key);
            return !string.IsNullOrWhiteSpace(value)
                && long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : (long?)null;
        }

        /// <summary>
        /// 在派生类中为属性的 get 访问器提供获取配置值的方法。
        /// 此方法会返回可空值类型，如果配置项不存在或不是指定的枚举类型，则值为 null。
        /// </summary>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        /// <returns>配置项的值。</returns>
        protected T? GetEnum<T>([CallerMemberName] string? key = null)
            where T : struct, IConvertible
        {
            var value = GetValue(key);
            return !string.IsNullOrWhiteSpace(value)
                && Enum.TryParse<T>(value, out var result)
                ? result
                : (T?)null;
        }

        /// <summary>
        /// 在派生类中为属性的 get 访问器提供获取配置值的方法。<para/>
        /// 如果配置中没有储存值：<para/>
        /// 1. 如果你当作字符串使用，则会获取到 <see cref="string.Empty"/>，即不可能为 null；<para/>
        /// 2. 你也可以使用 ?? 操作符以便指定默认值，写法形如 `get => GetString() ?? "DefaultValue"`。
        /// 假设你只打算使用字符串，请调用 `ToString()` 方法，这将返回一个永不为 null 的字符串（<see cref="string.Empty"/> 代表默认值）。
        /// </summary>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        /// <returns>配置项的值。</returns>
        protected ConfigurationString? GetString([CallerMemberName] string? key = null)
            => GetValue(key);

        /// <summary>
        /// 在派生类中为属性的 get 访问器提供获取配置值的方法。
        /// 如果指定配置项的值不存在，则返回空字符串。
        /// </summary>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        /// <returns>配置项的值。</returns>
        internal string? GetValue([CallerMemberName] string? key = null)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (Repo == null)
            {
                throw new InvalidOperationException($"必须通过 {nameof(IAppConfigurator)}.Of 使用配置。");
            }

            var value = Repo.GetValue($"{_section}{key}");
            return value;
        }

        /// <summary>
        /// 在派生类中为属性的 set 访问器提供设置配置值的方法。
        /// </summary>
        /// <param name="value">配置项的值。</param>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        protected void SetValue(bool? value, [CallerMemberName] string? key = null)
            => SetValue(value?.ToString(CultureInfo.InvariantCulture) ?? "", key);

        /// <summary>
        /// 在派生类中为属性的 set 访问器提供设置配置值的方法。
        /// </summary>
        /// <param name="value">配置项的值。</param>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        protected void SetValue(decimal? value, [CallerMemberName] string? key = null)
            => SetValue(value?.ToString(CultureInfo.InvariantCulture) ?? "", key);

        /// <summary>
        /// 在派生类中为属性的 set 访问器提供设置配置值的方法。
        /// </summary>
        /// <param name="value">配置项的值。</param>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        protected void SetValue(double? value, [CallerMemberName] string? key = null)
            => SetValue(value?.ToString(CultureInfo.InvariantCulture) ?? "", key);

        /// <summary>
        /// 在派生类中为属性的 set 访问器提供设置配置值的方法。
        /// </summary>
        /// <param name="value">配置项的值。</param>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        protected void SetValue(float? value, [CallerMemberName] string? key = null)
            => SetValue(value?.ToString(CultureInfo.InvariantCulture) ?? "", key);

        /// <summary>
        /// 在派生类中为属性的 set 访问器提供设置配置值的方法。
        /// </summary>
        /// <param name="value">配置项的值。</param>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        protected void SetValue(int? value, [CallerMemberName] string? key = null)
            => SetValue(value?.ToString(CultureInfo.InvariantCulture) ?? "", key);

        /// <summary>
        /// 在派生类中为属性的 set 访问器提供设置配置值的方法。
        /// </summary>
        /// <param name="value">配置项的值。</param>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        protected void SetValue(long? value, [CallerMemberName] string? key = null)
            => SetValue(value?.ToString(CultureInfo.InvariantCulture) ?? "", key);

        /// <summary>
        /// 在派生类中为属性的 set 访问器提供设置配置值的方法。
        /// </summary>
        /// <param name="value">配置项的值。</param>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        protected void SetEnum<T>(T? value, [CallerMemberName] string? key = null)
            where T : struct, IConvertible
            => SetValue(value?.ToString() ?? "", key);

        /// <summary>
        /// 在派生类中为属性的 set 访问器提供设置配置值的方法。
        /// 不允许 <paramref name="value"/> 为 null；如果需要存入空值，请使用 <see cref="string.Empty"/>。
        /// </summary>
        /// <param name="value">配置项的值。</param>
        /// <param name="key">配置项的标识符，自动从属性名中获取。</param>
        protected internal void SetValue(ConfigurationString? value, [CallerMemberName] string? key = null)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (Repo == null)
            {
                throw new InvalidOperationException($"必须通过 {nameof(IAppConfigurator)}.Of 使用配置。");
            }

            // value.ToString() 可以拿到一定非 null 的字符串；
            // value?.ToString() 则可以在字符串为 null/"" 时拿到 null。
            Repo.SetValue($"{_section}{key}", value?.ToString());
        }

        /// <summary>
        /// 清除此类型的所有配置项。此方法调用后，此类型中的所有属性将被设为默认值。
        /// </summary>
        protected void ClearValues()
        {
            if (Repo == null)
            {
                throw new InvalidOperationException($"必须通过 {nameof(IAppConfigurator)}.Of 使用配置。");
            }

            Repo.ClearValues(key => key.StartsWith(_section, StringComparison.InvariantCulture));
        }

        /// <summary>
        /// 获取配置属性标识符的类型前缀。
        /// </summary>
        private readonly string _section;

        /// <summary>
        /// 获取用于管理应用程序字符串配置项的管理器。
        /// </summary>
        internal IConfigurationRepo? Repo { get; set; }
    }
}
