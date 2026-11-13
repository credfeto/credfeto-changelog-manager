using System.IO;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Mcp.Paths;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests;

public sealed class ServerSetupTests : ToolTestBase
{
    public ServerSetupTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public async Task RepositoryRootWithParentSegmentAndTrailingSeparatorDetectsTheNormalisedRootAsync()
    {
        string root = this.CreateNestedRoot();
        string changeLog = Path.Combine(path1: root, path2: CHANGELOG);
        await File.WriteAllTextAsync(
            path: changeLog,
            contents: "# Changelog",
            cancellationToken: this.CancellationToken()
        );
        Directory.CreateDirectory(Path.Combine(path1: root, path2: "sub"));

        ChangeLogPathResolver resolver = await this.BuildResolverAsync(
            Path.Combine(path1: root, path2: "sub", path3: "..") + Path.DirectorySeparatorChar
        );

        Assert.Equal(expected: root, actual: resolver.RepositoryRoot);
        Assert.Equal(expected: changeLog, actual: resolver.Resolve(null));
    }

    private async ValueTask<ChangeLogPathResolver> BuildResolverAsync(string repositoryRoot)
    {
        await using ServiceProvider services = new ServiceCollection()
            .AddChangeLogMcpServer(workingDirectory: this.TempFolder, repositoryRoot: repositoryRoot)
            .BuildServiceProvider();

        return services.GetRequiredService<ChangeLogPathResolver>();
    }

    // A folder inside the test repository, so any walk up to the enclosing .git would be visible
    private string CreateNestedRoot()
    {
        string root = Path.Combine(path1: this.RepositoryFolder, path2: "nested");
        Directory.CreateDirectory(root);

        return root;
    }
}
