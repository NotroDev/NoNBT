using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace NoNBT.Benchmarks;

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithWarmupCount(3)
            .WithIterationCount(10));
        
        AddJob(Job.Default
            .WithRuntime(CoreRuntime.Core80)
            .WithId(".NET 8.0"));

        AddJob(Job.Default
            .WithRuntime(CoreRuntime.Core90)
            .WithId(".NET 9.0"));
    }
}