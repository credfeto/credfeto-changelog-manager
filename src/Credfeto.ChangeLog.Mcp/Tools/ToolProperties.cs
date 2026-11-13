namespace Credfeto.ChangeLog.Mcp.Tools;

public static class ToolProperties
{
    public static readonly SchemaProperty ChangeLog = new(
        Name: "changelog",
        Description: "Path to the CHANGELOG.md file, relative to the repository the server was started in. Found automatically when omitted.",
        Required: false,
        IsFlag: false
    );

    public static readonly SchemaProperty AdditionalSections = new(
        Name: "additionalSections",
        Description: "Comma-separated list of extra section names allowed in the changelog.",
        Required: false,
        IsFlag: false
    );

    public static readonly SchemaProperty Type = new(
        Name: "type",
        Description: "The section of the [Unreleased] section to change, for example Added, Changed, Fixed or Removed.",
        Required: true,
        IsFlag: false
    );

    public static readonly SchemaProperty Message = new(
        Name: "message",
        Description: "The text of the entry.",
        Required: true,
        IsFlag: false
    );

    public static readonly SchemaProperty Version = new(
        Name: "version",
        Description: "The release version, for example 1.2.3.",
        Required: true,
        IsFlag: false
    );

    public static readonly SchemaProperty Pending = new(
        Name: "pending",
        Description: "When true the release date is left as pending instead of today.",
        Required: false,
        IsFlag: true
    );

    public static readonly SchemaProperty Branch = new(
        Name: "branch",
        Description: "The branch the changes will be merged into. Defaults to origin/main.",
        Required: false,
        IsFlag: false
    );

    public static readonly SchemaProperty Fix = new(
        Name: "fix",
        Description: "When true the changelog is rewritten to fix the problems found, then checked again.",
        Required: false,
        IsFlag: true
    );
}
