using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Extensions;
using Credfeto.ChangeLog.Helpers;
using Credfeto.ChangeLog.Models;

namespace Credfeto.ChangeLog.Services;

public sealed class ChangeLogParser : IChangeLogParser
{
    public ValueTask<ChangeLogDocument> ParseAsync(
        string content,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<string> lines = content.SplitToLines();
        ChangeLogDocument document = Parse(lines: lines, language: language);
        return ValueTask.FromResult(document);
    }

    private static ChangeLogDocument Parse(IReadOnlyList<string> lines, ChangeLogLanguage language)
    {
        string unreleasedHeader = language.UnreleasedHeader;
        int unreleasedStart = lines.FindUnreleasedStart(unreleasedHeader);
        if (unreleasedStart < 0)
        {
            return new(HeaderLines: [.. lines], Unreleased: null, Releases: [], TrailingLines: []);
        }

        int unreleasedEnd = lines.FindUnreleasedEnd(
            unreleasedStart: unreleasedStart,
            unreleasedHeader: unreleasedHeader
        );
        ChangeLogUnreleased unreleased = ParseUnreleased(lines, start: unreleasedStart, end: unreleasedEnd);
        (ImmutableArray<ChangeLogRelease> releases, ImmutableArray<string> trailingLines) = ParseReleases(
            lines: lines,
            start: unreleasedEnd,
            unreleasedHeader: unreleasedHeader
        );
        return new(
            HeaderLines: CollectLines(lines, start: 0, end: unreleasedStart),
            Unreleased: unreleased,
            Releases: ApplyUnreleasedBoundaryBlankLines(releases: releases, unreleased: unreleased),
            TrailingLines: trailingLines
        );
    }

    private static ChangeLogUnreleased ParseUnreleased(IReadOnlyList<string> lines, int start, int end)
    {
        List<ChangeLogSection> sections = [];
        List<string> trailer = [];
        string? currentName = null;
        List<string> currentEntries = [];
        int blanksBeforeFirstSection = 0;

        for (int i = start + 1; i < end; i++)
        {
            if (
                !ProcessUnreleasedLine(
                    lines: lines,
                    lineIndex: i,
                    end: end,
                    sections: sections,
                    currentName: ref currentName,
                    currentEntries: currentEntries,
                    trailer: trailer,
                    blanksBeforeFirstSection: ref blanksBeforeFirstSection
                )
            )
            {
                break;
            }
        }

        FlushSection(sections: sections, name: currentName, entries: currentEntries);
        MoveTrailingBlanksFromLastSection(
            sections: sections,
            trailer: trailer,
            blanksBeforeFirstSection: blanksBeforeFirstSection
        );
        return new(
            LineNumber: start + 1,
            Sections: [.. sections],
            TrailingLines: [.. trailer],
            // trailer is always a contiguous suffix of the [start + 1, end) line range (every
            // other path, sections/entries, blanksBeforeFirstSection, accounts for every line
            // before it), so its own start line is derived directly from that range rather than
            // reconstructed from section/entry counts; see MoveTrailingBlanksFromLastSection.
            TrailingLinesStartLineNumber: end - trailer.Count + 1
        );
    }

    // When an HTML comment precedes the next release heading, ProcessUnreleasedLine's
    // StartsWithHtmlComment() branch already moves the last section's trailing blanks into
    // trailer before collecting the comment. With no comment, those blanks are never moved out
    // of the last section's Entries (FlushSection does not trim them), so the gap before the
    // heading is silently lost. Doing the move here unconditionally covers both cases: a no-op
    // when it already happened, the missing step when it didn't.
    //
    // A [Unreleased] block with no ### sections at all never reaches sections/currentEntries, so
    // its blank-line gap is tracked separately via blanksBeforeFirstSection (see
    // ProcessUnreleasedLine) and applied here instead.
    private static void MoveTrailingBlanksFromLastSection(
        List<ChangeLogSection> sections,
        List<string> trailer,
        int blanksBeforeFirstSection
    )
    {
        if (sections.Count == 0)
        {
            for (int i = 0; i < blanksBeforeFirstSection; i++)
            {
                trailer.Insert(index: 0, item: string.Empty);
            }

            return;
        }

        ChangeLogSection last = sections[^1];
        int end = last.Entries.Length - last.Entries.CountTrailingBlankLines();

        if (end == last.Entries.Length)
        {
            return;
        }

        trailer.InsertRange(index: 0, last.Entries[end..]);
        sections[^1] = last with { Entries = last.Entries[..end] };
    }

