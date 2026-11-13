using System;
using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Credfeto.ChangeLog.Mcp.Tools;

public static class JsonResult
{
    private const string CHANGELOG_PROPERTY = "changeLog";

    public static string Create(string changeLog, Action<Utf8JsonWriter> writeProperties)
    {
        ArrayBufferWriter<byte> buffer = new();

        using (Utf8JsonWriter writer = new(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString(propertyName: CHANGELOG_PROPERTY, value: changeLog);
            writeProperties(writer);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
