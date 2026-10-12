using System;
using System.Collections.Generic;
using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

// These cached compatibility catalogs contain only a flat object of string values.
// Decode JSON once, independently of regex syntax and without a framework dependency.
internal static class CompatibilityFlatJson
{
    internal static Dictionary<string, string> Parse(string json)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(json)) return values;
        int index = 0;
        SkipWhitespace(json, ref index);
        Require(json, ref index, '{');
        SkipWhitespace(json, ref index);
        if (index < json.Length && json[index] == '}')
        {
            index++;
        }
        else
        {
            while (true)
            {
                string key = ReadString(json, ref index);
                SkipWhitespace(json, ref index);
                Require(json, ref index, ':');
                SkipWhitespace(json, ref index);
                values[key] = ReadString(json, ref index);
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == '}')
                {
                    index++;
                    break;
                }
                Require(json, ref index, ',');
                SkipWhitespace(json, ref index);
            }
        }
        SkipWhitespace(json, ref index);
        if (index != json.Length) throw InvalidJson();
        return values;
    }

    private static string ReadString(string json, ref int index)
    {
        Require(json, ref index, '"');
        int segmentStart = index;
        StringBuilder decoded = null;
        while (index < json.Length)
        {
            char value = json[index++];
            if (value == '"')
            {
                int length = index - segmentStart - 1;
                return decoded == null
                    ? json.Substring(segmentStart, length)
                    : decoded.Append(json, segmentStart, length).ToString();
            }
            if (value < 0x20) throw InvalidJson();
            if (value != '\\') continue;
            if (decoded == null) decoded = new StringBuilder();
            decoded.Append(json, segmentStart, index - segmentStart - 1);
            if (index >= json.Length) throw InvalidJson();
            switch (json[index++])
            {
                case '"': decoded.Append('"'); break;
                case '\\': decoded.Append('\\'); break;
                case '/': decoded.Append('/'); break;
                case 'b': decoded.Append('\b'); break;
                case 'f': decoded.Append('\f'); break;
                case 'n': decoded.Append('\n'); break;
                case 'r': decoded.Append('\r'); break;
                case 't': decoded.Append('\t'); break;
                case 'u':
                    int codeUnit = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (index >= json.Length) throw InvalidJson();
                        char digit = json[index++];
                        int hex = digit >= '0' && digit <= '9' ? digit - '0'
                            : digit >= 'a' && digit <= 'f' ? digit - 'a' + 10
                            : digit >= 'A' && digit <= 'F' ? digit - 'A' + 10 : -1;
                        if (hex < 0) throw InvalidJson();
                        codeUnit = codeUnit * 16 + hex;
                    }
                    decoded.Append((char)codeUnit);
                    break;
                default: throw InvalidJson();
            }
            segmentStart = index;
        }
        throw InvalidJson();
    }

    private static void SkipWhitespace(string json, ref int index)
    {
        while (index < json.Length)
        {
            char value = json[index];
            if (value != ' ' && value != '\t' && value != '\r' && value != '\n') break;
            index++;
        }
    }

    private static void Require(string json, ref int index, char expected)
    {
        if (index >= json.Length || json[index++] != expected) throw InvalidJson();
    }

    private static FormatException InvalidJson()
        => new FormatException("Expected a flat JSON object containing string values.");
}