    private static bool ProcessUnreleasedLine(
        IReadOnlyList<string> lines,
        int lineIndex,
        int end,
        List<ChangeLogSection> sections,
        ref string? currentName,
        List<string> currentEntries,
        List<string> trailer,
        ref int blanksBeforeFirstSection
    )
    {
        string line = lines[lineIndex];

        if (line.StartsWithHtmlComment())
        {
            MoveTrailingBlanks(source: currentEntries, destination: trailer);
            CollectTrailer(lines: lines, from: lineIndex, to: end, trailer: trailer);
            return false;
        }

        if (line.IsChangeTypeHeading())
        {
            FlushSection(sections: sections, name: currentName, entries: currentEntries);
            currentName = line.GetChangeTypeName();
            currentEntries.Clear();
            blanksBeforeFirstSection = 0;
        }
        else if (currentName is not null)
        {
            currentEntries.Add(line);
        }
        else if (string.IsNullOrWhiteSpace(line))
        {
            blanksBeforeFirstSection++;
        }
        else
        {
            blanksBeforeFirstSection = 0;
        }

        return true;
    }

    private static void FlushSection(List<ChangeLogSection> sections, string? name, List<string> entries)
    {
        if (name is not null)
        {
            sections.Add(new(Name: name, LineNumber: 0, Entries: [.. entries]));
        }
    }

    // Assumes destination is empty: blanks are appended in original order, so a non-empty
    // destination would end up with its existing content before the moved blanks, whereas the
    // previous Insert(0, ...) implementation placed them before existing content.
    private static void MoveTrailingBlanks(List<string> source, List<string> destination)
    {
        int end = source.Count;
        int start = end;

        while (start > 0 && string.IsNullOrWhiteSpace(source[start - 1]))
        {
            --start;
        }

        for (int i = start; i < end; ++i)
        {
            destination.Add(source[i]);
        }

        source.RemoveRange(start, end - start);
    }

    private static void CollectTrailer(IReadOnlyList<string> lines, int from, int to, List<string> trailer)
    {
        for (int j = from; j < to; j++)
        {
            trailer.Add(lines[j]);
        }
    }

    private static (ImmutableArray<ChangeLogRelease> Releases, ImmutableArray<string> TrailingLines) ParseReleases(
        IReadOnlyList<string> lines,
        int start,
        string unreleasedHeader
    )
    {
        List<ChangeLogRelease> releases = [];
        ReleaseParseState state = new();

        for (int i = start; i < lines.Count; i++)
        {
            ProcessReleaseLine(
                line: lines[i],
                lineIndex: i,
                releases: releases,
                state: state,
                unreleasedHeader: unreleasedHeader
            );
        }

        state.Flush(releases);
        return ([.. releases], [.. state.TrailingLines]);
    }

