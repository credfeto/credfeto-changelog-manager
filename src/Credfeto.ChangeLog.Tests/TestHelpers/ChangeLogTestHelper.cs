using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;

namespace Credfeto.ChangeLog.Tests.TestHelpers;

internal static class ChangeLogTestHelper
{
    private static readonly ChangeLogLanguage EnglishLanguage = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );

    internal static ValueTask<ChangeLogDocument> ParseAsync(string content, ChangeLogLanguage? language = null)
    {
        return new ChangeLogParser().ParseAsync(
            content: content,
            language: language ?? EnglishLanguage,
            cancellationToken: default
        );
    }
}
