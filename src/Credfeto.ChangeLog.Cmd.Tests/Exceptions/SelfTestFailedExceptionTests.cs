using System;
using Credfeto.ChangeLog.Cmd.Exceptions;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Cmd.Tests.Exceptions;

public sealed class SelfTestFailedExceptionTests : TestBase
{
    [Fact]
    public void DefaultConstructorCreatesException()
    {
        SelfTestFailedException exception = new();
        Assert.NotNull(exception);
    }

    [Fact]
    public void MessageConstructorCreatesExceptionWithMessage()
    {
        const string message = "Self-test failed";
        SelfTestFailedException exception = new(message);
        Assert.Equal(expected: message, actual: exception.Message);
    }

    [Fact]
    public void MessageAndInnerExceptionConstructorCreatesException()
    {
        const string message = "Self-test failed";
        InvalidOperationException innerException = new("inner");
        SelfTestFailedException exception = new(message: message, innerException: innerException);
        Assert.Equal(expected: message, actual: exception.Message);
        Assert.Same(expected: innerException, actual: exception.InnerException);
    }
}
