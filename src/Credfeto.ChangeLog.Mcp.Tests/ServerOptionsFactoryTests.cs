using System;
using System.Linq;
using System.Text.Json;
using Credfeto.ChangeLog.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests;

public sealed class ServerOptionsFactoryTests : ToolTestBase
{
    public ServerOptionsFactoryTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public void OptionsDescribeTheServerAndEveryTool()
    {
        using ServiceProvider services = this.BuildServices();

        McpServerOptions options = services.GetRequiredService<McpServerOptions>();

        Assert.Equal(expected: ServerOptionsFactory.SERVER_NAME, actual: options.ServerInfo?.Name);
        Assert.False(string.IsNullOrWhiteSpace(options.ServerInfo?.Version), userMessage: "Version should be set");
        Assert.False(string.IsNullOrWhiteSpace(options.ServerInstructions), userMessage: "Instructions should be set");
        Assert.Equal(
            expected:
            [
                "changelog_add",
                "changelog_check_insert",
                "changelog_create_release",
                "changelog_extract",
                "changelog_lint",
                "changelog_remove",
                "changelog_unreleased",
            ],
            actual: options.ToolCollection?.Select(tool => tool.ProtocolTool.Name).Order(StringComparer.Ordinal),
            comparer: StringComparer.Ordinal
        );
    }

    [Fact]
    public void ReadOnlyToolsAreAnnotated()
    {
        using ServiceProvider services = this.BuildServices();

        Tool extract = GetTool(services: services, name: "changelog_extract").ProtocolTool;
        Tool add = GetTool(services: services, name: "changelog_add").ProtocolTool;

        Assert.True(condition: extract.Annotations?.ReadOnlyHint ?? false, userMessage: "extract is read only");
        Assert.False(condition: add.Annotations?.ReadOnlyHint ?? true, userMessage: "add changes the file");
    }

    [Fact]
    public void ToolsHaveNoAttributeMetadata()
    {
        using ServiceProvider services = this.BuildServices();

        Assert.Empty(GetTool(services: services, name: "changelog_lint").Metadata);
    }

    [Fact]
    public void SchemaListsPropertiesAndRequiredParameters()
    {
        JsonElement schema = ToolSchema.Create([ToolProperties.ChangeLog, ToolProperties.Message, ToolProperties.Fix]);

        Assert.Equal(expected: "object", actual: schema.GetProperty("type").GetString());
        Assert.False(
            condition: schema.GetProperty("additionalProperties").GetBoolean(),
            userMessage: "Unknown properties should be refused"
        );

        JsonElement properties = schema.GetProperty("properties");
        Assert.Equal(expected: "string", actual: properties.GetProperty("changelog").GetProperty("type").GetString());
        Assert.Equal(expected: "boolean", actual: properties.GetProperty("fix").GetProperty("type").GetString());
        Assert.False(
            condition: properties.GetProperty("fix").GetProperty("default").GetBoolean(),
            userMessage: "Flags default to false"
        );

        JsonElement required = Assert.Single(schema.GetProperty("required").EnumerateArray());
        Assert.Equal(expected: "message", actual: required.GetString());
    }

    [Fact]
    public void SchemaWithNoRequiredParametersHasNoRequiredList()
    {
        JsonElement schema = ToolSchema.Create([ToolProperties.ChangeLog]);

        Assert.False(
            condition: schema.TryGetProperty(propertyName: "required", value: out _),
            userMessage: "No required list expected"
        );
    }
}
