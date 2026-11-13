using System;
using System.Collections.Generic;
using Credfeto.ChangeLog.Mcp.Models;
using Credfeto.ChangeLog.Mcp.Paths;

namespace Credfeto.ChangeLog.Mcp.Tools;

public sealed class ToolContext
{
    private readonly IChangeLogLanguageFactory _languageFactory;
    private readonly ChangeLogPathResolver _resolver;

    public ToolContext(ChangeLogPathResolver resolver, IChangeLogLanguageFactory languageFactory)
    {
        this._resolver = resolver;
        this._languageFactory = languageFactory;
    }

    public ChangeLogTarget Resolve(ToolArguments arguments)
    {
        string fullPath = this._resolver.Resolve(arguments.GetOptionalString(ToolProperties.ChangeLog));
        ChangeLogLanguage language = this.Language(arguments.GetOptionalString(ToolProperties.AdditionalSections));

        return new(FullPath: fullPath, RelativePath: this._resolver.RelativeToRepository(fullPath), Language: language);
    }

    private ChangeLogLanguage Language(string? additionalSections)
    {
        ChangeLogLanguage language = this._languageFactory.Get(ChangeLogLanguageFactory.English);
        IReadOnlyList<string> additional = SplitSections(additionalSections);

        if (additional.Count == 0)
        {
            return language;
        }

        return language with
        {
            SectionOrder = [.. language.SectionOrder, .. additional],
        };
    }

    private static IReadOnlyList<string> SplitSections(string? additionalSections)
    {
        if (string.IsNullOrWhiteSpace(additionalSections))
        {
            return [];
        }

        return additionalSections.Split(
            separator: ',',
            options: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
        );
    }
}
