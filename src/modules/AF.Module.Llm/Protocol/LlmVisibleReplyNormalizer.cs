using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

/// <summary>
/// Unwraps accidental JSON envelopes around player-visible LLM replies without
/// changing structured LLM outputs used by non-chat systems.
/// </summary>
public static class LlmVisibleReplyNormalizer
{
	private static readonly string[] ReplyPropertyNames = new string[8]
	{
		"response",
		"reply",
		"answer",
		"content",
		"text",
		"message",
		"output",
		"output_text"
	};

	private const int MaxEnvelopeDepth = 3;

	public static string NormalizeComplete(string text)
	{
		string original = text ?? "";
		if (string.IsNullOrWhiteSpace(original))
		{
			return original;
		}
		string candidate = StripJsonCodeFence(original);
		if (!LooksLikePotentialEnvelope(candidate))
		{
			return original;
		}
		return TryNormalizeEnvelope(candidate, 0, out var normalized) ? normalized.Trim() : original;
	}

	public static string NormalizeStreamingPreview(string text)
	{
		string original = text ?? "";
		if (string.IsNullOrEmpty(original))
		{
			return original;
		}
		string candidate = StripJsonCodeFence(original);
		if (!LooksLikePotentialEnvelope(candidate))
		{
			return original;
		}
		if (TryExtractLooseNamedReply(candidate, allowIncompleteValue: true, out var partial, out var foundProperty))
		{
			return NormalizeExtractedReply(partial).TrimStart();
		}
		return foundProperty || CouldStillBecomeReplyEnvelope(candidate) ? "" : original;
	}

	public sealed class StreamFilter
	{
		private readonly StringBuilder _candidate = new StringBuilder();

		private string _lastPreview = "";

		private bool _passThrough;

		private int _emittedLength;

		public string NormalizedText { get; private set; } = "";

		public string Push(string chunk)
		{
			if (string.IsNullOrEmpty(chunk))
			{
				return "";
			}
			if (_passThrough)
			{
				_emittedLength += chunk.Length;
				return chunk;
			}
			_candidate.Append(chunk);
			string buffered = _candidate.ToString();
			if (!CouldStillBecomeReplyEnvelope(buffered))
			{
				_passThrough = true;
				_emittedLength = buffered.Length;
				_candidate.Clear();
				return buffered;
			}
			string preview = NormalizeStreamingPreview(buffered);
			return EmitNewSuffix(preview);
		}

		public string Complete(string finalText)
		{
			string source = finalText;
			if (source == null)
			{
				source = _candidate.ToString();
			}
			string normalized = NormalizeComplete(source);
			if (string.Equals(normalized, source, StringComparison.Ordinal) && HasReplyPropertyMarker(StripJsonCodeFence(source)))
			{
				normalized = NormalizeStreamingPreview(source);
			}
			NormalizedText = normalized ?? "";
			if (_passThrough)
			{
				if (NormalizedText.Length <= _emittedLength)
				{
					return "";
				}
				return NormalizedText.Substring(_emittedLength);
			}
			return EmitNewSuffix(NormalizedText);
		}

		private string EmitNewSuffix(string preview)
		{
			preview ??= "";
			if (preview.Length == 0)
			{
				return "";
			}
			if (_lastPreview.Length == 0)
			{
				_lastPreview = preview;
				_emittedLength = preview.Length;
				return preview;
			}
			if (!preview.StartsWith(_lastPreview, StringComparison.Ordinal))
			{
				return "";
			}
			string suffix = preview.Substring(_lastPreview.Length);
			_lastPreview = preview;
			_emittedLength = preview.Length;
			return suffix;
		}
	}

	private static bool TryNormalizeEnvelope(string candidate, int depth, out string normalized)
	{
		normalized = "";
		if (depth >= MaxEnvelopeDepth || string.IsNullOrWhiteSpace(candidate) || !LooksLikePotentialEnvelope(candidate))
		{
			return false;
		}
		if (TryParseNamedReply(candidate, out var extracted) || TryExtractLooseNamedReply(candidate, allowIncompleteValue: false, out extracted, out var _))
		{
			extracted = NormalizeExtractedReply(extracted);
			string nestedCandidate = StripJsonCodeFence(extracted);
			if (depth + 1 < MaxEnvelopeDepth && TryNormalizeEnvelope(nestedCandidate, depth + 1, out var nested))
			{
				normalized = nested;
			}
			else
			{
				normalized = extracted;
			}
			return true;
		}
		return false;
	}

