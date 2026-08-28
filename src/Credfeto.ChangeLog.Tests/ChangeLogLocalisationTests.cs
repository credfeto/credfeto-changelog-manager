using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogLocalisationTests : TestBase
{
    private static ChangeLogLanguage GetLanguage(string languageCode)
    {
        return new ChangeLogLanguageFactory().Get(languageCode);
    }

    [Theory]
    [InlineData(ChangeLogLanguageFactory.Russian)]
    [InlineData(ChangeLogLanguageFactory.Polish)]
    public static async Task LocalisedTemplate_ParsesWithNonNullUnreleasedSectionAsync(string languageCode)
    {
        ChangeLogLanguage language = GetLanguage(languageCode);

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(TemplateFile.Build(language), language);

        Assert.NotNull(document.Unreleased);
    }

    [Theory]
    [InlineData(ChangeLogLanguageFactory.Russian)]
    [InlineData(ChangeLogLanguageFactory.Polish)]
    public static async Task LocalisedTemplate_RoundTripsThroughSerialiseAndReparseAsync(string languageCode)
    {
        ChangeLogLanguage language = GetLanguage(languageCode);

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(TemplateFile.Build(language), language);
        string serialised = await ChangeLogTestHelper.SerialiseAsync(document, language);
        ChangeLogDocument reparsed = await ChangeLogTestHelper.ParseAsync(serialised, language);

        Assert.NotNull(reparsed.Unreleased);
        Assert.Contains(language.UnreleasedHeader, serialised, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ChangeLogLanguageFactory.Russian)]
    [InlineData(ChangeLogLanguageFactory.Polish)]
    public static async Task LocalisedTemplate_LintDoesNotReportMissingUnreleasedSectionAsync(string languageCode)
    {
        ChangeLogLanguage language = GetLanguage(languageCode);

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(TemplateFile.Build(language), language);

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(document: document, language: language);

        Assert.DoesNotContain(errors, e => StringComparer.Ordinal.Equals(e.Message, "Missing [Unreleased] section"));
    }
}
