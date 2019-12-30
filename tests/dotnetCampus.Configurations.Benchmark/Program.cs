using BenchmarkDotNet.Running;
using System;
using System.Reflection;

namespace dotnetCampus.Configurations.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ConfigurationBenchmark>();
            //BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
        }
    }
}
