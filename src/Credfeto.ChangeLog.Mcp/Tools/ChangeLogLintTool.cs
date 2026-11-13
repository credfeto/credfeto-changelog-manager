using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Mcp.Models;
using Credfeto.ChangeLog.Models;

namespace Credfeto.ChangeLog.Mcp.Tools;

public sealed class ChangeLogLintTool : ChangeLogTool
{
    private readonly IChangeLogFixer _fixer;
    private readonly IChangeLogLinter _linter;

    public ChangeLogLintTool(ToolContext context, IChangeLogLinter linter, IChangeLogFixer fixer)
        : base(
            context: context,
            name: "changelog_lint",
            title: "Lint changelog",
            description: "Checks the changelog for formatting problems, optionally fixing them first.",
            specificSchema: [ToolProperties.Fix],
            readOnly: false
        )
    {
        this._linter = linter;
        this._fixer = fixer;
    }

    protected override async ValueTask<string> ExecuteAsync(
        ChangeLogTarget target,
        ToolArguments arguments,
        CancellationToken cancellationToken
    )
    {
        bool fix = arguments.GetFlag(ToolProperties.Fix);

        if (fix)
        {
            await this._fixer.FixAsync(
                changeLogFileName: target.FullPath,
                language: target.Language,
                cancellationToken: cancellationToken
            );
        }

        IReadOnlyList<LintError> errors = await this._linter.LintAsync(
            changeLogFileName: target.FullPath,
            language: target.Language,
            cancellationToken: cancellationToken
        );

        return JsonResult.Create(
            changeLog: target.RelativePath,
            writeProperties: writer => WriteLintResult(writer: writer, fix: fix, errors: errors)
        );
    }

    private static void WriteLintResult(Utf8JsonWriter writer, bool fix, IReadOnlyList<LintError> errors)
    {
        writer.WriteBoolean(propertyName: "valid", value: errors.Count == 0);
        writer.WriteBoolean(propertyName: "fixed", value: fix);
        writer.WriteStartArray("errors");

        foreach (LintError error in errors)
        {
            writer.WriteStartObject();
            writer.WriteNumber(propertyName: "lineNumber", value: error.LineNumber);
            writer.WriteString(propertyName: "message", value: error.Message);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }
}