    private static void ProcessReleaseLine(
        string line,
        int lineIndex,
        List<ChangeLogRelease> releases,
        ReleaseParseState state,
        string unreleasedHeader
    )
    {
        if (state.InTrailerMode)
        {
            state.TrailingLines.Add(line);
        }
        else if (line.IsComparisonLink())
        {
            state.EnterTrailerMode();
            state.TrailingLines.Add(line);
        }
        else if (line.IsReleaseHeader(unreleasedHeader))
        {
            state.Flush(releases);
            state.StartRelease(line: line, lineNumber: lineIndex + 1);
        }
        else if (line.IsChangeTypeHeading())
        {
            state.FlushSection();
            state.CurrentSectionName = line.GetChangeTypeName();
            state.CurrentSectionLine = lineIndex + 1;
            state.NoteNonBlankLine();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                state.NoteBlankLine();
            }
            else
            {
                state.NoteNonBlankLine();
            }

            if (state.CurrentSectionName is not null)
            {
                state.CurrentEntries.Add(line);
            }
        }
    }

    private static ImmutableArray<string> CollectLines(IReadOnlyList<string> lines, int start, int end)
    {
        ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>(end - start);
        for (int i = start; i < end; i++)
        {
            builder.Add(lines[i]);
        }

        return builder.ToImmutable();
    }

    // The gap between [Unreleased] and the first release heading is parsed via a different
    // code path (ChangeLogUnreleased.TrailingLines) to the gap between subsequent releases
    // (tracked in ReleaseParseState), so the first release's blank-line count is corrected here
    // once both are known, rather than in ReleaseParseState itself.
    private static ImmutableArray<ChangeLogRelease> ApplyUnreleasedBoundaryBlankLines(
        in ImmutableArray<ChangeLogRelease> releases,
        ChangeLogUnreleased unreleased
    )
    {
        if (releases.IsEmpty)
        {
            return releases;
        }

        int blankLines = unreleased.TrailingLines.CountTrailingBlankLines();
        return releases.SetItem(index: 0, releases[0] with { BlankLinesBeforeHeading = blankLines });
    }

    private sealed class ReleaseParseState
    {
        public string? CurrentVersion { get; private set; }
        public string? CurrentDate { get; private set; }
        public int CurrentReleaseLineNumber { get; private set; }
        public string? CurrentSectionName { get; set; }
        public int CurrentSectionLine { get; set; }
        public List<string> CurrentEntries { get; } = [];
        public List<string> TrailingLines { get; } = [];
        public bool InTrailerMode { get; private set; }
        private List<ChangeLogSection> CurrentSections { get; } = [];

        public bool CurrentIsYanked { get; private set; }

        // Consecutive blank lines seen since the last non-blank line, i.e. the gap immediately
        // before whatever comes next (a new release heading, or end of file). Tracked
        // independently of CurrentSectionName so it stays correct even for a release with no
        // ### sections at all, where blank lines are never added to CurrentEntries.
        private int TrailingBlankLineCount { get; set; }
        public int CurrentBlankLinesBeforeHeading { get; private set; }

        public void NoteBlankLine() => this.TrailingBlankLineCount++;

        public void NoteNonBlankLine() => this.TrailingBlankLineCount = 0;

        public void StartRelease(string line, int lineNumber)
        {
            this.InTrailerMode = false;
            this.TrailingLines.Clear();
            (this.CurrentVersion, this.CurrentDate, this.CurrentIsYanked) = ParseVersionHeader(line);
            this.CurrentReleaseLineNumber = lineNumber;
            this.CurrentBlankLinesBeforeHeading = this.TrailingBlankLineCount;
            this.TrailingBlankLineCount = 0;
        }

        public void EnterTrailerMode()
        {
            while (this.CurrentEntries.Count > 0 && string.IsNullOrWhiteSpace(this.CurrentEntries[^1]))
            {
                this.CurrentEntries.RemoveAt(this.CurrentEntries.Count - 1);
            }

            this.InTrailerMode = true;
        }

        private const string YANKED_SUFFIX = "[YANKED]";

        private static (string Version, string Date, bool IsYanked) ParseVersionHeader(string line)
        {
            int closeBracket = line.IndexOf(value: ']', comparisonType: StringComparison.Ordinal);

            if (closeBracket == -1)
            {
                return Throws.MalformedVersionHeader(line);
            }

            string version = line[4..closeBracket];
            string rest = line[(closeBracket + 1)..].Trim();

            bool isYanked = rest.EndsWith(value: YANKED_SUFFIX, comparisonType: StringComparison.OrdinalIgnoreCase);
            if (isYanked)
            {
                rest = rest[..^YANKED_SUFFIX.Length].Trim();
            }

            if (rest.StartsWith(value: '-'))
            {
                rest = rest[1..].Trim();
            }

            return (version, rest, isYanked);
        }

        public void FlushSection()
        {
            if (this.CurrentSectionName is not null)
            {
                TrimTrailingBlanks(this.CurrentEntries);
                this.CurrentSections.Add(
                    new(
                        Name: this.CurrentSectionName,
                        LineNumber: this.CurrentSectionLine,
                        Entries: [.. this.CurrentEntries]
                    )
                );
            }

            this.CurrentSectionName = null;
            this.CurrentEntries.Clear();
        }

        private static void TrimTrailingBlanks(List<string> entries)
        {
            while (entries.Count > 0 && string.IsNullOrWhiteSpace(entries[^1]))
            {
                entries.RemoveAt(entries.Count - 1);
            }
        }

        public void Flush(List<ChangeLogRelease> releases)
        {
            if (this.CurrentVersion is null)
            {
                return;
            }

            this.FlushSection();
            releases.Add(
                new(
                    Version: this.CurrentVersion,
                    Date: this.CurrentDate ?? string.Empty,
                    LineNumber: this.CurrentReleaseLineNumber,
                    Sections: [.. this.CurrentSections],
                    IsYanked: this.CurrentIsYanked,
                    BlankLinesBeforeHeading: this.CurrentBlankLinesBeforeHeading
                )
            );
            this.CurrentSections.Clear();
            this.CurrentVersion = null;
            this.CurrentIsYanked = false;
        }
    }
}
