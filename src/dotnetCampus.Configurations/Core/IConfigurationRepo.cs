using System;

namespace dotnetCampus.Configurations.Core
{
    /// <summary>
    /// 管理应用程序中的字符串配置项。
    /// </summary>
    public interface IConfigurationRepo
    {
        /// <summary>
        /// 创建一个使用强类型的用于提供给应用程序业务使用的应用程序配置管理器。
        /// </summary>
        /// <returns>用于提供给应用程序业务使用的配置管理器。</returns>
        IAppConfigurator CreateAppConfigurator();

        /// <summary>
        /// 获取指定配置项的值，如果指定的 <paramref name="key"/> 不存在，则返回 null。
        /// 此方法是线程安全的。
        /// </summary>
        /// <param name="key">配置项的标识符。</param>
        /// <returns>配置项的值。</returns>
        string GetValue(string key);

        /// <summary>
        /// 设置指定配置项的值，如果设置为 null，可能删除 <paramref name="key"/> 配置项。
        /// 此方法是线程安全的。
        /// </summary>
        /// <param name="key">配置项的标识符。</param>
        /// <param name="value">配置项的值。</param>
        void SetValue(string key, string value);

        /// <summary>
        /// 删除所有满足 <paramref name="keyFilter"/> 规则的 Key 所表示的配置项。
        /// </summary>
        /// <param name="keyFilter">
        /// 指定如何过滤 Key。当指定为 null 时，全部清除。
        /// </param>
        void ClearValues(Predicate<string> keyFilter);
    }
}
