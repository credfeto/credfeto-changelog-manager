using Xunit;

namespace Credfeto.ChangeLog.Benchmark.Tests;

// The benchmarks run in-process ([InProcess]), so BenchmarkDotNet's memory diagnoser measures
// allocations for the whole test process. Running the benchmark tests in parallel would count
// other tests' allocations against each benchmark, so they all share this test collection and
// run one at a time.
[CollectionDefinition(Name, DisableParallelization = true)]
public static class SerialBenchmarks
{
    public const string Name = "Benchmarks";
}
