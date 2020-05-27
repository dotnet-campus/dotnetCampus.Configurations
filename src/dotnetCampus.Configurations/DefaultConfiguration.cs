#nullable enable
using System;
using System.Collections.Concurrent;
using System.IO;
using dotnetCampus.Configurations.Core;

namespace dotnetCampus.Configurations
{
    /// <summary>
    /// 为所有的应用程序配置项提供与 <see cref="IAppConfigurator"/> 的交互。
    /// 派生类继承此基类时，添加属性以存储配置。
    /// </summary>
    public sealed class DefaultConfiguration : Configuration
    {
        /// <summary>
        /// 创建用于给 <see cref="IAppConfigurator"/> 管理配置的默认配置。
        /// </summary>
        public DefaultConfiguration() : base(null)
        {
        }

        /// <summary>
        /// 获取用标识符描述的配置项的字符串值。
        /// <para>在获取值时，会得到可当作字符串使用的值，你可以直接将其赋值给需要字符串类型的属性上，而且你永远不会得到值为 null 的字符串。</para>
        /// <para>另外，由于这是一个可空结构体，所以你也可以直接通过 is null 或者 == null 来判断此值是否没有被设置过，或者使用 ?? 空传递运算符来指定默认值。</para>
        /// <para>在设置值时，你可以直接将一个字符串或者 null 设置进来而不会出现异常。</para>
        /// </summary>
        /// <param name="key">配置项的标识符。</param>
        /// <returns>配置项的值。</returns>
        public ConfigurationString? this[string key]
        {
            get => GetString(key);
            set => SetValue(value, key);
        }

        /// <summary>
        /// 管理不同文件的 <see cref="DefaultConfiguration"/> 的实例。
        /// </summary>
        private static readonly ConcurrentDictionary<string, WeakReference<DefaultConfiguration>> Configurations
            = new ConcurrentDictionary<string, WeakReference<DefaultConfiguration>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 从文件创建默认的配置管理器，你将可以使用类似字典的方式管理线程和进程安全的应用程序配置。
        /// 对于同一个文件，此方法会获取到相同的 <see cref="DefaultConfiguration"/> 的实例。
        /// <para>此方法是线程安全的。</para>
        /// </summary>
        /// <param name="fileName">来自于本地文件系统的文件名/路径。文件或文件所在的文件夹不需要提前存在。</param>
        /// <returns>一个默认的应用程序配置。</returns>
        public static DefaultConfiguration FromFile(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("文件名不能使用空字符串。", nameof(fileName));
            }

            var path = Path.GetFullPath(fileName);
            var reference = Configurations.GetOrAdd(path, CreateConfigurationReference);

            // 以下两个 if 一个 lock 是类似于单例模式的创建方式，既保证性能又保证只创建一次。
            if (!reference.TryGetTarget(out var config))
            {
                lock (reference)
                {
                    if (!reference.TryGetTarget(out config))
                    {
                        config = CreateConfiguration(path);
                        reference.SetTarget(config);
                    }
                }
            }

            return config;
        }

        /// <summary>
        /// 创建 <see cref="DefaultConfiguration"/> 的弱引用实例。
        /// 为了保证线程安全，此方法仅能被 <see cref="Configurations"/> 访问。
        /// </summary>
        /// <param name="path">已经过验证的完整文件路径。</param>
        /// <returns><see cref="DefaultConfiguration"/> 的弱引用实例。</returns>
        private static WeakReference<DefaultConfiguration> CreateConfigurationReference(string path)
            => new WeakReference<DefaultConfiguration>(
                CreateConfiguration(path));

        /// <summary>
        /// 创建 <see cref="DefaultConfiguration"/> 的新实例。
        /// 为了保证线程安全，此方法仅能被 <see cref="CreateConfigurationReference"/> 访问。
        /// </summary>
        /// <param name="path">已经过验证的完整文件路径。</param>
        /// <returns><see cref="DefaultConfiguration"/> 的新实例。</returns>
        private static DefaultConfiguration CreateConfiguration(string path)
            => ConfigurationFactory.FromFile(path).CreateAppConfigurator().Of<DefaultConfiguration>();
    }
}
