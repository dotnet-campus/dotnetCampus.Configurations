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
                    SetValue(key, null);
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
                return Array.Empty<string>();
            }
        }
    }
}