using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Credfeto.ChangeLog.Mcp.Tools;

public static class ToolSchema
{
    public static JsonElement Create(IReadOnlyList<SchemaProperty> properties)
    {
        ArrayBufferWriter<byte> buffer = new();

        using (Utf8JsonWriter writer = new(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString(propertyName: "type", value: "object");
            WriteProperties(writer: writer, properties: properties);
            WriteRequired(writer: writer, properties: properties);
            writer.WriteBoolean(propertyName: "additionalProperties", value: false);
            writer.WriteEndObject();
        }

        using JsonDocument document = JsonDocument.Parse(buffer.WrittenMemory);

        return document.RootElement.Clone();
    }

    private static void WriteProperties(Utf8JsonWriter writer, IReadOnlyList<SchemaProperty> properties)
    {
        writer.WriteStartObject("properties");

        foreach (SchemaProperty property in properties)
        {
            writer.WriteStartObject(property.Name);
            writer.WriteString(propertyName: "type", value: property.IsFlag ? "boolean" : "string");
            writer.WriteString(propertyName: "description", value: property.Description);

            if (property.IsFlag)
            {
                writer.WriteBoolean(propertyName: "default", value: false);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static void WriteRequired(Utf8JsonWriter writer, IReadOnlyList<SchemaProperty> properties)
    {
        IReadOnlyList<string> required =
        [
            .. properties.Where(property => property.Required).Select(property => property.Name),
        ];

        if (required.Count == 0)
        {
            return;
        }

        writer.WriteStartArray("required");

        foreach (string name in required)
        {
            writer.WriteStringValue(name);
        }

        writer.WriteEndArray();
    }
}
