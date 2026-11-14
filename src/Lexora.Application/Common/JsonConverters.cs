using System.Text.Json;
using System.Text.Json.Serialization;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Domain.Enums;

namespace Lexora.Application.Common;

public class FlexiblePartOfSpeechJsonConverter : JsonConverter<PartOfSpeech>
{
    public override PartOfSpeech Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return PartOfSpeech.Noun;
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out int intVal) && Enum.IsDefined(typeof(PartOfSpeech), intVal))
            {
                return (PartOfSpeech)intVal;
            }
            return PartOfSpeech.Noun;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            return ExportImportService.ParsePartOfSpeech(str, PartOfSpeech.Noun);
        }

        return PartOfSpeech.Noun;
    }

    public override void Write(Utf8JsonWriter writer, PartOfSpeech value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class FlexibleCefrLevelJsonConverter : JsonConverter<CefrLevel>
{
    public override CefrLevel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return CefrLevel.None;
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out int intVal) && Enum.IsDefined(typeof(CefrLevel), intVal))
            {
                return (CefrLevel)intVal;
            }
            return CefrLevel.None;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            return ExportImportService.ParseCefrLevel(str, CefrLevel.None);
        }

        return CefrLevel.None;
    }

    public override void Write(Utf8JsonWriter writer, CefrLevel value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class FlexibleStringListJsonConverter : JsonConverter<List<string>>
{
    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new List<string>();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str)) return new List<string>();
            return str.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<string>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) break;
                if (reader.TokenType == JsonTokenType.String)
                {
                    var val = reader.GetString();
                    if (!string.IsNullOrWhiteSpace(val)) list.Add(val.Trim());
                }
            }
            return list;
        }

        return new List<string>();
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStringValue(item);
        }
        writer.WriteEndArray();
    }
}

public class FlexibleExampleListJsonConverter : JsonConverter<List<ExampleExportModel>>
{
    public override List<ExampleExportModel> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = new List<ExampleExportModel>();
        if (reader.TokenType == JsonTokenType.Null)
        {
            return list;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (!string.IsNullOrWhiteSpace(str))
            {
                list.Add(new ExampleExportModel { SentenceEn = str.Trim() });
            }
            return list;
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) break;

                if (reader.TokenType == JsonTokenType.String)
                {
                    var str = reader.GetString();
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        list.Add(new ExampleExportModel { SentenceEn = str.Trim() });
                    }
                }
                else if (reader.TokenType == JsonTokenType.StartObject)
                {
                    using var objDoc = JsonDocument.ParseValue(ref reader);
                    var el = objDoc.RootElement;
                    string en = string.Empty;
                    string vi = string.Empty;

                    if (el.TryGetProperty("sentenceEn", out var enProp)) en = enProp.GetString() ?? string.Empty;
                    else if (el.TryGetProperty("en", out var enP2)) en = enP2.GetString() ?? string.Empty;
                    else if (el.TryGetProperty("sentence", out var sProp)) en = sProp.GetString() ?? string.Empty;

                    if (el.TryGetProperty("sentenceVi", out var viProp)) vi = viProp.GetString() ?? string.Empty;
                    else if (el.TryGetProperty("vi", out var viP2)) vi = viP2.GetString() ?? string.Empty;
                    else if (el.TryGetProperty("translation", out var tProp)) vi = tProp.GetString() ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(en) || !string.IsNullOrWhiteSpace(vi))
                    {
                        list.Add(new ExampleExportModel
                        {
                            SentenceEn = en.Trim(),
                            SentenceVi = vi.Trim()
                        });
                    }
                }
            }
            return list;
        }

        return list;
    }

    public override void Write(Utf8JsonWriter writer, List<ExampleExportModel> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        if (value != null)
        {
            foreach (var item in value)
            {
                writer.WriteStartObject();
                writer.WriteString("sentenceEn", item.SentenceEn ?? string.Empty);
                writer.WriteString("sentenceVi", item.SentenceVi ?? string.Empty);
                writer.WriteEndObject();
            }
        }
        writer.WriteEndArray();
    }
}
