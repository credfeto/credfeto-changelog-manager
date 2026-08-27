using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

[SuppressMessage(
    category: "Meziantou.Analyzer",
    checkId: "MA0045:Use async overload",
    Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
)]
[SuppressMessage(
    category: "Microsoft.VisualStudio.Threading.Analyzers",
    checkId: "VSTHRD002",
    Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
)]
[SuppressMessage(
    category: "Microsoft.Reliability",
    checkId: "CA2012:UseValueTasksCorrectly",
    Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
)]
public sealed class ChangeLogLocalisationTests : TestBase
{
    private static ChangeLogDocument Parse(string content, ChangeLogLanguage language) =>
        new ChangeLogParser()
            .ParseAsync(content: content, language: language, cancellationToken: default)
            .GetAwaiter()
            .GetResult();

    private static string Serialise(ChangeLogDocument document, ChangeLogLanguage language) =>
        new ChangeLogSerialiser()
            .SerialiseAsync(document, language: language, cancellationToken: default)
            .GetAwaiter()
            .GetResult();

    [Theory]
    [InlineData(ChangeLogLanguageFactory.Russian)]
    [InlineData(ChangeLogLanguageFactory.Polish)]
    public static void LocalisedTemplate_ParsesWithNonNullUnreleasedSection(string languageCode)
    {
        ChangeLogLanguage language = new ChangeLogLanguageFactory().Get(languageCode);

        ChangeLogDocument document = Parse(TemplateFile.Build(language), language);

        Assert.NotNull(document.Unreleased);
    }

    [Theory]
    [InlineData(ChangeLogLanguageFactory.Russian)]
    [InlineData(ChangeLogLanguageFactory.Polish)]
    public static void LocalisedTemplate_RoundTripsThroughSerialiseAndReparse(string languageCode)
    {
        ChangeLogLanguage language = new ChangeLogLanguageFactory().Get(languageCode);

        ChangeLogDocument document = Parse(TemplateFile.Build(language), language);
        string serialised = Serialise(document, language);
        ChangeLogDocument reparsed = Parse(serialised, language);

        Assert.NotNull(reparsed.Unreleased);
        Assert.Contains("## [" + language.UnreleasedSectionName + "]", serialised, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ChangeLogLanguageFactory.Russian)]
    [InlineData(ChangeLogLanguageFactory.Polish)]
    public static void LocalisedTemplate_LintDoesNotReportMissingUnreleasedSection(string languageCode)
    {
        ChangeLogLanguage language = new ChangeLogLanguageFactory().Get(languageCode);

        ChangeLogDocument document = Parse(TemplateFile.Build(language), language);

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(document: document, language: language);

        Assert.DoesNotContain(errors, e => StringComparer.Ordinal.Equals(e.Message, "Missing [Unreleased] section"));
    }
}
