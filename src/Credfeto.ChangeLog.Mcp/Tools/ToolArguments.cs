using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Credfeto.ChangeLog.Mcp.Exceptions;

namespace Credfeto.ChangeLog.Mcp.Tools;

public sealed class ToolArguments
{
    private readonly IReadOnlyDictionary<string, JsonElement> _values;

    private ToolArguments(IReadOnlyDictionary<string, JsonElement> values)
    {
        this._values = values;
    }

    public static ToolArguments Parse(
        IReadOnlyList<SchemaProperty> schema,
        IEnumerable<KeyValuePair<string, JsonElement>>? arguments
    )
    {
        Dictionary<string, JsonElement> values = [with(StringComparer.Ordinal)];

        foreach (KeyValuePair<string, JsonElement> argument in arguments ?? [])
        {
            if (!schema.Any(property => StringComparer.Ordinal.Equals(x: property.Name, y: argument.Key)))
            {
                throw new ToolArgumentException(
                    "Unknown parameter; this tool takes: "
                        + string.Join(separator: ", ", schema.Select(property => property.Name))
                );
            }

            // A JSON null is how some clients say "not supplied"
            if (argument.Value.ValueKind != JsonValueKind.Null)
            {
                values[argument.Key] = argument.Value;
            }
        }

        return new(values);
    }

    public string? GetOptionalString(SchemaProperty property)
    {
        if (!this._values.TryGetValue(key: property.Name, value: out JsonElement value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new ToolArgumentException("Invalid value for " + property.Name + "; expected a string");
        }

        return value.GetString();
    }

    public string GetRequiredString(SchemaProperty property)
    {
        string? value = this.GetOptionalString(property);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ToolArgumentException("Missing required parameter " + property.Name);
        }

        return value;
    }

    public bool GetFlag(SchemaProperty property)
    {
        if (!this._values.TryGetValue(key: property.Name, value: out JsonElement value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new ToolArgumentException("Invalid value for " + property.Name + "; expected true or false"),
        };
    }
}
