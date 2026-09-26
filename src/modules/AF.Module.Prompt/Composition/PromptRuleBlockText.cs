using System;
using System.Text;

namespace AnimusForge;

/// <summary>
/// Owner of the injected rule block text format ("【附加规则:id】" + body). All three channels,
/// the postprocess stage and team bridges detect or edit these blocks; this class is the single
/// place that knows the delimiter. Pure string functions, no allocation beyond the result.
/// </summary>
internal static class PromptRuleBlockText
{
	internal const string HeaderPrefix = "【附加规则:";
	internal const string HeaderSuffix = "】";
	internal const string Disclaimer = "【说明】你不必提到附加规则内的内容，除非有人问起。";

	internal static string Header(string ruleId) => HeaderPrefix + (ruleId ?? "").Trim() + HeaderSuffix;

	/// <summary>Append one block; blank id or body appends nothing.</summary>
	internal static void Append(StringBuilder sb, string ruleId, string body)
	{
		if (sb == null)
		{
			return;
		}
		string id = (ruleId ?? "").Trim();
		string value = (body ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(value))
		{
			sb.AppendLine(HeaderPrefix + id + HeaderSuffix);
			sb.AppendLine(value);
		}
	}

	internal static bool Has(string text, string ruleId)
	{
		string t = (text ?? "").Trim();
		string id = (ruleId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(t) || string.IsNullOrWhiteSpace(id))
		{
			return false;
		}
		return t.IndexOf(Header(id), StringComparison.OrdinalIgnoreCase) >= 0;
	}

	/// <summary>Number of block headers; legacy scan advanced by 6 characters per hit.</summary>
	internal static int Count(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		int count = 0;
		int index = 0;
		while (index >= 0 && index < text.Length)
		{
			index = text.IndexOf(HeaderPrefix, index, StringComparison.Ordinal);
			if (index < 0)
			{
				break;
			}
			count++;
			index += 6;
		}
		return count;
	}

	/// <summary>Replace the body of one block (case-insensitive header match); unknown block returns the trimmed text.</summary>
	internal static string ReplaceBody(string text, string ruleId, string newBody)
	{
		string t = (text ?? "").Trim();
		string id = (ruleId ?? "").Trim();
		string body = (newBody ?? "").Trim();
		if (string.IsNullOrWhiteSpace(t) || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(body))
		{
			return t;
		}
		string header = Header(id);
		int start = t.IndexOf(header, StringComparison.OrdinalIgnoreCase);
		if (start < 0)
		{
			return t;
		}
		int next = t.IndexOf(HeaderPrefix, start + header.Length, StringComparison.Ordinal);
		string before = t.Substring(0, start).TrimEnd();
		string after = next >= 0 ? t.Substring(next).TrimStart() : "";
		StringBuilder sb = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(before))
		{
			sb.AppendLine(before);
		}
		sb.AppendLine(header);
		sb.Append(body.Trim());
		if (!string.IsNullOrWhiteSpace(after))
		{
			sb.AppendLine();
			sb.Append(after);
		}
		return sb.ToString().Trim();
	}

	internal static string Remove(string text, string ruleId)
	{
		string t = (text ?? "").Trim();
		string id = (ruleId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(t) || string.IsNullOrWhiteSpace(id))
		{
			return t;
		}
		string header = Header(id);
		int start = t.IndexOf(header, StringComparison.OrdinalIgnoreCase);
		if (start < 0)
		{
			return t;
		}
		int next = t.IndexOf(HeaderPrefix, start + header.Length, StringComparison.Ordinal);
		string before = t.Substring(0, start).TrimEnd();
		string after = next >= 0 ? t.Substring(next).TrimStart() : "";
		if (string.IsNullOrWhiteSpace(before))
		{
			return after.Trim();
		}
		if (string.IsNullOrWhiteSpace(after))
		{
			return before.Trim();
		}
		return (before + Environment.NewLine + after).Trim();
	}

	/// <summary>Append a block only when missing and under the block cap; matches the legacy lords_hall/encounter pattern.</summary>
	internal static string AppendIfMissing(string text, string ruleId, string body, int cap)
	{
		string t = text ?? "";
		string value = (body ?? "").Trim();
		if (string.IsNullOrWhiteSpace(value) || Has(t, ruleId) || Count(t) >= cap)
		{
			return t;
		}
		string block = Header(ruleId) + Environment.NewLine + value;
		return string.IsNullOrWhiteSpace(t) ? block : (t.TrimEnd() + Environment.NewLine + block);
	}

	internal static string PrependDisclaimer(string text)
	{
		if (string.IsNullOrWhiteSpace(text) || Count(text) <= 0)
		{
			return text;
		}
		if (text.IndexOf(Disclaimer, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return text;
		}
		return Disclaimer + Environment.NewLine + text.TrimStart();
	}
}