	private static bool TryParseNamedReply(string candidate, out string reply)
	{
		reply = "";
		try
		{
			JToken token = JToken.Parse(candidate);
			return TryExtractReplyToken(token, requireNamedProperty: true, out reply);
		}
		catch
		{
			return false;
		}
	}

	private static bool TryExtractReplyToken(JToken token, bool requireNamedProperty, out string reply)
	{
		reply = "";
		if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
		{
			return false;
		}
		if (token is JObject obj)
		{
			foreach (string propertyName in ReplyPropertyNames)
			{
				JProperty property = FindPropertyIgnoreCase(obj, propertyName);
				if (property != null && TryExtractReplyToken(property.Value, requireNamedProperty: false, out reply))
				{
					return true;
				}
			}
			return false;
		}
		if (token is JArray array)
		{
			List<string> parts = new List<string>();
			foreach (JToken item in array)
			{
				if (TryExtractReplyToken(item, requireNamedProperty, out var part))
				{
					parts.Add(part ?? "");
				}
			}
			if (parts.Count == 0)
			{
				return false;
			}
			reply = string.Join("\n", parts);
			return true;
		}
		if (!requireNamedProperty && token is JValue value)
		{
			reply = value.Value?.ToString() ?? "";
			return true;
		}
		if (requireNamedProperty && token.Type == JTokenType.String)
		{
			string nested = token.ToString();
			return TryNormalizeEnvelope(StripJsonCodeFence(nested), 1, out reply);
		}
		return false;
	}

	private static JProperty FindPropertyIgnoreCase(JObject obj, string propertyName)
	{
		foreach (JProperty property in obj.Properties())
		{
			if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
			{
				return property;
			}
		}
		return null;
	}

	private static bool TryExtractLooseNamedReply(string candidate, bool allowIncompleteValue, out string reply, out bool foundProperty)
	{
		reply = "";
		foundProperty = false;
		if (!TryFindReplyProperty(candidate, out var propertyEnd))
		{
			return false;
		}
		foundProperty = true;
		int index = propertyEnd;
		SkipWhitespace(candidate, ref index);
		if (index >= candidate.Length || candidate[index] != ':')
		{
			return false;
		}
		index++;
		SkipWhitespace(candidate, ref index);
		if (index >= candidate.Length)
		{
			return false;
		}
		char quote = candidate[index];
		if (quote == '"' || quote == '\'')
		{
			return TryReadLooseQuotedValue(candidate, index + 1, quote, allowIncompleteValue, out reply);
		}
		int end = index;
		while (end < candidate.Length && candidate[end] != '\r' && candidate[end] != '\n' && candidate[end] != '}' && candidate[end] != ']')
		{
			end++;
		}
		reply = candidate.Substring(index, end - index).Trim().TrimEnd(',');
		return reply.Length > 0;
	}

	private static bool TryFindReplyProperty(string text, out int propertyEnd)
	{
		propertyEnd = -1;
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		foreach (string name in ReplyPropertyNames)
		{
			string doubleQuoted = "\"" + name + "\"";
			string singleQuoted = "'" + name + "'";
			int doubleQuotedIndex = text.IndexOf(doubleQuoted, StringComparison.OrdinalIgnoreCase);
			int singleQuotedIndex = text.IndexOf(singleQuoted, StringComparison.OrdinalIgnoreCase);
			if (doubleQuotedIndex < 0 && singleQuotedIndex < 0)
			{
				continue;
			}
			bool useDoubleQuoted = doubleQuotedIndex >= 0 && (singleQuotedIndex < 0 || doubleQuotedIndex <= singleQuotedIndex);
			propertyEnd = useDoubleQuoted ? (doubleQuotedIndex + doubleQuoted.Length) : (singleQuotedIndex + singleQuoted.Length);
			return true;
		}
		return false;
	}

