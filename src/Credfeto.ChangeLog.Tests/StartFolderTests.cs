using System.IO;
using Credfeto.ChangeLog.Exceptions;
using Credfeto.ChangeLog.Helpers;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

[Collection(SerialCurrentDirectory.Name)]
public sealed class StartFolderTests : TestBase
{
    [Fact]
    public void ReadReturnsTheCurrentFolderWhenItExists()
    {
        Assert.Equal(expected: Directory.GetCurrentDirectory(), actual: StartFolder.Read());
    }

    [Fact]
    public void TryReadReturnsTheCurrentFolderWhenItExists()
    {
        Assert.Equal(expected: Directory.GetCurrentDirectory(), actual: StartFolder.TryRead());
    }

    [Fact]
    public void TryReadReturnsNullWhenTheStartFolderHasBeenDeleted()
    {
        using DeletedCurrentDirectoryScope deleted = new();

        Assert.Null(StartFolder.TryRead());
    }

    [Fact]
    public void ReadThrowsWhenTheStartFolderHasBeenDeleted()
    {
        using DeletedCurrentDirectoryScope deleted = new();

        StartFolderMissingException exception = Assert.Throws<StartFolderMissingException>(StartFolder.Read);

        Assert.Equal(expected: "The folder this was started in no longer exists", actual: exception.Message);
    }

    [Fact]
    public void ResolvingARelativePathThrowsWhenTheStartFolderHasBeenDeleted()
    {
        using DeletedCurrentDirectoryScope deleted = new();

        Assert.Throws<StartFolderMissingException>(() => PhysicalPath.Resolve("CHANGELOG.md"));
    }

    [Fact]
    public void ResolvingAnAbsolutePathWorksWhenTheStartFolderHasBeenDeleted()
    {
        string folder = Path.GetFullPath(Path.GetTempPath());
        string expected = PhysicalPath.Resolve(folder);

        using DeletedCurrentDirectoryScope deleted = new();

        Assert.Equal(expected: expected, actual: PhysicalPath.Resolve(folder));
    }

    [Fact]
    public void DetectingTheChangeLogThrowsWhenTheStartFolderHasBeenDeleted()
    {
        ChangeLogDetector detector = new();

        using DeletedCurrentDirectoryScope deleted = new();

        Assert.Throws<StartFolderMissingException>(() => detector.TryFindChangeLog(out _));
    }
}
