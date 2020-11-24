using System;

namespace dotnetCampus.Configurations.Core
{
    /// <summary>
    /// 提供一个同步 <see cref="Configuration"/> 配置管理仓库的基类。
    /// </summary>
    public abstract class ConfigurationRepo : IConfigurationRepo
    {
        /// <inheritdoc />
        public IAppConfigurator CreateAppConfigurator() => new ConcurrentAppConfigurator(this);

        /// <inheritdoc />
        public abstract string? GetValue(string key);

        /// <inheritdoc />
        public abstract void SetValue(string key, string? value);

        /// <inheritdoc />
        public abstract void ClearValues(Predicate<string> keyFilter);
    }
}