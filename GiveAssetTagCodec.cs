using System;
using System.Collections.Generic;
using System.Text;

namespace AnimusForge;

/// <summary>
/// Parses the variable-length asset part of GIVE_ASSET tags without treating ']' or ':' as forbidden
/// item-name characters. The final ':&lt;quantity&gt;]' is the tag terminator by grammar.
/// </summary>
internal readonly struct GiveAssetTag
{
	internal int Index { get; }

	internal int Length { get; }

	internal string RawTag { get; }

	internal string AssetToken { get; }

	internal string QuantityToken { get; }

	internal GiveAssetTag(int index, int length, string rawTag, string assetToken, string quantityToken)
	{
		Index = index;
		Length = length;
		RawTag = rawTag ?? "";
		AssetToken = assetToken ?? "";
		QuantityToken = quantityToken ?? "";
	}
}

/// <summary>
/// Shared syntax layer for [ACTION:GIVE_ASSET:&lt;asset&gt;:&lt;quantity&gt;].
/// It is used only for postprocess-sized text, never from a frame/tick hot path.
/// </summary>
internal static class GiveAssetTagCodec
{
	internal const string Prefix = "[ACTION:GIVE_ASSET:";

	internal static bool TryParseWhole(string value, out GiveAssetTag tag)
	{
		tag = default;
		string text = (value ?? "").Trim();
		return TryParseAt(text, 0, out tag) && tag.Index == 0 && tag.Length == text.Length;
	}

	internal static List<GiveAssetTag> Extract(string text)
	{
		List<GiveAssetTag> result = new List<GiveAssetTag>();
		string source = text ?? "";
		int searchStart = 0;
		while (searchStart < source.Length)
		{
			int tagStart = source.IndexOf(Prefix, searchStart, StringComparison.OrdinalIgnoreCase);
			if (tagStart < 0)
			{
				break;
			}
			if (TryParseAt(source, tagStart, out GiveAssetTag tag))
			{
				result.Add(tag);
				searchStart = tagStart + tag.Length;
			}
			else
			{
				// Advance past this introducer so a malformed tag cannot consume a following valid tag.
				searchStart = tagStart + Prefix.Length;
			}
		}
		return result;
	}

	internal static bool Contains(string text)
	{
		string source = text ?? "";
		int searchStart = 0;
		while (searchStart < source.Length)
		{
			int tagStart = source.IndexOf(Prefix, searchStart, StringComparison.OrdinalIgnoreCase);
			if (tagStart < 0)
			{
				return false;
			}
			if (TryParseAt(source, tagStart, out _))
			{
				return true;
			}
			searchStart = tagStart + Prefix.Length;
		}
		return false;
	}

	internal static string StripTags(string text)
	{
		return ReplaceTags(text, delegate(GiveAssetTag _) { return string.Empty; });
	}

	internal static string ReplaceTags(string text, Func<GiveAssetTag, string> replacement)
	{
		string source = text ?? "";
		if (replacement == null)
		{
			return source;
		}
		List<GiveAssetTag> tags = Extract(source);
		if (tags.Count == 0)
		{
			return source;
		}
		StringBuilder builder = new StringBuilder(source.Length);
		int copiedUntil = 0;
		foreach (GiveAssetTag tag in tags)
		{
			if (tag.Index > copiedUntil)
			{
				builder.Append(source, copiedUntil, tag.Index - copiedUntil);
			}
			builder.Append(replacement(tag) ?? "");
			copiedUntil = tag.Index + tag.Length;
		}
		if (copiedUntil < source.Length)
		{
			builder.Append(source, copiedUntil, source.Length - copiedUntil);
		}
		return builder.ToString();
	}

	private static bool TryParseAt(string source, int tagStart, out GiveAssetTag tag)
	{
		tag = default;
		if (string.IsNullOrEmpty(source) || tagStart < 0 || tagStart + Prefix.Length >= source.Length || !StartsWithPrefixAt(source, tagStart))
		{
			return false;
		}
		int assetStart = tagStart + Prefix.Length;
		int lastColon = -1;
		for (int index = assetStart; index < source.Length; index++)
		{
			char current = source[index];
			if (current == '\r' || current == '\n')
			{
				return false;
			}
			if (current == '[' && index > assetStart && StartsWithPrefixAt(source, index))
			{
				// A second complete GIVE_ASSET introducer begins a new candidate. Keeping the
				// first malformed candidate isolated prevents cross-tag accidental grants.
				return false;
			}
			if (current == ':')
			{
				lastColon = index;
				continue;
			}
			if (current != ']' || lastColon <= assetStart)
			{
				continue;
			}
			int quantityStart = lastColon + 1;
			int quantityLength = index - quantityStart;
			if (!IsQuantityToken(source, quantityStart, quantityLength))
			{
				continue;
			}
			int assetLength = lastColon - assetStart;
			if (assetLength <= 0)
			{
				return false;
			}
			int tagLength = index - tagStart + 1;
			tag = new GiveAssetTag(
				tagStart,
				tagLength,
				source.Substring(tagStart, tagLength),
				source.Substring(assetStart, assetLength),
				source.Substring(quantityStart, quantityLength));
			return true;
		}
		return false;
	}

	private static bool StartsWithPrefixAt(string source, int index)
	{
		return index >= 0
			&& index + Prefix.Length <= source.Length
			&& string.Compare(source, index, Prefix, 0, Prefix.Length, StringComparison.OrdinalIgnoreCase) == 0;
	}

	private static bool IsQuantityToken(string source, int start, int length)
	{
		if (length <= 0)
		{
			return false;
		}
		if (length == 3 && string.Compare(source, start, "ALL", 0, 3, StringComparison.OrdinalIgnoreCase) == 0)
		{
			return true;
		}
		for (int index = start; index < start + length; index++)
		{
			if (source[index] < '0' || source[index] > '9')
			{
				return false;
			}
		}
		return true;
	}
}
