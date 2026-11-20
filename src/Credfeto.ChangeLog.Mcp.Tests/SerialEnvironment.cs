using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests;

// The process environment is shared by every test in the run. Tests that set an environment
// variable share this test collection, which xunit runs on its own after the parallel
// collections, so no other test sees the temporary value.
[CollectionDefinition(Name, DisableParallelization = true)]
public static class SerialEnvironment
{
    public const string Name = "Environment";
}
