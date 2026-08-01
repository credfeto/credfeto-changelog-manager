using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using Credfeto.ChangeLog.Extensions;

namespace Credfeto.ChangeLog.BenchMark.Tests.Bench;

[SimpleJob]
[MemoryDiagnoser(false)]
[SuppressMessage(category: "codecracker.CSharp", checkId: "CC0091:MarkMembersAsStatic", Justification = "Benchmark")]
[SuppressMessage(
    category: "FunFair.CodeAnalysis",
    checkId: "FFS0012: Make sealed static or abstract",
    Justification = "Benchmark"
)]
public class TextBlockToLinesBenchmark
{
    private static readonly string ManyLinesText = BuildText(lineCount: 200);

    [Benchmark]
    public IReadOnlyList<string> SplitToLines_ManyLines()
    {
        return ManyLinesText.SplitToLines();
    }

    private static string BuildText(int lineCount)
    {
        StringBuilder builder = new();

        for (int i = 0; i < lineCount; ++i)
        {
            builder.Append("line ").Append(i.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
        }

        return builder.ToString();
    }
}
