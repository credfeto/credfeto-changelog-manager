using System;
using Credfeto.ChangeLog.Exceptions;
using Credfeto.ChangeLog.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ThrowsTests : TestBase
{
    [Fact]
    public void CouldNotFindBranchThrowsBranchMissingException()
    {
        Assert.Throws<BranchMissingException>(() => Throws.CouldNotFindBranch("origin/main"));
    }

    [Fact]
    public void CouldNotFindChangeLogThrowsInvalidChangeLogException()
    {
        Assert.Throws<InvalidChangeLogException>(() => Throws.CouldNotFindChangeLog("CHANGELOG.md"));
    }

    [Fact]
    public void ChangeLogNotInRepositoryThrowsInvalidChangeLogException()
    {
        InvalidChangeLogException exception = Assert.Throws<InvalidChangeLogException>(() =>
            Throws.ChangeLogNotInRepository(changeLogFileName: "/elsewhere/CHANGELOG.md", workingDirectory: "/repo/")
        );

        Assert.Equal(
            expected: "Changelog \"/elsewhere/CHANGELOG.md\" is not inside the repository working folder \"/repo/\"",
            actual: exception.Message
        );
    }
}
