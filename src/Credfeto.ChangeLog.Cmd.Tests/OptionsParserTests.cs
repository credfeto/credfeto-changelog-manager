using System;
using Credfeto.ChangeLog.Cmd.Exceptions;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Cmd.Tests;

public sealed class OptionsParserTests : TestBase
{
    [Fact]
    public void LongValueOptionsAreParsed()
    {
        Options options = OptionsParser.Parse([
            "--changelog",
            "CHANGELOG.md",
            "--add",
            "Added",
            "--message",
            "Some message",
        ]);

        Assert.Equal(expected: "CHANGELOG.md", actual: options.ChangeLog);
        Assert.Equal(expected: "Added", actual: options.Add);
        Assert.Equal(expected: "Some message", actual: options.Message);
    }

    [Fact]
    public void ShortValueOptionsAreParsed()
    {
        Options options = OptionsParser.Parse(["-f", "CHANGELOG.md", "-r", "Fixed", "-m", "Some message"]);

        Assert.Equal(expected: "CHANGELOG.md", actual: options.ChangeLog);
        Assert.Equal(expected: "Fixed", actual: options.Remove);
        Assert.Equal(expected: "Some message", actual: options.Message);
    }

    [Fact]
    public void ExtractOptionsAreParsed()
    {
        Options options = OptionsParser.Parse(["-x", "notes.md", "-v", "1.2.3"]);

        Assert.Equal(expected: "notes.md", actual: options.Extract);
        Assert.Equal(expected: "1.2.3", actual: options.Version);
    }

    [Fact]
    public void CheckInsertIsParsed()
    {
        Options options = OptionsParser.Parse(["-t", "origin/main"]);

        Assert.Equal(expected: "origin/main", actual: options.CheckInsert);
    }

    [Fact]
    public void CreateReleaseWithPendingIsParsed()
    {
        Options options = OptionsParser.Parse(["--create-release", "2.0.0", "--Pending"]);

        Assert.Equal(expected: "2.0.0", actual: options.CreateRelease);
        Assert.True(options.Pending, userMessage: "Expected --Pending to set Pending");
    }

    [Fact]
    public void ShortSwitchesAreParsed()
    {
        Options options = OptionsParser.Parse(["-c", "2.0.0", "-p", "-u", "-l"]);

        Assert.True(options.Pending, userMessage: "Expected -p to set Pending");
        Assert.True(options.DisplayUnreleased, userMessage: "Expected -u to set DisplayUnreleased");
        Assert.True(options.Lint, userMessage: "Expected -l to set Lint");
    }

    [Fact]
    public void LintWithFixIsParsed()
    {
        Options options = OptionsParser.Parse(["--lint", "--fix"]);

        Assert.True(options.Lint, userMessage: "Expected --lint to set Lint");
        Assert.True(options.Fix, userMessage: "Expected --fix to set Fix");
    }

    [Fact]
    public void UnreleasedIsParsed()
    {
        Options options = OptionsParser.Parse(["--un-released"]);

        Assert.True(options.DisplayUnreleased, userMessage: "Expected --un-released to set DisplayUnreleased");
    }

    [Fact]
    public void SelfTestSwitchIsParsedAsCommand()
    {
        Options options = OptionsParser.Parse(["--self-test"]);

        Assert.True(options.SelfTest, userMessage: "Expected --self-test to set SelfTest");
    }

    [Fact]
    public void HelpIsParsedWithoutACommand()
    {
        Options options = OptionsParser.Parse(["--help"]);

        Assert.True(options.ShowHelp, userMessage: "Expected --help to set ShowHelp");
    }

    [Fact]
    public void InlineValueIsParsed()
    {
        Options options = OptionsParser.Parse(["--add=Added", "--message=a=b"]);

        Assert.Equal(expected: "Added", actual: options.Add);
        Assert.Equal(expected: "a=b", actual: options.Message);
    }

    [Fact]
    public void ValueStartingWithDashIsTakenVerbatim()
    {
        Options options = OptionsParser.Parse(["--add", "Added", "--message", "-starts with a dash"]);

        Assert.Equal(expected: "-starts with a dash", actual: options.Message);
    }

    [Fact]
    public void AdditionalSectionsAreSplitOnComma()
    {
        Options options = OptionsParser.Parse(["--lint", "--additional-sections", "Custom, Experimental,,"]);

        Assert.Equal(
            expected: ["Custom", "Experimental"],
            actual: options.AdditionalSections,
            comparer: StringComparer.Ordinal
        );
    }

    [Fact]
    public void AdditionalSectionsDefaultsToEmpty()
    {
        Options options = OptionsParser.Parse(["--lint"]);

        Assert.Empty(options.AdditionalSections);
    }

    [Fact]
    public void UnknownOptionThrows()
    {
        InvalidOptionsException exception = Assert.Throws<InvalidOptionsException>(() =>
            OptionsParser.Parse(["--unknown-flag"])
        );

        Assert.Contains(
            expectedSubstring: "--unknown-flag",
            actualString: exception.Message,
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public void LongNamesAreCaseSensitive()
    {
        Assert.Throws<InvalidOptionsException>(() => OptionsParser.Parse(["--LINT"]));
    }

    [Fact]
    public void PositionalArgumentThrows()
    {
        Assert.Throws<InvalidOptionsException>(() => OptionsParser.Parse(["--lint", "extra"]));
    }

    [Fact]
    public void MissingValueThrows()
    {
        InvalidOptionsException exception = Assert.Throws<InvalidOptionsException>(() =>
            OptionsParser.Parse(["--add"])
        );

        Assert.Contains(
            expectedSubstring: "requires a value",
            actualString: exception.Message,
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public void InlineValueOnSwitchThrows()
    {
        InvalidOptionsException exception = Assert.Throws<InvalidOptionsException>(() =>
            OptionsParser.Parse(["--lint=true"])
        );

        Assert.Contains(
            expectedSubstring: "does not take a value",
            actualString: exception.Message,
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public void RepeatedOptionThrows()
    {
        InvalidOptionsException exception = Assert.Throws<InvalidOptionsException>(() =>
            OptionsParser.Parse(["--lint", "-l"])
        );

        Assert.Contains(
            expectedSubstring: "more than once",
            actualString: exception.Message,
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public void NoCommandThrows()
    {
        InvalidOptionsException exception = Assert.Throws<InvalidOptionsException>(() =>
            OptionsParser.Parse(["--changelog", "CHANGELOG.md", "--Pending"])
        );

        Assert.Contains(
            expectedSubstring: "--self-test",
            actualString: exception.Message,
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public void EmptyArgumentsThrow()
    {
        Assert.Throws<InvalidOptionsException>(() => OptionsParser.Parse([]));
    }

    [Fact]
    public void UsageListsEveryOption()
    {
        string usage = OptionsParser.Usage;

        Assert.Contains(
            expectedSubstring: "Usage: changelog [options]",
            actualString: usage,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Contains(
            expectedSubstring: "-f, --changelog <value>",
            actualString: usage,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Contains(
            expectedSubstring: "-p, --Pending",
            actualString: usage,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Contains(
            expectedSubstring: "    --additional-sections <value>",
            actualString: usage,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Contains(
            expectedSubstring: "    --self-test",
            actualString: usage,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Contains(expectedSubstring: "    --help", actualString: usage, comparisonType: StringComparison.Ordinal);
    }
}
