using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Runtime;

// Shared text codec: no live game state, no transport or domain policy.
internal static class JsonResponseTextCodec
{
internal static string StripJsonResponseEnvelope(string content)
	{
		string text = (content ?? "").Trim('\uFEFF', '\u200B', '\u200C', '\u200D', ' ', '\t', '\r', '\n');
		if (text.StartsWith("```", StringComparison.Ordinal))
		{
			int firstLineEnd = text.IndexOf('\n');
			if (firstLineEnd >= 0)
			{
				text = text.Substring(firstLineEnd + 1).Trim();
			}
			int lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
			if (lastFence >= 0)
			{
				text = text.Substring(0, lastFence).Trim();
			}
		}
		text = Regex.Replace(text, "^(?:json)\\s*(?=[\\r\\n{\\[])", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Trim();
		return text;
	}

internal static List<string> ExtractJsonObjectPayloads(string text)
	{
		List<string> list = new List<string>();
		text = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return list;
		}
		bool inString = false;
		bool escaped = false;
		int depth = 0;
		int start = -1;
		for (int i = 0; i < text.Length; i++)
		{
			char ch = text[i];
			if (depth == 0)
			{
				if (ch == '{')
				{
					start = i;
					depth = 1;
					inString = false;
					escaped = false;
				}
				continue;
			}
			if (inString)
			{
				if (escaped)
				{
					escaped = false;
				}
				else if (ch == '\\')
				{
					escaped = true;
				}
				else if (ch == '"')
				{
					inString = false;
				}
				continue;
			}
			if (ch == '"')
			{
				inString = true;
				continue;
			}
			if (ch == '{')
			{
				depth++;
				continue;
			}
			if (ch == '}')
			{
				depth--;
				if (depth == 0)
				{
					if (start >= 0)
					{
						list.Add(text.Substring(start, i - start + 1).Trim());
					}
					start = -1;
				}
				if (depth < 0)
				{
					depth = 0;
					start = -1;
				}
			}
		}
		return list;
	}

internal static bool TryExtractLooseJsonStringProperty(string text, string propertyName, out string value)
	{
		value = "";
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(propertyName))
		{
			return false;
		}
		string key = "\"" + propertyName + "\"";
		int searchIndex = 0;
		while (searchIndex < text.Length)
		{
			int keyIndex = text.IndexOf(key, searchIndex, StringComparison.OrdinalIgnoreCase);
			if (keyIndex < 0)
			{
				return false;
			}
			int colonIndex = SkipJsonWhitespace(text, keyIndex + key.Length);
			if (colonIndex >= text.Length || text[colonIndex] != ':')
			{
				searchIndex = keyIndex + key.Length;
				continue;
			}
			int valueStart = SkipJsonWhitespace(text, colonIndex + 1);
			if (valueStart >= text.Length || text[valueStart] != '"')
			{
				searchIndex = keyIndex + key.Length;
				continue;
			}
			if (TryReadLooseJsonStringValue(text, valueStart, out value))
			{
				return true;
			}
			searchIndex = keyIndex + key.Length;
		}
		return false;
	}

internal static bool TryReadLooseJsonStringValue(string text, int quoteIndex, out string value)
	{
		value = "";
		if (string.IsNullOrEmpty(text) || quoteIndex < 0 || quoteIndex >= text.Length || text[quoteIndex] != '"')
		{
			return false;
		}
		StringBuilder stringBuilder = new StringBuilder();
		bool escaped = false;
		for (int i = quoteIndex + 1; i < text.Length; i++)
		{
			char ch = text[i];
			if (escaped)
			{
				switch (ch)
				{
				case '"':
				case '\\':
				case '/':
					stringBuilder.Append(ch);
					break;
				case 'b':
					stringBuilder.Append('\b');
					break;
				case 'f':
					stringBuilder.Append('\f');
					break;
				case 'n':
					stringBuilder.Append('\n');
					break;
				case 'r':
					stringBuilder.Append('\r');
					break;
				case 't':
					stringBuilder.Append('\t');
					break;
				case 'u':
					if (i + 4 < text.Length && int.TryParse(text.Substring(i + 1, 4), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var codePoint))
					{
						stringBuilder.Append((char)codePoint);
						i += 4;
					}
					else
					{
						stringBuilder.Append(ch);
					}
					break;
				default:
					stringBuilder.Append(ch);
					break;
				}
				escaped = false;
				continue;
			}
			if (ch == '\\')
			{
				escaped = true;
				continue;
			}
			if (ch == '"')
			{
				int next = SkipJsonWhitespace(text, i + 1);
				if (next >= text.Length || text[next] == ',' || text[next] == '}' || text[next] == ']')
				{
					value = stringBuilder.ToString();
					return !string.IsNullOrWhiteSpace(value);
				}
			}
			if (ch == '\r' || ch == '\n')
			{
				stringBuilder.Append(' ');
			}
			else
			{
				stringBuilder.Append(ch);
			}
		}
		value = stringBuilder.ToString();
		return !string.IsNullOrWhiteSpace(value);
	}

internal static int SkipJsonWhitespace(string text, int index)
	{
		while (index < (text?.Length ?? 0) && char.IsWhiteSpace(text[index]))
		{
			index++;
		}
		return index;
	}

internal static JToken GetJsonPropertyIgnoreCase(JObject obj, params string[] names)
	{
		if (obj == null || names == null)
		{
			return null;
		}
		foreach (string name in names)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				continue;
			}
			JProperty prop = obj.Properties().FirstOrDefault((JProperty x) => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
			if (prop != null)
			{
				return prop.Value;
			}
		}
		return null;
	}

internal static string GetJsonStringIgnoreCase(JObject obj, params string[] names)
	{
		JToken token = GetJsonPropertyIgnoreCase(obj, names);
		return token?.ToString() ?? "";
	}

internal static string TrimToMaxChars(string s, int maxChars)
	{
		if (string.IsNullOrWhiteSpace(s))
		{
			return "";
		}
		s = s.Trim();
		if (s.Length <= maxChars)
		{
			return s;
		}
		return s.Substring(0, maxChars).Trim();
	}
}
