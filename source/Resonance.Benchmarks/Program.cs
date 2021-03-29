using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            Summary summary = null;

            summary = BenchmarkRunner.Run<AdaptersBenchmark>(ManualConfig.Create(DefaultConfig.Instance)
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true));

            summary = BenchmarkRunner.Run<TranscodingBenchmark>(ManualConfig.Create(DefaultConfig.Instance)
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true));

            Process.Start("explorer.exe", string.Format("/select,\"{0}\"", summary.ResultsDirectoryPath));
        }
    }
}
