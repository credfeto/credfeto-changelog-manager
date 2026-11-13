using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Mcp.Models;

namespace Credfeto.ChangeLog.Mcp.Tools;

public sealed class ChangeLogCheckInsertTool : ChangeLogTool
{
    private const string DEFAULT_BRANCH = "origin/main";

    private readonly IChangeLogChecker _checker;

    public ChangeLogCheckInsertTool(ToolContext context, IChangeLogChecker checker)
        : base(
            context: context,
            name: "changelog_check_insert",
            title: "Check changelog changes",
            description: "Checks that the changes made to the changelog since the branch are only in the [Unreleased] section.",
            specificSchema: [ToolProperties.Branch],
            readOnly: true
        )
    {
        this._checker = checker;
    }

    protected override async ValueTask<string> ExecuteAsync(
        ChangeLogTarget target,
        ToolArguments arguments,
        CancellationToken cancellationToken
    )
    {
        string branch = arguments.GetOptionalString(ToolProperties.Branch) ?? DEFAULT_BRANCH;

        bool valid = await this._checker.ChangeLogModifiedInReleaseSectionAsync(
            changeLogFileName: target.FullPath,
            originBranchName: branch,
            language: target.Language,
            cancellationToken: cancellationToken
        );

        return JsonResult.Create(
            changeLog: target.RelativePath,
            writeProperties: writer =>
            {
                writer.WriteString(propertyName: ToolProperties.Branch.Name, value: branch);
                writer.WriteBoolean(propertyName: "valid", value: valid);
            }
        );
    }
}
