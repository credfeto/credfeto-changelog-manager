using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Mcp.Tools;
using FunFair.Test.Common;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests;

public abstract class ToolTestBase : LoggingFolderCleanupTestBase
{
    protected const string CHANGELOG = "CHANGELOG.md";

    protected ToolTestBase(ITestOutputHelper output)
        : base(output)
    {
        this.RepositoryFolder = Path.Combine(path1: this.TempFolder, path2: "repo");
        Directory.CreateDirectory(this.RepositoryFolder);
        Repository.Init(this.RepositoryFolder);
    }

    protected string RepositoryFolder { get; }

    protected string ChangeLogPath => Path.Combine(path1: this.RepositoryFolder, path2: CHANGELOG);

    protected ServiceProvider BuildServices(IChangeLogDetector? detector = null)
    {
        return new ServiceCollection()
            .AddChangeLogMcpServer(workingDirectory: this.RepositoryFolder, repositoryRoot: null)
            .AddSingleton(detector ?? new FixedChangeLogDetector(this.ChangeLogPath))
            .BuildServiceProvider();
    }

    protected static ChangeLogTool GetTool(ServiceProvider services, string name)
    {
        return services
            .GetServices<McpServerTool>()
            .OfType<ChangeLogTool>()
            .Single(tool => StringComparer.Ordinal.Equals(x: tool.ProtocolTool.Name, y: name));
    }

    protected async Task<(string Text, bool IsError)> CallAsync(
        string name,
        IReadOnlyList<KeyValuePair<string, JsonElement>> arguments,
        IChangeLogDetector? detector = null
    )
    {
        await using ServiceProvider services = this.BuildServices(detector);
        ChangeLogTool tool = GetTool(services: services, name: name);

        CallToolResult result = await tool.CallAsync(arguments: arguments, cancellationToken: this.CancellationToken());

        string text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;

        return (text, result.IsError ?? false);
    }

    protected async Task<string> CallSuccessfullyAsync(
        string name,
        IReadOnlyList<KeyValuePair<string, JsonElement>> arguments
    )
    {
        (string text, bool isError) = await this.CallAsync(name: name, arguments: arguments);

        Assert.False(condition: isError, userMessage: text);

        return text;
    }

    protected async Task<string> CallFailingAsync(
        string name,
        IReadOnlyList<KeyValuePair<string, JsonElement>> arguments
    )
    {
        (string text, bool isError) = await this.CallAsync(name: name, arguments: arguments);

        Assert.True(condition: isError, userMessage: text);

        return text;
    }

    protected Task<string> AddEntryAsync(string type, string message)
    {
        return this.CallSuccessfullyAsync(
            name: "changelog_add",
            arguments: [Text("type", type), Text("message", message)]
        );
    }

    protected Task<string> ReadChangeLogAsync()
    {
        return File.ReadAllTextAsync(path: this.ChangeLogPath, cancellationToken: this.CancellationToken());
    }

    protected Task WriteChangeLogAsync(string content)
    {
        // Raw string literals take the line endings of the test source file's checkout, but the tools write
        // Environment.NewLine, so an unnormalised fixture makes git see every line as changed.
        return File.WriteAllTextAsync(
            path: this.ChangeLogPath,
            contents: content.ReplaceLineEndings(Environment.NewLine),
            cancellationToken: this.CancellationToken()
        );
    }

    protected static KeyValuePair<string, JsonElement> Text(string name, string value)
    {
        ArrayBufferWriter<byte> buffer = new();

        using (Utf8JsonWriter writer = new(buffer))
        {
            writer.WriteStringValue(value);
        }

        return Parsed(name: name, json: Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    protected static KeyValuePair<string, JsonElement> Flag(string name, bool value)
    {
        return Parsed(name: name, json: value ? "true" : "false");
    }

    protected static KeyValuePair<string, JsonElement> Number(string name, int value)
    {
        return Parsed(name: name, json: value.ToString(CultureInfo.InvariantCulture));
    }

    protected static KeyValuePair<string, JsonElement> Null(string name)
    {
        return Parsed(name: name, json: "null");
    }

    protected static JsonElement Property(string json, string name)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        return document.RootElement.GetProperty(name).Clone();
    }

    private static KeyValuePair<string, JsonElement> Parsed(string name, string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        return KeyValuePair.Create(key: name, value: document.RootElement.Clone());
    }
}
