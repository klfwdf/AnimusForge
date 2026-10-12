using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
	private static readonly string[] TimingTerms = Terms("RecallQuery_TimingTerms");
	private static readonly string[] PolicyPrefixes = Terms("RecallQuery_PolicyPrefixes");
	private static readonly string[] ExclusionPrefixes = Terms("RecallQuery_ExclusionPrefixes");
	private static readonly string[] RepealPrefixes = Terms("RecallQuery_RepealPrefixes");
	private static readonly string[] ProhibitionTerms = Terms("RecallQuery_ProhibitionTerms");
	private static readonly string[] NoNumericIntentValues = Terms("RecallQuery_NoNumericIntentValues");
	private static readonly Regex ContrastBoundary = new Regex(string.Join("|", Terms("RecallQuery_ContrastTerms")
		.Select(term => term.All(character => character <= 127) ? @"\b" + Regex.Escape(term) + @"\b" : Regex.Escape(term))),
		RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
	private static readonly Regex SentenceBoundary = new Regex("[" + Regex.Escape(PolicyCompatibilityTexts.Get("RecallQuery_SentenceDelimiters"))
		+ @"]|(?<!\d)\.(?!\d)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static string[] Terms(string key) => PolicyCompatibilityTexts.Get(key).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

	internal static IReadOnlyList<PolicyEffectRecallQuery> Build(string name, string content, string impact, string numeric, IReadOnlyList<IPolicyEffectModule> modules)
	{
		string authoritative = ((name ?? "").Trim() + "\n" + (content ?? "").Trim()).Trim();
		if (authoritative.Length == 0) throw new InvalidOperationException("Policy effect module retrieval text is empty.");
		List<PolicyEffectRecallQuery> result = new List<PolicyEffectRecallQuery>(PolicyEffectModuleRouter.QueryIntentLimit)
		{
			new PolicyEffectRecallQuery { Text = authoritative, Source = PolicyEffectRecallQuerySource.Policy }
		};
		HashSet<string> emitted = new HashSet<string>(StringComparer.Ordinal) { authoritative };
		HashSet<string> clauseSeen = new HashSet<string>(StringComparer.Ordinal);
		List<(PolicyEffectRecallQuery Query, int Ordinal, bool Cue, bool Numeric)> clauses = new List<(PolicyEffectRecallQuery, int, bool, bool)>();
		int ordinal = 0;
		// A comma often separates a target/condition from its consequence; keep that context together.
		string original = ContrastBoundary.Replace(content ?? "", ";");
		foreach (string sentence in SentenceBoundary.Split(original))
		{
			bool excluded = IsExplicitExclusion(sentence.Trim());
			foreach (string window in Windows(sentence))
			{
				string text = window.Trim();
				if (text.Length < 2 || !clauseSeen.Add(text)) continue;
				bool cue = (modules ?? Array.Empty<IPolicyEffectModule>()).Any(m => m?.CueTerms?.Any(term =>
					!string.IsNullOrWhiteSpace(term) && text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) == true);
				bool timed = text.Any(char.IsDigit) || TimingTerms.Any(term => text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
				clauses.Add((new PolicyEffectRecallQuery { Text = text, Source = PolicyEffectRecallQuerySource.OriginalClause, IsExplicitExclusion = excluded }, ordinal++, cue, timed));
			}
		}
		List<string> supplements = new List<string>(2);
		if (!string.IsNullOrWhiteSpace(impact)) supplements.Add(PolicyGenerationPromptTexts.Get("ModuleImpactQuery", "Impact overview: ") + impact.Trim());
		if (!string.IsNullOrWhiteSpace(numeric) && !NoNumericIntentValues.Any(value => string.Equals(numeric.Trim(), value, StringComparison.OrdinalIgnoreCase)))
			supplements.Add(PolicyGenerationPromptTexts.Get("ModuleNumericQuery", "Numeric intent: ") + numeric.Trim());
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
		foreach (string prefix in PolicyPrefixes)
			if (StartsWithTerm(clean, prefix))
			{
				clean = clean.Substring(prefix.Length).Trim();
				if (prefix.All(character => character <= 127)) clean = clean.TrimStart(':', ',').Trim();
				break;
			}
		// These are ranking hints, not execution judgments or candidate rejection.
		// Prohibiting raids and reducing a numeric value remain positive measures.
		return ExclusionPrefixes.Any(prefix => StartsWithTerm(clean, prefix))
			|| (RepealPrefixes.Any(prefix => StartsWithTerm(clean, prefix))
				&& ProhibitionTerms.Any(term => clean.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0));
	}

	private static bool StartsWithTerm(string text, string term)
	{
		if (!text.StartsWith(term, StringComparison.OrdinalIgnoreCase)) return false;
		return !term.All(character => character <= 127) || text.Length == term.Length
			|| !char.IsLetterOrDigit(text[term.Length]);
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
