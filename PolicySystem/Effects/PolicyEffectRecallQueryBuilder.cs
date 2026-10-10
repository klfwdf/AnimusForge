using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.PolicyEffects;

internal enum PolicyEffectRecallQuerySource { Policy, OriginalClause, Assessment }

internal sealed class PolicyEffectRecallQuery
{
	internal string Text { get; set; }
	internal PolicyEffectRecallQuerySource Source { get; set; }
	internal bool IsExplicitExclusion { get; set; }
}

// Runs once per generation; at most twelve embeddings, never on Tick.
internal static class PolicyEffectRecallQueryBuilder
{
	internal static IReadOnlyList<PolicyEffectRecallQuery> Build(string name, string content, string impact, string numeric, IReadOnlyList<IPolicyEffectModule> modules)
	{
		string authoritative = ((name ?? "").Trim() + "\n" + (content ?? "").Trim()).Trim();
		if (authoritative.Length == 0) throw new InvalidOperationException("政策效果模块检索文本为空。");
		List<PolicyEffectRecallQuery> result = new List<PolicyEffectRecallQuery>(PolicyEffectModuleRouter.QueryIntentLimit)
		{
			new PolicyEffectRecallQuery { Text = authoritative, Source = PolicyEffectRecallQuerySource.Policy }
		};
		HashSet<string> emitted = new HashSet<string>(StringComparer.Ordinal) { authoritative };
		HashSet<string> clauseSeen = new HashSet<string>(StringComparer.Ordinal);
		List<(PolicyEffectRecallQuery Query, int Ordinal, bool Cue, bool Numeric)> clauses = new List<(PolicyEffectRecallQuery, int, bool, bool)>();
		int ordinal = 0;
		// A comma often separates a target/condition from its consequence; keep that context together.
		string original = (content ?? "").Replace("而是", "；").Replace("但是", "；");
		foreach (string sentence in original.Split(new[] { '\r', '\n', '。', '；', ';', '！', '!', '？', '?' }, StringSplitOptions.RemoveEmptyEntries))
		{
			bool excluded = IsExplicitExclusion(sentence.Trim());
			foreach (string window in Windows(sentence))
			{
				string text = window.Trim();
				if (text.Length < 2 || !clauseSeen.Add(text)) continue;
				bool cue = (modules ?? Array.Empty<IPolicyEffectModule>()).Any(m => m?.CueTerms?.Any(term =>
					!string.IsNullOrWhiteSpace(term) && text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) == true);
				bool timed = text.Any(char.IsDigit) || new[] { "每日", "每天", "一次性", "单次", "第纳尔", "百分" }.Any(text.Contains);
				clauses.Add((new PolicyEffectRecallQuery { Text = text, Source = PolicyEffectRecallQuerySource.OriginalClause, IsExplicitExclusion = excluded }, ordinal++, cue, timed));
			}
		}
		List<string> supplements = new List<string>(2);
		if (!string.IsNullOrWhiteSpace(impact)) supplements.Add("影响概述：" + impact.Trim());
		if (!string.IsNullOrWhiteSpace(numeric) && numeric.Trim() != "无直接数值意图") supplements.Add("数值意图：" + numeric.Trim());
		int originalBudget = PolicyEffectModuleRouter.QueryIntentLimit - supplements.Count;
		foreach (var clause in clauses.OrderBy(c => c.Query.IsExplicitExclusion).ThenByDescending(c => c.Cue).ThenByDescending(c => c.Numeric).ThenBy(c => c.Ordinal))
		{
			if (result.Count >= originalBudget) break;
			if (emitted.Add(clause.Query.Text)) result.Add(clause.Query);
		}
		foreach (string supplement in supplements)
		{
			string text = supplement.Substring(0, Math.Min(supplement.Length, PolicyEffectModuleRouter.QueryIntentCharacterLimit));
			if (emitted.Add(text)) result.Add(new PolicyEffectRecallQuery { Text = text, Source = PolicyEffectRecallQuerySource.Assessment });
		}
		return result;
	}

	private static bool IsExplicitExclusion(string text)
	{
		string clean = text;
		foreach (string prefix in new[] { "本政策", "此政策", "本方案", "本措施" })
			if (clean.StartsWith(prefix, StringComparison.Ordinal)) { clean = clean.Substring(prefix.Length).Trim(); break; }
		// These are ranking hints, not execution judgments or candidate rejection.
		// In particular 禁止/不得劫掠 and decreases are valid positive measures.
		return new[] { "不改变", "不调整", "不执行", "不实施", "不影响", "不会改变", "无需改变", "不增加", "不减少", "不提高", "不降低" }
			.Any(prefix => clean.StartsWith(prefix, StringComparison.Ordinal))
			|| (clean.StartsWith("废除", StringComparison.Ordinal) && clean.Contains("禁令"));
	}

	private static IEnumerable<string> Windows(string text)
	{
		string clean = (text ?? "").Trim();
		if (clean.Length > PolicyEffectModuleRouter.QueryIntentCharacterLimit)
		{
			// A tail measure can be diluted by a long unpunctuated preamble even inside a 320-char
			// window. Reserve one shorter suffix before the overlapping context windows.
			int tailStart = clean.Length - PolicyEffectModuleRouter.QueryIntentCharacterLimit / 4;
			if (char.IsLowSurrogate(clean[tailStart])) tailStart--;
			yield return clean.Substring(tailStart);
		}
		int offset = 0;
		while (offset < clean.Length)
		{
			int length = Math.Min(PolicyEffectModuleRouter.QueryIntentCharacterLimit, clean.Length - offset);
			if (offset + length < clean.Length && char.IsHighSurrogate(clean[offset + length - 1])) length--;
			yield return clean.Substring(offset, length);
			if (offset + length == clean.Length) yield break;
			offset += length - 64;
			if (char.IsLowSurrogate(clean[offset])) offset--;
		}
	}
}
