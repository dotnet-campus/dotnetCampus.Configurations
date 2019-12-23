#pragma warning disable CA1716

namespace dotnetCampus.Configurations
{
    /// <summary>
    /// 管理应用程序中 <see cref="Configuration"/> 类型的配置项。
    /// </summary>
    public interface IAppConfigurator
    {
        /// <summary>
        /// 获取 <typeparamref name="TConfiguration"/> 类型的配置项组。
        /// </summary>
        /// <typeparam name="TConfiguration">配置项组的类型。</typeparam>
        /// <returns>配置项组。</returns>
        TConfiguration Of<TConfiguration>() where TConfiguration : Configuration, new();

        /// <summary>
        /// 获取默认的纯字符串值的配置项组。
        /// </summary>
        DefaultConfiguration Default { get; }
    }
}
