using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Models;

namespace Credfeto.ChangeLog.Services;

public sealed class ChangeLogReader : IChangeLogReader
{
    private readonly IChangeLogStorage _storage;

    public ChangeLogReader(IChangeLogStorage storage)
    {
        this._storage = storage;
    }

    public async ValueTask<string> ExtractReleaseNotesFromFileAsync(
        string changeLogFileName,
        string version,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    )
    {
        ChangeLogDocument document = await this._storage.LoadAsync(
            changeLogFileName,
            language: language,
            cancellationToken: cancellationToken
        );

        return ExtractReleaseNotes(document: document, version: version);
    }

    public async ValueTask<int?> FindFirstReleaseVersionPositionAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    )
    {
        ChangeLogDocument document = await this._storage.LoadAsync(
            changeLogFileName,
            language: language,
            cancellationToken: cancellationToken
        );

        return document.Releases.IsEmpty ? null : document.Releases[0].LineNumber;
    }

    public static string ExtractReleaseNotes(ChangeLogDocument document, string version)
    {
        Version? releaseVersion = BuildNumberHelpers.DetermineVersionForChangeLog(version);

        if (releaseVersion is null)
        {
            return FormatSections(
                preambleLines: document.Unreleased?.PreambleLines ?? [],
                sections: document.Unreleased?.Sections ?? []
            );
        }

        ChangeLogRelease? release = FindRelease(releases: document.Releases, version: releaseVersion);

        return FormatSections(preambleLines: [], sections: release?.Sections ?? []);
    }

    private static ChangeLogRelease? FindRelease(in ImmutableArray<ChangeLogRelease> releases, Version version)
    {
        foreach (ChangeLogRelease release in releases)
        {
            if (
                Version.TryParse(input: release.Version, result: out Version? parsed)
                && VersionMatches(parsed: parsed, requested: version)
            )
            {
                return release;
            }
        }

        return null;
    }

    private static bool VersionMatches(Version parsed, Version requested)
    {
        int requestedBuild = requested.Build is 0 or -1 ? 0 : requested.Build;
        int parsedBuild = parsed.Build is 0 or -1 ? 0 : parsed.Build;

        return parsed.Major == requested.Major && parsed.Minor == requested.Minor && parsedBuild == requestedBuild;
    }

    private static string FormatSections(
        in ImmutableArray<ChangeLogPreambleLine> preambleLines,
        in ImmutableArray<ChangeLogSection> sections
    )
    {
        StringBuilder result = new();

        foreach (ChangeLogPreambleLine preambleLine in preambleLines)
        {
            result.AppendLine(preambleLine.Text);
        }

        foreach (ChangeLogSection section in sections)
        {
            AppendSection(result: result, section: section);
        }

        return result.ToString().Trim();
    }

    private static void AppendSection(StringBuilder result, ChangeLogSection section)
    {
        bool headingWritten = false;

        foreach (string entry in section.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                continue;
            }

            if (!headingWritten)
            {
                result.AppendLine($"### {section.Name}");
                headingWritten = true;
            }

            result.AppendLine(entry);
        }
    }
}
