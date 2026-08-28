namespace Credfeto.ChangeLog.Benchmark.Tests.Bench;

internal static class BenchmarkLanguage
{
    internal static readonly ChangeLogLanguage English = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );
}
