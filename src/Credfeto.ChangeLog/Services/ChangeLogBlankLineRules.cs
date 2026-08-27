namespace Credfeto.ChangeLog.Services;

// Shared by ChangeLogFixer and ChangeLogLinter so they can't drift on what counts as "needs no
// correction" for a CountBlankLinesBeforeHtmlComment result. Kept as its own type rather than an
// extension on int, since the -1/0/1 sentinel convention only has meaning for that one result,
// not for every int in scope.
internal static class ChangeLogBlankLineRules
{
    public static bool IsAlreadyOneBlankLineOrNoComment(int blankLineCount) => blankLineCount is < 0 or 1;
}
