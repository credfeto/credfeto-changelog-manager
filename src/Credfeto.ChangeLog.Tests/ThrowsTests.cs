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
}
