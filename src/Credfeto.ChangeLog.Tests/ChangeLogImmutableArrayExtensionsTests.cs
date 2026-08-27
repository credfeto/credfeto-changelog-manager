using System.Collections.Immutable;
using Credfeto.ChangeLog.Extensions;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogImmutableArrayExtensionsTests : TestBase
{
    [Fact]
    public void CountBlankLinesBeforeHtmlComment_CommentIsFirstLine_ReturnsZero()
    {
        ImmutableArray<string> lines = ["<!--", "note", "-->"];

        Assert.Equal(expected: 0, actual: lines.CountBlankLinesBeforeHtmlComment());
    }

    [Fact]
    public void CountBlankLinesBeforeHtmlComment_BlankLinesThenComment_ReturnsBlankLineCount()
    {
        ImmutableArray<string> lines = ["", "", "<!--", "note", "-->"];

        Assert.Equal(expected: 2, actual: lines.CountBlankLinesBeforeHtmlComment());
    }

    [Fact]
    public void CountBlankLinesBeforeHtmlComment_NoCommentPresent_ReturnsMinusOne()
    {
        ImmutableArray<string> lines = ["", ""];

        Assert.Equal(expected: -1, actual: lines.CountBlankLinesBeforeHtmlComment());
    }

    [Fact]
    public void CountBlankLinesBeforeHtmlComment_EmptyArray_ReturnsMinusOne()
    {
        ImmutableArray<string> lines = [];

        Assert.Equal(expected: -1, actual: lines.CountBlankLinesBeforeHtmlComment());
    }

    [Fact]
    public void CountBlankLinesBeforeHtmlComment_NonBlankContentBeforeComment_ReturnsMinusOne()
    {
        // Not reachable via ChangeLogParser's own output today (it never leaves arbitrary
        // non-blank content as TrailingLines' leading element), but the extension's contract is
        // to treat this as "not applicable" for any caller, not just parser-produced input.
        ImmutableArray<string> lines = ["Some unrelated note.", "<!--", "note", "-->"];

        Assert.Equal(expected: -1, actual: lines.CountBlankLinesBeforeHtmlComment());
    }
}
