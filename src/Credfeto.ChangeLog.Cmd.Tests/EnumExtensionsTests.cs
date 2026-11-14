using System.Diagnostics;
using FunFair.Test.Common;
using LibGit2Sharp;
using Xunit;

namespace Credfeto.ChangeLog.Cmd.Tests;

public sealed class EnumExtensionsTests : TestBase
{
    [Theory]
    [InlineData(LogLevel.None)]
    [InlineData(LogLevel.Fatal)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Info)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Trace)]
    public static void LogLevelGetNameReturnsNonEmptyString(LogLevel value)
    {
        string name = value.GetName();
        Assert.NotEmpty(name);
    }

    [Theory]
    [InlineData(LogLevel.None)]
    [InlineData(LogLevel.Fatal)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Info)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Trace)]
    public static void LogLevelGetDescriptionReturnsNonEmptyString(LogLevel value)
    {
        string description = value.GetDescription();
        Assert.NotEmpty(description);
    }

    [Theory]
    [InlineData(LogLevel.None)]
    [InlineData(LogLevel.Fatal)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Info)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Trace)]
    public static void LogLevelIsDefinedReturnsTrueForKnownValues(LogLevel value)
    {
        bool defined = value.IsDefined();
        Assert.True(defined, userMessage: "Expected IsDefined to return true for a known LogLevel value");
    }

    [Fact]
    public void LogLevelIsDefinedReturnsFalseForUnknownValue()
    {
        bool defined = ((LogLevel)int.MaxValue).IsDefined();
        Assert.False(defined, userMessage: "Expected IsDefined to return false for an unknown LogLevel value");
    }

    [Fact]
    public void LogLevelGetNameThrowsForUnknownValue()
    {
        Assert.Throws<UnreachableException>(() => ((LogLevel)int.MaxValue).GetName());
    }
}
