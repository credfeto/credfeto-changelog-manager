using System;
using System.Collections.Generic;
using Credfeto.ChangeLog.Extensions;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class TextBlockToLinesTests : TestBase
{
    [Theory]
    [InlineData("a\r\nb\r\nc", "a,b,c")]
    [InlineData("a\nb\nc", "a,b,c")]
    [InlineData("a\rb\rc", "a,b,c")]
    [InlineData("a\r\nb\nc\rd", "a,b,c,d")]
    [InlineData("a\r\n\r\nb", "a,,b")]
    [InlineData("", "")]
    [InlineData("single-line", "single-line")]
    public static void SplitToLinesSplitsOnAllLineEndingVariants(string value, string expectedJoined)
    {
        IReadOnlyList<string> expected = expectedJoined.Split(',');

        IReadOnlyList<string> result = value.SplitToLines();

        Assert.Equal(expected: expected, actual: result);
    }

    [Theory]
    [InlineData("a\r\nb", "\r\n")]
    [InlineData("a\nb", "\n")]
    [InlineData("a\rb", "\n")]
    [InlineData("a\r\nb\nc", "\r\n")]
    [InlineData("a\nb\r\nc", "\n")]
    [InlineData("a\n\rb", "\n")]
    public static void DetectLineEndingUsesTheFirstLineBreak(string value, string expected)
    {
        string result = value.DetectLineEnding();

        Assert.Equal(expected: expected, actual: result, comparer: StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("single-line")]
    public static void DetectLineEndingWithoutALineBreakUsesTheEnvironmentNewLine(string value)
    {
        string result = value.DetectLineEnding();

        Assert.Equal(expected: Environment.NewLine, actual: result, comparer: StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("a\nb\r\nc\rd", "\r\n", "a\r\nb\r\nc\r\nd")]
    [InlineData("a\r\nb\r\n\r\nc", "\n", "a\nb\n\nc")]
    [InlineData("single-line", "\r\n", "single-line")]
    [InlineData("", "\n", "")]
    public static void WithLineEndingRejoinsEveryLineWithTheGivenEnding(
        string value,
        string lineEnding,
        string expected
    )
    {
        string result = value.WithLineEnding(lineEnding);

        Assert.Equal(expected: expected, actual: result, comparer: StringComparer.Ordinal);
    }
}