	private static bool TryReadLooseQuotedValue(string text, int start, char quote, bool allowIncompleteValue, out string value)
	{
		StringBuilder builder = new StringBuilder(Math.Max(0, text.Length - start));
		for (int i = start; i < text.Length; i++)
		{
			char c = text[i];
			if (c == quote)
			{
				value = builder.ToString();
				return true;
			}
			if (c != '\\')
			{
				builder.Append(c);
				continue;
			}
			if (i + 1 >= text.Length)
			{
				break;
			}
			char escaped = text[++i];
			switch (escaped)
			{
			case 'n':
			case 'N':
				builder.Append('\n');
				break;
			case 'r':
			case 'R':
				builder.Append('\n');
				if (i + 2 < text.Length && text[i + 1] == '\\' && (text[i + 2] == 'n' || text[i + 2] == 'N'))
				{
					i += 2;
				}
				break;
			case 't':
				builder.Append('\t');
				break;
			case 'b':
				builder.Append('\b');
				break;
			case 'f':
				builder.Append('\f');
				break;
			case 'u':
				if (i + 4 < text.Length && TryParseHexChar(text, i + 1, out var unicode))
				{
					builder.Append(unicode);
					i += 4;
				}
				else if (!allowIncompleteValue)
				{
					builder.Append('\\').Append(escaped);
				}
				break;
			case '"':
			case '\'':
			case '\\':
			case '/':
				builder.Append(escaped);
				break;
			default:
				builder.Append('\\').Append(escaped);
				break;
			}
		}
		value = builder.ToString();
		return allowIncompleteValue && (value.Length > 0 || start <= text.Length);
	}

	private static bool TryParseHexChar(string text, int start, out char value)
	{
		value = '\0';
		if (start < 0 || start + 4 > text.Length)
		{
			return false;
		}
		int code = 0;
		for (int i = start; i < start + 4; i++)
		{
			char c = text[i];
			int digit;
			if (c >= '0' && c <= '9')
			{
				digit = c - '0';
			}
			else if (c >= 'a' && c <= 'f')
			{
				digit = c - 'a' + 10;
			}
			else if (c >= 'A' && c <= 'F')
			{
				digit = c - 'A' + 10;
			}
			else
			{
				return false;
			}
			code = (code << 4) | digit;
		}
		value = (char)code;
		return true;
	}

	private static string NormalizeExtractedReply(string text)
	{
		return (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
	}

	private static string StripJsonCodeFence(string text)
	{
		string value = (text ?? "").Trim();
		if (!value.StartsWith("```", StringComparison.Ordinal))
		{
			return value;
		}
		int lineEnd = value.IndexOf('\n');
		if (lineEnd < 0)
		{
			return value;
		}
		value = value.Substring(lineEnd + 1).Trim();
		if (value.EndsWith("```", StringComparison.Ordinal))
		{
			value = value.Substring(0, value.Length - 3).TrimEnd();
		}
		return value;
	}

	private static bool LooksLikePotentialEnvelope(string text)
	{
		string value = (text ?? "").TrimStart();
		if (value.Length == 0)
		{
			return false;
		}
		char first = value[0];
		if (first == '{' || first == '"' || first == '\'')
		{
			return true;
		}
		if (first != '[')
		{
			return false;
		}
		int index = 1;
		SkipWhitespace(value, ref index);
		return index >= value.Length || value[index] == '{' || value[index] == '[' || value[index] == '"' || value[index] == '\'';
	}

	private static bool CouldStillBecomeReplyEnvelope(string text)
	{
		string value = (text ?? "").TrimStart();
		if (value.Length == 0 || "```".StartsWith(value, StringComparison.Ordinal))
		{
			return true;
		}
		if (value.StartsWith("```", StringComparison.Ordinal))
		{
			string withoutFence = StripJsonCodeFence(value);
			return string.Equals(withoutFence, value, StringComparison.Ordinal) || CouldStillBecomeReplyEnvelope(withoutFence);
		}
		if (!LooksLikePotentialEnvelope(value))
		{
			return false;
		}
		return true;
	}

	private static bool HasReplyPropertyMarker(string text)
	{
		return TryFindReplyProperty(text, out var _);
	}

	private static void SkipWhitespace(string text, ref int index)
	{
		while (index < (text?.Length ?? 0) && char.IsWhiteSpace(text[index]))
		{
			index++;
		}
	}
}
