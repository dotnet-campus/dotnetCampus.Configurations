using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace dotnetCampus.Configurations.Core
{
    /// <summary>
    /// 为了避免同一个进程中对同一个文件的竞争访问，此类型提供线程安全的获取 <see cref="FileConfigurationRepo"/> 的工厂方法。
    /// </summary>
    public static class ConfigurationFactory
    {
        /// <summary>
        /// 管理不同文件的 <see cref="IAppConfigurator"/> 的实例。
        /// </summary>
        private static readonly ConcurrentDictionary<string, WeakReference<FileConfigurationRepo>> Configurations
            = new ConcurrentDictionary<string, WeakReference<FileConfigurationRepo>>();

        /// <summary>
        /// 从文件创建默认的配置管理仓库，你将可以使用类似字典的方式管理线程和进程安全的应用程序配置。
        /// 对于同一个文件，此方法会获取到相同的 <see cref="FileConfigurationRepo"/> 的实例。
        /// <para>此方法是线程安全的。</para>
        /// </summary>
        /// <param name="fileName">来自于本地文件系统的文件名/路径。文件或文件所在的文件夹不需要提前存在。</param>
        /// <returns>一个用于管理指定文件的配置仓库。</returns>
        public static FileConfigurationRepo FromFile(string fileName)
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
        /// 创建 <see cref="IAppConfigurator"/> 的弱引用实例。
        /// 为了保证线程安全，此方法仅能被 <see cref="Configurations"/> 访问。
        /// </summary>
        /// <param name="path">已经过验证的完整文件路径。</param>
        /// <returns><see cref="IAppConfigurator"/> 的弱引用实例。</returns>
        private static WeakReference<FileConfigurationRepo> CreateConfigurationReference(string path)
            => new WeakReference<FileConfigurationRepo>(
                CreateConfiguration(path));

        /// <summary>
        /// 创建 <see cref="IAppConfigurator"/> 的新实例。
        /// 为了保证线程安全，此方法仅能被 <see cref="CreateConfigurationReference"/> 访问。
        /// </summary>
        /// <param name="path">已经过验证的完整文件路径。</param>
        /// <returns><see cref="IAppConfigurator"/> 的新实例。</returns>
        private static FileConfigurationRepo CreateConfiguration(string path)
#pragma warning disable CS0618 // 类型或成员已过时
            => new FileConfigurationRepo(path);
#pragma warning restore CS0618 // 类型或成员已过时

        /// <summary>
        /// 尝试重新加载此配置文件的外部修改（例如使用其他编辑器或其他客户端修改的部分）。
        /// <para>外部修改会自动同步到此配置中，但此同步不会立刻发生，所以如果你明确知道外部修改了文件后需要立刻重新加载外部修改，才需要调用此方法。</para>
        /// </summary>
        public static Task ReloadExternalChangesAsync(this IAppConfigurator configs)
        {
            if (configs is null)
            {
                throw new ArgumentNullException(nameof(configs));
            }

            if (configs.Of<DefaultConfiguration>().Repo is FileConfigurationRepo repo)
            {
                return repo.ReloadExternalChangesAsync();
            }
            return Task.FromResult<object?>(null);
        }
    }
}
