using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Credfeto.ChangeLog.Cmd.Exceptions;

namespace Credfeto.ChangeLog.Cmd;

public static class OptionsParser
{
    private const string LONG_PREFIX = "--";
    private const string SHORT_PREFIX = "-";
    private const char INLINE_VALUE_SEPARATOR = '=';
    private const char LIST_SEPARATOR = ',';

    private static readonly IReadOnlyList<OptionDefinition> Definitions =
    [
        Value(
            longName: "changelog",
            shortName: 'f',
            isCommand: false,
            helpText: "The changelog filename to use",
            apply: static (o, v) => o with { ChangeLog = v }
        ),
        Value(
            longName: "version",
            shortName: 'v',
            isCommand: true,
            helpText: "The version to extract",
            apply: static (o, v) => o with { Version = v }
        ),
        Value(
            longName: "extract",
            shortName: 'x',
            isCommand: true,
            helpText: "The filename to write the extracted",
            apply: static (o, v) => o with { Extract = v }
        ),
        Value(
            longName: "remove",
            shortName: 'r',
            isCommand: true,
            helpText: "The entry type to remove",
            apply: static (o, v) => o with { Remove = v }
        ),
        Value(
            longName: "add",
            shortName: 'a',
            isCommand: true,
            helpText: "The entry type to add",
            apply: static (o, v) => o with { Add = v }
        ),
        Value(
            longName: "message",
            shortName: 'm',
            isCommand: false,
            helpText: "The message to add",
            apply: static (o, v) => o with { Message = v }
        ),
        Value(
            longName: "check-insert",
            shortName: 't',
            isCommand: true,
            helpText: "The branch to check the changelog again",
            apply: static (o, v) => o with { CheckInsert = v }
        ),
        Value(
            longName: "create-release",
            shortName: 'c',
            isCommand: true,
            helpText: "The release version to create",
            apply: static (o, v) => o with { CreateRelease = v }
        ),
        Switch(
            longName: "Pending",
            shortName: 'p',
            isCommand: false,
            helpText: "If the release is pending",
            apply: static o => o with { Pending = true }
        ),
        Switch(
            longName: "un-released",
            shortName: 'u',
            isCommand: true,
            helpText: "Prints the unreleased section to the console.",
            apply: static o => o with { DisplayUnreleased = true }
        ),
        Switch(
            longName: "lint",
            shortName: 'l',
            isCommand: true,
            helpText: "Lint the changelog for correctness",
            apply: static o => o with { Lint = true }
        ),
        Value(
            longName: "additional-sections",
            shortName: null,
            isCommand: false,
            helpText: "Additional change type sections allowed in unreleased (comma separated)",
            apply: static (o, v) => o with { AdditionalSections = SplitList(v) }
        ),
        Switch(
            longName: "fix",
            shortName: null,
            isCommand: false,
            helpText: "When used with --lint, rewrite the file to fix formatting issues",
            apply: static o => o with { Fix = true }
        ),
        Switch(
            longName: "self-test",
            shortName: null,
            isCommand: true,
            helpText: "Verify the tool's dependencies load, without touching any file, then exit",
            apply: static o => o with { SelfTest = true }
        ),
        Switch(
            longName: "help",
            shortName: null,
            isCommand: false,
            helpText: "Display this help",
            apply: static o => o with { ShowHelp = true }
        ),
    ];

    private static readonly FrozenDictionary<string, OptionDefinition> DefinitionsByToken = BuildLookup(Definitions);

    public static string Usage { get; } = BuildUsage(Definitions);

    public static Options Parse(IReadOnlyList<string> args)
    {
        Options options = new();
        HashSet<string> seen = [with(StringComparer.Ordinal)];

        for (int index = 0; index < args.Count; ++index)
        {
            (OptionDefinition definition, string? inlineValue) = FindDefinition(args[index]);

            if (!seen.Add(definition.LongName))
            {
                throw new InvalidOptionsException($"Option --{definition.LongName} was specified more than once");
            }

            string value = definition.TakesValue
                ? inlineValue ?? ReadFollowingValue(args: args, index: ref index, definition: definition)
                : EnsureNoValue(definition: definition, inlineValue: inlineValue);

            options = definition.Apply(arg1: options, arg2: value);
        }

        EnsureCommandSpecified(options: options, seen: seen);

        return options;
    }

