using System;
using System.Globalization;
using System.IO;
using Credfeto.ChangeLog.Cmd.Exceptions;
using FunFair.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Credfeto.ChangeLog.Cmd.Tests;

public sealed class SelfTestRunnerTests : TestBase
{
    private const string FAILURE_PREFIX = "SELF-TEST FAILED: ";

    [Fact]
    public void Run_WithFullyRegisteredServices_ReportsSuccess()
    {
        using StringWriter output = new(CultureInfo.InvariantCulture);
        using StringWriter error = new(CultureInfo.InvariantCulture);

        SelfTestRunner.Run(
            buildServiceProvider: static () =>
                new ServiceCollection()
                    .AddChangeLog()
                    .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }),
            output: output,
            error: error
        );

        Assert.Contains(
            expectedSubstring: "Self-test passed",
            actualString: output.ToString(),
            comparisonType: StringComparison.Ordinal
        );
        Assert.Empty(error.ToString());
    }

    [Fact]
    public void Run_WithMissingRegistration_ThrowsWithSingleLineReason()
    {
        using StringWriter output = new(CultureInfo.InvariantCulture);
        using StringWriter error = new(CultureInfo.InvariantCulture);

        Assert.Throws<SelfTestFailedException>(() =>
            SelfTestRunner.Run(
                buildServiceProvider: static () => new ServiceCollection().BuildServiceProvider(),
                output: output,
                error: error
            )
        );

        AssertSingleLineFailure(error: error, expectedReason: nameof(IChangeLogDetector));
        Assert.Empty(output.ToString());
    }

    [Fact]
    public void Run_WithThrowingRegistration_ThrowsWithSingleLineReason()
    {
        using StringWriter output = new(CultureInfo.InvariantCulture);
        using StringWriter error = new(CultureInfo.InvariantCulture);

        SelfTestFailedException exception = Assert.Throws<SelfTestFailedException>(() =>
            SelfTestRunner.Run(
                buildServiceProvider: static () =>
                    new ServiceCollection()
                        .AddChangeLog()
                        .AddSingleton<IChangeLogFixer>(_ => throw new InvalidOperationException("Fixer could not load"))
                        .BuildServiceProvider(),
                output: output,
                error: error
            )
        );

        Assert.Equal(expected: "Fixer could not load", actual: exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        AssertSingleLineFailure(error: error, expectedReason: "Fixer could not load");
        Assert.Empty(output.ToString());
    }

    [Fact]
    public void Run_WhenServiceProviderCannotBeBuilt_ThrowsWithMultiLineMessageFlattened()
    {
        using StringWriter output = new(CultureInfo.InvariantCulture);
        using StringWriter error = new(CultureInfo.InvariantCulture);

        Assert.Throws<SelfTestFailedException>(() =>
            SelfTestRunner.Run(
                buildServiceProvider: static () => throw new InvalidOperationException("first line\nsecond line"),
                output: output,
                error: error
            )
        );

        AssertSingleLineFailure(error: error, expectedReason: "first line second line");
    }

    private static void AssertSingleLineFailure(StringWriter error, string expectedReason)
    {
        string[] lines = error
            .ToString()
            .Split(separator: Environment.NewLine, options: StringSplitOptions.RemoveEmptyEntries);

        string line = Assert.Single(lines);
        Assert.StartsWith(
            expectedStartString: FAILURE_PREFIX,
            actualString: line,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Contains(
            expectedSubstring: expectedReason,
            actualString: line,
            comparisonType: StringComparison.Ordinal
        );
    }
}
