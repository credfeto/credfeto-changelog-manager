using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;

namespace Credfeto.ChangeLog.Benchmark.Tests.Bench;

[InProcess]
[MemoryDiagnoser(false)]
[SuppressMessage(category: "codecracker.CSharp", checkId: "CC0091:MarkMembersAsStatic", Justification = "Benchmark")]
[SuppressMessage(
    category: "FunFair.CodeAnalysis",
    checkId: "FFS0012: Make sealed static or abstract",
    Justification = "Benchmark"
)]
public class EnsureUnreleasedSectionsBenchmark
{
    private const string CORRECT_ORDER_CHANGELOG =
        @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Security
### Added
### Fixed
### Changed
### Deprecated
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

    private const string OUT_OF_ORDER_CHANGELOG =
        @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Deployment Changes
### Removed
### Added
- A new feature was added.
### Fixed
- A bug was fixed.
### Changed

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

    private ChangeLogDocument? _correctOrderDocument;
    private ChangeLogDocument? _outOfOrderDocument;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        this._correctOrderDocument = await ParseAsync(CORRECT_ORDER_CHANGELOG);
        this._outOfOrderDocument = await ParseAsync(OUT_OF_ORDER_CHANGELOG);
    }

    [Benchmark]
    public ChangeLogDocument EnsureUnreleasedSections_AllSectionsCorrect()
    {
        ChangeLogDocument document = this._correctOrderDocument
            ?? throw new InvalidOperationException("Setup has not run");

        return ChangeLogUpdater.EnsureUnreleasedSections(document: document, language: BenchmarkLanguage.English);
    }

    [Benchmark]
    public ChangeLogDocument EnsureUnreleasedSections_OutOfOrderAndMissing()
    {
        ChangeLogDocument document = this._outOfOrderDocument
            ?? throw new InvalidOperationException("Setup has not run");

        return ChangeLogUpdater.EnsureUnreleasedSections(document: document, language: BenchmarkLanguage.English);
    }

    private static ValueTask<ChangeLogDocument> ParseAsync(string content)
    {
        return new ChangeLogParser().ParseAsync(
            content: content,
            language: BenchmarkLanguage.English,
            cancellationToken: CancellationToken.None
        );
    }
}