    private static (OptionDefinition Definition, string? InlineValue) FindDefinition(string arg)
    {
        (string token, string? inlineValue) = SplitInlineValue(arg);

        if (DefinitionsByToken.TryGetValue(key: token, out OptionDefinition? definition))
        {
            return (definition, inlineValue);
        }

        throw new InvalidOptionsException($"Unknown option {arg}");
    }

    private static (string Token, string? InlineValue) SplitInlineValue(string arg)
    {
        if (!arg.StartsWith(value: LONG_PREFIX, comparisonType: StringComparison.Ordinal))
        {
            return (arg, null);
        }

        string[] parts = arg.Split(separator: INLINE_VALUE_SEPARATOR, count: 2);

        return parts.Length == 2 ? (parts[0], parts[1]) : (arg, null);
    }

    private static string ReadFollowingValue(IReadOnlyList<string> args, ref int index, OptionDefinition definition)
    {
        int valueIndex = index + 1;

        if (valueIndex >= args.Count)
        {
            throw new InvalidOptionsException($"Option --{definition.LongName} requires a value");
        }

        index = valueIndex;

        return args[valueIndex];
    }

    private static string EnsureNoValue(OptionDefinition definition, string? inlineValue)
    {
        if (inlineValue is not null)
        {
            throw new InvalidOptionsException($"Option --{definition.LongName} does not take a value");
        }

        return string.Empty;
    }

    private static void EnsureCommandSpecified(Options options, HashSet<string> seen)
    {
        if (
            options.ShowHelp
            || Definitions.Any(definition => definition.IsCommand && seen.Contains(definition.LongName))
        )
        {
            return;
        }

        string commands = string.Join(
            separator: ", ",
            Definitions
                .Where(static definition => definition.IsCommand)
                .Select(static definition => LONG_PREFIX + definition.LongName)
        );

        throw new InvalidOptionsException($"At least one command must be specified: {commands}");
    }

    private static IReadOnlyList<string> SplitList(string value)
    {
        return value.Split(
            separator: LIST_SEPARATOR,
            options: StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
    }

    private static OptionDefinition Value(
        string longName,
        char? shortName,
        bool isCommand,
        string helpText,
        Func<Options, string, Options> apply
    )
    {
        return new(
            LongName: longName,
            ShortName: shortName,
            IsCommand: isCommand,
            TakesValue: true,
            HelpText: helpText,
            Apply: apply
        );
    }

    private static OptionDefinition Switch(
        string longName,
        char? shortName,
        bool isCommand,
        string helpText,
        Func<Options, Options> apply
    )
    {
        return new(
            LongName: longName,
            ShortName: shortName,
            IsCommand: isCommand,
            TakesValue: false,
            HelpText: helpText,
            Apply: (o, _) => apply(o)
        );
    }

    private static FrozenDictionary<string, OptionDefinition> BuildLookup(IReadOnlyList<OptionDefinition> definitions)
    {
        return definitions
            .SelectMany(static definition =>
                Tokens(definition).Select(token => KeyValuePair.Create(key: token, value: definition))
            )
            .ToFrozenDictionary(comparer: StringComparer.Ordinal);
    }

    private static IEnumerable<string> Tokens(OptionDefinition definition)
    {
        yield return LONG_PREFIX + definition.LongName;

        if (definition.ShortName is { } shortName)
        {
            yield return SHORT_PREFIX + shortName;
        }
    }

    private static string BuildUsage(IReadOnlyList<OptionDefinition> definitions)
    {
        IReadOnlyList<(string Syntax, string HelpText)> rows =
        [
            .. definitions.Select(static definition => (Syntax(definition), definition.HelpText)),
        ];
        int width = rows.Max(static row => row.Syntax.Length);

        StringBuilder usage = new StringBuilder()
            .AppendLine("Usage: changelog [options]")
            .AppendLine()
            .AppendLine("Options:");

        foreach ((string syntax, string helpText) in rows)
        {
            usage = usage.Append("  ").Append(syntax.PadRight(width)).Append("  ").AppendLine(helpText);
        }

        return usage.ToString();
    }

    private static string Syntax(OptionDefinition definition)
    {
        string shortForm = definition.ShortName is { } shortName ? SHORT_PREFIX + shortName + ", " : "    ";
        string valueForm = definition.TakesValue ? " <value>" : string.Empty;

        return shortForm + LONG_PREFIX + definition.LongName + valueForm;
    }
}
