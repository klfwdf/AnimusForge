using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Domain;
namespace AnimusForge;

internal readonly struct WorldDiplomacyMemorySnapshot
{
    internal WorldDiplomacyMemorySnapshot(WorldDiplomacyStorage storage, string kingdomId, HashSet<string> knownIds)
    { Storage = storage; KingdomId = kingdomId; KnownIds = knownIds; }
    internal WorldDiplomacyStorage Storage { get; }
    internal string KingdomId { get; }
    internal HashSet<string> KnownIds { get; }
}
internal interface IWorldDiplomacyMemorySource
{
    bool TryCapture(string heroId, string kingdomOverride, out WorldDiplomacyMemorySnapshot snapshot);
    string FormatDate(int day);
}
internal static class WorldDiplomacyMemoryApplication
{
    internal static string Build(IWorldDiplomacyMemorySource source, string heroId, string kingdomOverride,
        string input, IReadOnlyList<string> ruleIds, bool proactive)
    {
        bool discussionHit = ruleIds?.Any(id => string.Equals(id, "world_diplomacy_discussion", StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "diplomacy", StringComparison.OrdinalIgnoreCase)) == true;
        if (!discussionHit && !proactive && string.IsNullOrWhiteSpace(input)) return "";
        if (!source.TryCapture(heroId, kingdomOverride, out var snapshot)) return "";
        if (!discussionHit && !proactive && !ShouldInjectDiplomacyMemoryForInput(snapshot, input)) return "";
        return BuildDiplomacyMemoryContext(snapshot, input, source.FormatDate);
    }
	private static bool ShouldInjectDiplomacyMemoryForInput(WorldDiplomacyMemorySnapshot snapshot, string input)
	{
		string text = (input ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text)) return false;
		if (new[] { "外交", "宣言", "公文", "王庭", "结盟", "同盟", "议和", "停战", "宣战", "贸易", "通商", "条约", "回应", "条件", "最后通牒" }
			.Any(keyword => text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)) return true;
		HashSet<string> knownIds = snapshot.KnownIds;
		return snapshot.Storage.Documents.Any(document => document != null && knownIds.Contains(document.DocumentId ?? "")
			&& ((!string.IsNullOrWhiteSpace(document.Title) && text.IndexOf(document.Title, StringComparison.OrdinalIgnoreCase) >= 0)
				|| (!string.IsNullOrWhiteSpace(document.AuthorKingdomName) && text.IndexOf(document.AuthorKingdomName, StringComparison.OrdinalIgnoreCase) >= 0)
				|| (!string.IsNullOrWhiteSpace(document.TargetKingdomName) && text.IndexOf(document.TargetKingdomName, StringComparison.OrdinalIgnoreCase) >= 0)));
	}
	private static string BuildDiplomacyMemoryContext(WorldDiplomacyMemorySnapshot snapshot, string input, Func<int, string> formatDate)
	{
		if (snapshot.Storage.Documents.Count == 0)
		{
			return "";
		}
		string kingdomId = snapshot.KingdomId;
		HashSet<string> knownIds = snapshot.KnownIds;
		if (knownIds.Count == 0) return "";
		List<WorldDiplomacyDocument> queryMatches = WorldDiplomacyRoundLifecycleRules.ThenOrderDocumentsByRecency(snapshot.Storage.Documents
				.Where(x => x != null && !x.IsCompressed && knownIds.Contains(x.DocumentId ?? "")
					&& WorldDiplomacyDocumentFactRules.DiplomacyDocumentQueryRelevance(x, input) > 0)
				.OrderByDescending(x => WorldDiplomacyDocumentFactRules.DiplomacyDocumentQueryRelevance(x, input)))
			.Take(2)
			.ToList();
		HashSet<string> selectedIds = new HashSet<string>(queryMatches.Select(x => x.DocumentId), StringComparer.OrdinalIgnoreCase);
		List<WorldDiplomacyDocument> direct = WorldDiplomacyRoundLifecycleRules.ThenOrderDocumentsByRecency(snapshot.Storage.Documents
				.Where(x => x != null && !x.IsCompressed && knownIds.Contains(x.DocumentId ?? "")
					&& !selectedIds.Contains(x.DocumentId ?? "")
					&& (!string.IsNullOrWhiteSpace(kingdomId) && (string.Equals(x.AuthorKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)
						|| string.Equals(x.TargetKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)
						|| (x.AddressedKingdomIds ?? new List<string>()).Contains(kingdomId, StringComparer.OrdinalIgnoreCase))))
				.OrderByDescending(x => WorldDiplomacyDocumentFactRules.DiplomacyDocumentQueryRelevance(x, input)))
			.Take(3)
			.ToList();
		foreach (WorldDiplomacyDocument document in direct) selectedIds.Add(document.DocumentId ?? "");
		List<WorldDiplomacyDocument> headlines = WorldDiplomacyRoundLifecycleRules.OrderDocumentsByRecency(snapshot.Storage.Documents
				.Where(x => x != null && !x.IsCompressed && knownIds.Contains(x.DocumentId ?? "") && !selectedIds.Contains(x.DocumentId ?? "") && WorldDiplomacyDocumentFactRules.IsMajorDiplomaticDocument(x)))
			.Take(2)
			.ToList();
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【当前人物已获知的王国公告】");
		sb.AppendLine("以下仅是公文传播到此人所在地点后，或传到其所属王庭后由贵族通信网获得的事实；不代表全世界同步知晓，也不是当前对话的新承诺。");
		foreach (WorldDiplomacyDocument document in queryMatches)
		{
			sb.AppendLine("- [当前问题命中] " + WorldDiplomacyTextRules.BuildDetailedDocumentMemoryLine(document, formatDate));
		}
		foreach (WorldDiplomacyDocument document in direct)
		{
			sb.AppendLine("- [直接相关] " + WorldDiplomacyTextRules.BuildDetailedDocumentMemoryLine(document, formatDate));
		}
		foreach (WorldDiplomacyDocument document in headlines)
		{
			sb.AppendLine("- [世界要闻] " + WorldDiplomacyTextRules.BuildCompactDocumentMemoryLine(document, formatDate));
		}
		foreach (WorldDiplomacyRoundSummary summary in snapshot.Storage.RoundSummaries
			.Where(x => x != null && (x.SourceDocumentIds ?? new List<string>()).Any(knownIds.Contains))
			.OrderByDescending(x => x.CreatedDay).Take(1))
		{
			List<string> visibleFacts = (summary.Facts ?? new List<WorldDiplomacyRoundFact>()).Where(x => x != null && (x.SourceDocumentIds ?? new List<string>()).Any(knownIds.Contains)).Select(WorldDiplomacyDocumentFactRules.FormatRoundFactForPrompt).Where(x => !string.IsNullOrWhiteSpace(x)).Take(6).ToList();
			sb.AppendLine("- [往期外交事件] " + WorldDiplomacyTextRules.Limit(visibleFacts.Count > 0 ? string.Join("、", visibleFacts) : summary.Summary, 650));
		}
		return sb.ToString().TrimEnd();
	}
}
