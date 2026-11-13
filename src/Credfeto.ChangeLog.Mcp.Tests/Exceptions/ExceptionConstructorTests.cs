using System;
using Credfeto.ChangeLog.Mcp.Exceptions;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests.Exceptions;

public sealed class ExceptionConstructorTests : TestBase
{
    private const string MESSAGE = "Custom message";

    [Fact]
    public void ChangeLogPathExceptionDefaultConstructorHasDefaultMessage()
    {
        ChangeLogPathException ex = new();
        Assert.Equal(expected: "Invalid changelog path", actual: ex.Message);
    }

    [Fact]
    public void ChangeLogPathExceptionWithMessageStoresMessage()
    {
        ChangeLogPathException ex = new(MESSAGE);
        Assert.Equal(expected: MESSAGE, actual: ex.Message);
    }

    [Fact]
    public void ChangeLogPathExceptionWithMessageAndInnerExceptionStoresBoth()
    {
        InvalidOperationException inner = new("inner");
        ChangeLogPathException ex = new(MESSAGE, inner);
        Assert.Equal(expected: MESSAGE, actual: ex.Message);
        Assert.Same(expected: inner, actual: ex.InnerException);
    }

    [Fact]
    public void ToolArgumentExceptionDefaultConstructorHasDefaultMessage()
    {
        ToolArgumentException ex = new();
        Assert.Equal(expected: "Invalid tool argument", actual: ex.Message);
    }

    [Fact]
    public void ToolArgumentExceptionWithMessageStoresMessage()
    {
        ToolArgumentException ex = new(MESSAGE);
        Assert.Equal(expected: MESSAGE, actual: ex.Message);
    }

    [Fact]
    public void ToolArgumentExceptionWithMessageAndInnerExceptionStoresBoth()
    {
        InvalidOperationException inner = new("inner");
        ToolArgumentException ex = new(MESSAGE, inner);
        Assert.Equal(expected: MESSAGE, actual: ex.Message);
        Assert.Same(expected: inner, actual: ex.InnerException);
    }

    [Fact]
    public void RepositoryRootExceptionDefaultConstructorHasDefaultMessage()
    {
        RepositoryRootException ex = new();
        Assert.Equal(expected: "Invalid repository root", actual: ex.Message);
    }

    [Fact]
    public void RepositoryRootExceptionWithMessageStoresMessage()
    {
        RepositoryRootException ex = new(MESSAGE);
        Assert.Equal(expected: MESSAGE, actual: ex.Message);
    }

    [Fact]
    public void RepositoryRootExceptionWithMessageAndInnerExceptionStoresBoth()
    {
        InvalidOperationException inner = new("inner");
        RepositoryRootException ex = new(MESSAGE, inner);
        Assert.Equal(expected: MESSAGE, actual: ex.Message);
        Assert.Same(expected: inner, actual: ex.InnerException);
    }
}
