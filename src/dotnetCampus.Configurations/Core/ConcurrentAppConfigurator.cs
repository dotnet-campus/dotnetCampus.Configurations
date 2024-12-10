using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace dotnetCampus.Configurations.Core
{
    /// <summary>
    /// 以线程安全的方式管理应用程序中 <see cref="Configuration"/> 类型的配置项。
    /// </summary>
    internal sealed class ConcurrentAppConfigurator : IAppConfigurator
    {
        /// <summary>
        /// 创建使用 <paramref name="repo"/> 管理的线程安全的应用程序配置。
        /// </summary>
        /// <param name="repo">配置管理器。</param>
        internal ConcurrentAppConfigurator(IConfigurationRepo repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        private readonly IConfigurationRepo _repo;

        private readonly ConcurrentDictionary<Type, Configuration> _configurationDictionary
            = new ConcurrentDictionary<Type, Configuration>();

        /// <summary>
        /// 获取 <typeparamref name="TConfiguration"/> 类型的配置项组。
        /// </summary>
        /// <typeparam name="TConfiguration">配置项组的类型。</typeparam>
        /// <returns>配置项组。</returns>
        public TConfiguration Of<TConfiguration>() where TConfiguration : Configuration, new()
        {
            return (TConfiguration) _configurationDictionary.GetOrAdd(typeof(TConfiguration), _ =>
            {
                return new TConfiguration
                {
                    Repo = _repo
                };
            });
        }

        /// <summary>
        /// 获取默认的纯字符串值的配置项组。
        /// </summary>
        public DefaultConfiguration Default => Of<DefaultConfiguration>();
    }
}
