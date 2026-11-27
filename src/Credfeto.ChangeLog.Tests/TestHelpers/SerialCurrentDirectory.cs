using Xunit;

namespace Credfeto.ChangeLog.Tests.TestHelpers;

// The current folder is shared by every test in the run. Tests that delete it share this test
// collection, which xunit runs on its own after the parallel collections, so no other test
// resolves a relative path against a folder that has gone.
[CollectionDefinition(Name, DisableParallelization = true)]
public static class SerialCurrentDirectory
{
    public const string Name = "CurrentDirectory";
}
