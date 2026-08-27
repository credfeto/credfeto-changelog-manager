using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Extensions;
using Credfeto.ChangeLog.Models;
using ZLinq;

namespace Credfeto.ChangeLog.Services;

public sealed class ChangeLogFixer : IChangeLogFixer
{
    private readonly IChangeLogStorage _storage;

    public ChangeLogFixer(IChangeLogStorage storage)
    {
        this._storage = storage;
    }

    public async ValueTask FixAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    )
    {
        ChangeLogDocument document = await this._storage.LoadAsync(changeLogFileName, cancellationToken);
        ChangeLogDocument corrected = Fix(document: document, language: language);
        await this._storage.SaveAsync(changeLogFileName, document: corrected, cancellationToken: cancellationToken);
    }

    public static ChangeLogDocument Fix(ChangeLogDocument document, ChangeLogLanguage language)
    {
        ChangeLogDocument ensured = ChangeLogUpdater.EnsureUnreleasedSections(document: document, language: language);
        ChangeLogDocument withPreamble = EnsurePreamble(ensured);
        ChangeLogDocument withoutBlankHeadingLines = RemoveBlankLinesAfterHeadings(withPreamble);
        return EnsureBlankLineBeforeTrailerComment(withoutBlankHeadingLines);
    }

    public static ChangeLogDocument EnsurePreamble(ChangeLogDocument document)
    {
        if (HasPreamble(document.HeaderLines))
        {
            return document;
        }

        return document with
        {
            HeaderLines = InsertPreamble(document.HeaderLines),
        };
    }

    private static bool HasPreamble(in ImmutableArray<string> headerLines) =>
        headerLines
            .AsValueEnumerable()
            .Any(line =>
                line.Contains(value: TemplateFile.PreambleLine1, comparisonType: System.StringComparison.Ordinal)
            );

    private static ImmutableArray<string> InsertPreamble(in ImmutableArray<string> headerLines)
    {
        int commentStart = FindHtmlCommentStart(headerLines);

        ImmutableArray<string> before = commentStart >= 0 ? headerLines[..commentStart] : headerLines;
        ImmutableArray<string> after = commentStart >= 0 ? headerLines[commentStart..] : [];

        ImmutableArray<string> trimmed = before.TrimTrailingBlanks();

        return
        [
            .. trimmed,
            string.Empty,
            TemplateFile.PreambleLine1,
            TemplateFile.PreambleLine2,
            string.Empty,
            .. after,
        ];
    }

    private static int FindHtmlCommentStart(in ImmutableArray<string> headerLines)
    {
        for (int i = 0; i < headerLines.Length; i++)
        {
            if (headerLines[i].StartsWithHtmlComment())
            {
                return i;
            }
        }

        return -1;
    }

    private static ChangeLogDocument RemoveBlankLinesAfterHeadings(ChangeLogDocument document)
    {
        return document with
        {
            Unreleased = document.Unreleased is not null ? RemoveBlankLinesFromSections(document.Unreleased) : null,
        };
    }

    private static ChangeLogUnreleased RemoveBlankLinesFromSections(ChangeLogUnreleased unreleased)
    {
        ImmutableArray<ChangeLogSection>.Builder builder = ImmutableArray.CreateBuilder<ChangeLogSection>(
            unreleased.Sections.Length
        );

        foreach (ChangeLogSection section in unreleased.Sections)
        {
            builder.Add(RemoveLeadingBlank(section));
        }

        return unreleased with
        {
            Sections = builder.ToImmutable(),
        };
    }

    private static ChangeLogSection RemoveLeadingBlank(ChangeLogSection section)
    {
        int start = 0;

        while (start < section.Entries.Length && string.IsNullOrWhiteSpace(section.Entries[start]))
        {
            start++;
        }

        return start == 0 ? section : section with { Entries = section.Entries[start..] };
    }

    private static ChangeLogDocument EnsureBlankLineBeforeTrailerComment(ChangeLogDocument document) =>
        document.Unreleased is null
            ? document
            : document with
            {
                Unreleased = document.Unreleased with
                {
                    TrailingLines = NormaliseBlankLinesBeforeTrailerComment(document.Unreleased.TrailingLines),
                },
            };

    private static ImmutableArray<string> NormaliseBlankLinesBeforeTrailerComment(
        in ImmutableArray<string> trailingLines
    )
    {
        int blankLineCount = trailingLines.CountBlankLinesBeforeHtmlComment();

        return blankLineCount.IsAlreadyOneBlankLineOrNoComment()
            ? trailingLines
            : [string.Empty, .. trailingLines[blankLineCount..]];
    }
}
