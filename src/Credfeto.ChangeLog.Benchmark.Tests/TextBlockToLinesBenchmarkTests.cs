using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Credfeto.ChangeLog.Benchmark.Tests.Bench;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Benchmark.Tests;

public sealed class TextBlockToLinesBenchmarkTests : LoggingTestBase
{
    // Baseline measured against the pre-optimisation nested Split/SelectMany chain in
    // TextBlockToLines.SplitToLines (issue #329), over a 200-line \r\n-delimited fixture:
    // ~22.98us/op, ~32.44KB/op (BenchmarkDotNet SimpleJob).
    // This limit includes a 25% margin to allow for minor variation across machines.
    // NOTE: this ceiling is anchored to the pre-optimisation figure (agents never run
    // *.Benchmark.Tests locally; CI handles benchmarks). The single-Split implementation
    // allocates less, so it still passes; tighten it once CI or a human reports the
    // post-optimisation bytes/op.
    private const long MAX_ALLOCATED_BYTES_MANY_LINES = 41524;

    public TextBlockToLinesBenchmarkTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public void RunBenchmark()
    {
        (Summary summary, AccumulationLogger logger) = Benchmark<TextBlockToLinesBenchmark>();

        this.Output.WriteLine(logger.GetLog());

        summary.AssertAllocationsAtMost(maximumBytes: MAX_ALLOCATED_BYTES_MANY_LINES);
    }
}
