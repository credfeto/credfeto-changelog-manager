using System;
using Credfeto.ChangeLog.Exceptions;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ExceptionConstructorTests : TestBase
{
    // ─── BranchMissingException ───────────────────────────────────────────────────

    [Fact]
    public void BranchMissingExceptionDefaultConstructorHasDefaultMessage()
    {
        BranchMissingException ex = new();
        Assert.Equal(expected: "Could not find branch", actual: ex.Message);
    }

    [Fact]
    public void BranchMissingExceptionWithMessageStoresMessage()
    {
        const string message = "Custom branch missing message";
        BranchMissingException ex = new(message);
        Assert.Equal(expected: message, actual: ex.Message);
    }

    [Fact]
    public void BranchMissingExceptionWithMessageAndInnerExceptionStoresBoth()
    {
        const string message = "Custom branch missing message";
        InvalidOperationException inner = new("inner");
        BranchMissingException ex = new(message, inner);
        Assert.Equal(expected: message, actual: ex.Message);
        Assert.Same(expected: inner, actual: ex.InnerException);
    }

    // ─── EmptyChangeLogException ──────────────────────────────────────────────────

    [Fact]
    public void EmptyChangeLogExceptionDefaultConstructorHasDefaultMessage()
    {
        EmptyChangeLogException ex = new();
        Assert.Equal(expected: "Changelog does not contain content.", actual: ex.Message);
    }

    [Fact]
    public void EmptyChangeLogExceptionWithMessageStoresMessage()
    {
        const string message = "Custom empty changelog message";
        EmptyChangeLogException ex = new(message);
        Assert.Equal(expected: message, actual: ex.Message);
    }

    [Fact]
    public void EmptyChangeLogExceptionWithMessageAndInnerExceptionStoresBoth()
    {
        const string message = "Custom empty changelog message";
        InvalidOperationException inner = new("inner");
        EmptyChangeLogException ex = new(message, inner);
        Assert.Equal(expected: message, actual: ex.Message);
        Assert.Same(expected: inner, actual: ex.InnerException);
    }

    // ─── InvalidChangeLogException ────────────────────────────────────────────────

    [Fact]
    public void InvalidChangeLogExceptionDefaultConstructorHasDefaultMessage()
    {
        InvalidChangeLogException ex = new();
        Assert.Equal(expected: "Invalid Changelog file", actual: ex.Message);
    }

    [Fact]
    public void InvalidChangeLogExceptionWithMessageStoresMessage()
    {
        const string message = "Custom invalid changelog message";
        InvalidChangeLogException ex = new(message);
        Assert.Equal(expected: message, actual: ex.Message);
    }

    [Fact]
    public void InvalidChangeLogExceptionWithMessageAndInnerExceptionStoresBoth()
    {
        const string message = "Custom invalid changelog message";
        InvalidOperationException inner = new("inner");
        InvalidChangeLogException ex = new(message, inner);
        Assert.Equal(expected: message, actual: ex.Message);
        Assert.Same(expected: inner, actual: ex.InnerException);
    }

    // ─── ReleaseAlreadyExistsException ────────────────────────────────────────────

    [Fact]
    public void ReleaseAlreadyExistsExceptionDefaultConstructorHasDefaultMessage()
    {
        ReleaseAlreadyExistsException ex = new();
        Assert.Equal(expected: "Release already exists.", actual: ex.Message);
    }

    [Fact]
    public void ReleaseAlreadyExistsExceptionWithMessageStoresMessage()
    {
        const string message = "Custom release exists message";
        ReleaseAlreadyExistsException ex = new(message);
        Assert.Equal(expected: message, actual: ex.Message);
    }

    [Fact]
    public void ReleaseAlreadyExistsExceptionWithMessageAndInnerExceptionStoresBoth()
    {
        const string message = "Custom release exists message";
        InvalidOperationException inner = new("inner");
        ReleaseAlreadyExistsException ex = new(message, inner);
        Assert.Equal(expected: message, actual: ex.Message);
        Assert.Same(expected: inner, actual: ex.InnerException);
    }

    // ─── GitRepositoryNotFoundException ───────────────────────────────────────────

    [Fact]
    public void GitRepositoryNotFoundExceptionDefaultConstructorHasDefaultMessage()
    {
        GitRepositoryNotFoundException ex = new();
        Assert.Equal(expected: "Could not find a usable git repository.", actual: ex.Message);
    }

    [Fact]
    public void GitRepositoryNotFoundExceptionWithMessageStoresMessage()
    {
        const string message = "Custom repository not found message";
        GitRepositoryNotFoundException ex = new(message);
        Assert.Equal(expected: message, actual: ex.Message);
    }

    [Fact]
    public void GitRepositoryNotFoundExceptionWithMessageAndInnerExceptionStoresBoth()
    {
        const string message = "Custom repository not found message";
        InvalidOperationException inner = new("inner");
        GitRepositoryNotFoundException ex = new(message, inner);
        Assert.Equal(expected: message, actual: ex.Message);
        Assert.Same(expected: inner, actual: ex.InnerException);
    }

    // ─── StartFolderMissingException ──────────────────────────────────────────────

    [Fact]
    public void StartFolderMissingExceptionDefaultConstructorHasDefaultMessage()
    {
        StartFolderMissingException ex = new();
        Assert.Equal(expected: "The folder this was started in no longer exists", actual: ex.Message);
    }

    [Fact]
    public void StartFolderMissingExceptionWithMessageStoresMessage()
    {
        const string message = "Custom start folder missing message";
        StartFolderMissingException ex = new(message);
        Assert.Equal(expected: message, actual: ex.Message);
    }

    [Fact]
    public void StartFolderMissingExceptionWithMessageAndInnerExceptionStoresBoth()
    {
        const string message = "Custom start folder missing message";
        InvalidOperationException inner = new("inner");
        StartFolderMissingException ex = new(message, inner);
        Assert.Equal(expected: message, actual: ex.Message);
        Assert.Same(expected: inner, actual: ex.InnerException);
    }

    // ─── ReleaseTooOldException ───────────────────────────────────────────────────

    [Fact]
    public void ReleaseTooOldExceptionDefaultConstructorHasDefaultMessage()
    {
        ReleaseTooOldException ex = new();
        Assert.Equal(expected: "Release is older than the current release.", actual: ex.Message);
    }

    [Fact]
    public void ReleaseTooOldExceptionWithMessageStoresMessage()
    {
        const string message = "Custom release too old message";
        ReleaseTooOldException ex = new(message);
        Assert.Equal(expected: message, actual: ex.Message);
    }

    [Fact]
    public void ReleaseTooOldExceptionWithMessageAndInnerExceptionStoresBoth()
    {
        const string message = "Custom release too old message";
        InvalidOperationException inner = new("inner");
        ReleaseTooOldException ex = new(message, inner);
        Assert.Equal(expected: message, actual: ex.Message);
        Assert.Same(expected: inner, actual: ex.InnerException);
    }
}
