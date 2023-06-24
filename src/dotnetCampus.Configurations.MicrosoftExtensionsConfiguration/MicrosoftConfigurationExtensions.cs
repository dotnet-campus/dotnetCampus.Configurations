using dotnetCampus.Configurations;
using dotnetCampus.Configurations.MicrosoftExtensionsConfiguration;

// 这里特别设置为 Microsoft.Extensions 命名空间，用于解决引用的时候使用扩展方法需要加上一堆命名空间
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// 配置的扩展方法，用于将 Microsoft.Extensions.Configuration 接入到 dotnetCampus.Configurations 库
    /// </summary>
    public static class MicrosoftConfigurationExtensions
    {
        /// <summary>
        /// 将 <see cref="IConfiguration"/> 转换为 <see cref="IAppConfigurator"/> 的方法
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IAppConfigurator ToAppConfigurator(this IConfiguration configuration) =>
            new MicrosoftExtensionsConfigurationRepo(configuration).CreateAppConfigurator();

        /// <summary>
        /// 将 <see cref="IConfigurationBuilder"/> 转换为 <see cref="IAppConfigurator"/> 的方法
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IAppConfigurator ToAppConfigurator(this IConfigurationBuilder configuration) =>
            new MicrosoftExtensionsConfigurationBuildRepo(configuration).CreateAppConfigurator();

        /// <inheritdoc cref="ToAppConfigurator(IConfigurationBuilder)"/>
        public static IAppConfigurator ToAppConfigurator<T>(this T configuration)
            where T : IConfiguration, IConfigurationBuilder
        {
            IConfigurationBuilder configurationBuilder = configuration;
            return configurationBuilder.ToAppConfigurator();
        }
    }
}
