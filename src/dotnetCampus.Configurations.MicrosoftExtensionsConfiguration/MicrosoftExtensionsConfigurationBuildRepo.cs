using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using dotnetCampus.Configurations.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace dotnetCampus.Configurations.MicrosoftExtensionsConfiguration
{
    internal class MicrosoftExtensionsConfigurationBuildRepo : ConfigurationRepo
    {
        public MicrosoftExtensionsConfigurationBuildRepo(IConfigurationBuilder configuration)
        {
            configuration.Add(new ConfigurationSource(_concurrentDictionary));
        }

        public override string? GetValue(string key)
        {
            if (_concurrentDictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            return null;
        }

        public override void SetValue(string key, string? value)
        {
            if (value is null)
            {
                _concurrentDictionary.TryRemove(key, out _);
            }
            else
            {
                _concurrentDictionary[key] = value;
            }
        }

        public override void ClearValues(Predicate<string> keyFilter)
        {
            foreach (var key in _concurrentDictionary.Keys)
            {
                if (keyFilter(key))
                {
                    _concurrentDictionary.TryRemove(key, out _);
                }
            }
        }

        private readonly ConcurrentDictionary<string, string> _concurrentDictionary =
            new ConcurrentDictionary<string, string>();

        class ConfigurationSource : IConfigurationSource
        {
            public ConfigurationSource(ConcurrentDictionary<string, string> concurrentDictionary)
            {
                _concurrentDictionary = concurrentDictionary;
            }

            private readonly ConcurrentDictionary<string, string> _concurrentDictionary;

            public IConfigurationProvider Build(IConfigurationBuilder builder)
            {
                return new MemoryConfigurationProvider(_concurrentDictionary);
            }
        }

        class MemoryConfigurationProvider : IConfigurationProvider
        {
            public MemoryConfigurationProvider(ConcurrentDictionary<string, string>? concurrentDictionary = null)
            {
                _concurrentDictionary = concurrentDictionary ?? new ConcurrentDictionary<string, string>();
            }

            private readonly ConcurrentDictionary<string, string> _concurrentDictionary;

            public bool TryGet(string key, out string value)
            {
                return _concurrentDictionary.TryGetValue(key, out value!);
            }

            public void Set(string key, string value)
            {
                _concurrentDictionary[key] = value;
            }

            public IChangeToken GetReloadToken()
            {
                return new ChangeToken();
            }

            class ChangeToken : IChangeToken
            {
                public IDisposable RegisterChangeCallback(Action<object> callback, object state) =>
                    new EmptyDisposable();

                public bool HasChanged => false;
                public bool ActiveChangeCallbacks => false;

                class EmptyDisposable : IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
            }

            public void Load()
            {
            }

            public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
            {
                // 这里如果返回空集合，将会清空原有的配置内容。这个函数的作用是用来进行过滤和追加两个合一起，底层框架这样设计是为了性能考虑。在一个配置管理里面，是由多个 IConfigurationProvider 组成，而多个 IConfigurationProvider 之间，需要有相互影响。在获取所有的 GetChildKeys 时候，假定每个 IConfigurationProvider 都需要追加自身的，那传入 IEnumerable 类型，用于追加是最省资源的。而有些 IConfigurationProvider 之间提供了相同的 Key 的配置，但是有些 IConfigurationProvider 期望覆盖，有些期望不覆盖，于是就通过 earlierKeys 即可用来实现过滤判断，每个不同的 IConfigurationProvider 可以有自己的策略，对先加入的 IConfigurationProvider 返回的 GetChildKeys 进行处理
                // return Array.Empty<string>();
                return earlierKeys;
            }
        }
    }
}