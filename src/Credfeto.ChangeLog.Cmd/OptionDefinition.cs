using System;
using System.Diagnostics;

namespace Credfeto.ChangeLog.Cmd;

[DebuggerDisplay("LongName={LongName}, ShortName={ShortName}, IsCommand={IsCommand}, TakesValue={TakesValue}")]
public sealed record OptionDefinition(
    string LongName,
    char? ShortName,
    bool IsCommand,
    bool TakesValue,
    string HelpText,
    Func<Options, string, Options> Apply
);
