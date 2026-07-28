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
    public void SplitToLinesSplitsOnAllLineEndingVariants(string value, string expectedJoined)
    {
        IReadOnlyList<string> expected = expectedJoined.Split(',');

        IReadOnlyList<string> result = value.SplitToLines();

        Assert.Equal(expected: expected, actual: result);
    }
}
