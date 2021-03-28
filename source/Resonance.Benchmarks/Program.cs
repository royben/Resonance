using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var summery1 = BenchmarkRunner.Run<AdaptersBenchmark>(ManualConfig.Create(DefaultConfig.Instance)
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true));

            var summery2 = BenchmarkRunner.Run<TranscodingBenchmark>(ManualConfig.Create(DefaultConfig.Instance)
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true));

            Console.ReadLine();
        }
    }
}
