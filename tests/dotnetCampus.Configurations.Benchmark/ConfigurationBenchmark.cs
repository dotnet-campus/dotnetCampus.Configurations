using BenchmarkDotNet.Attributes;
using dotnetCampus.Configurations.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotnetCampus.Configurations.Benchmark
{
    public class ConfigurationBenchmark
    {
        [Benchmark(Baseline = true, Description = "仅内存缓存")]
        public void ReadDirectly()
        {
            const string dcc = "configs.01.dcc";
            FileConfigurationRepo repo = ConfigurationFactory.FromFile(dcc);
            var configs = repo.CreateAppConfigurator().Of<FakeConfiguration>();
            _ = configs.Key;
        }

        [Benchmark(Description = "先检查文件")]
        public async Task ReadWithFileChangeChecking()
        {
            const string dcc = "configs.02.dcc";
            FileConfigurationRepo repo = ConfigurationFactory.FromFile(dcc);
            var configs = repo.CreateAppConfigurator().Of<FakeConfiguration>();
            await repo.ReloadExternalChangesAsync().ConfigureAwait(false);
            _ = configs.Key;
        }

        private class FakeConfiguration : Configuration
        {
            public FakeConfiguration() : base("")
            {
            }

            public string Key
            {
                get => GetString();
                set => SetValue(value);
            }
        }
    }
}
