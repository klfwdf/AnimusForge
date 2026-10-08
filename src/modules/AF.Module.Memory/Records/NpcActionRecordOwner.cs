using EventSourceMaterialEntry = AnimusForge.MyBehavior.EventSourceMaterialEntry;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace AnimusForge;

// One action-time path; owns recent-key index, not a duplicate of the saved action stores.
internal sealed class NpcActionRecordOwner
{
internal void RebuildNpcRecentActionStableKeyIndex(Dictionary<string,List<NpcActionEntry>> recentActions)
	{
		RecentStableKeys.Clear();
		foreach (KeyValuePair<string, List<NpcActionEntry>> pair in recentActions ?? new Dictionary<string, List<NpcActionEntry>>())
		{
			RefreshNpcRecentActionStableKeyIndexForHero(pair.Key, pair.Value);
		}
	}

    internal readonly Dictionary<string, HashSet<string>> RecentStableKeys = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
    internal void Record(Dictionary<string, List<NpcActionEntry>> storage, string heroKey, string text,
        string stableKey, int currentDay, bool keepOnlyRecentWindow, bool dedupeAcrossWindow,
        int maxEntries, Func<int> nextSequence, Func<string, string, int, int, int, NpcActionEntry> capture,
        Action markAll, Action<int> markDay)
    {
			string npcActionHeroKey = heroKey;
			string text2 = NpcActionLedger.NormalizeText(text);
			if (string.IsNullOrWhiteSpace(npcActionHeroKey) || string.IsNullOrWhiteSpace(text2))
			{
				return;
			}
			string text3 = NpcActionLedger.NormalizeStableKey(stableKey, text2);
			int currentGameDayIndexSafe = currentDay;
			if (!storage.TryGetValue(npcActionHeroKey, out var value) || value == null)
			{
				value = new List<NpcActionEntry>();
				storage[npcActionHeroKey] = value;
			}
			bool entriesChanged = NpcActionLedger.RemoveInvalid(value, keepOnlyRecentWindow ? NpcActionLedger.RecentWindowMinimumDay(currentGameDayIndexSafe) : int.MinValue, keepOnlyRecentWindow, e => e.Text, e => e.Day);
			if (entriesChanged)
			{
				markAll();
			}
			if (keepOnlyRecentWindow && entriesChanged)
			{
				RefreshNpcRecentActionStableKeyIndexForHero(npcActionHeroKey, value);
			}
			if (dedupeAcrossWindow)
			{
				if ((keepOnlyRecentWindow && IsNpcRecentActionStableKeyKnown(npcActionHeroKey, text3)) || NpcActionLedger.ContainsStableKey(value, text3, e => e.StableKey))
				{
					return;
				}
			}
			else if (NpcActionLedger.ContainsForDay(value, currentGameDayIndexSafe, text3, text2, e => e.Day, e => e.StableKey, e => e.Text))
			{
				return;
			}
			int order = NpcActionLedger.NextOrder(value, currentGameDayIndexSafe, e => e.Day, e => e.Order);
			int sequence = nextSequence();
			NpcActionEntry npcActionEntry = capture(text2, text3, currentGameDayIndexSafe, order, sequence);
			if (maxEntries > 0 && value.Count >= maxEntries)
			{
				markAll();
			}
			NpcActionLedger.Append(value, npcActionEntry, maxEntries, CompareTimeline);
			markDay(currentGameDayIndexSafe);
			if (keepOnlyRecentWindow)
			{
				RefreshNpcRecentActionStableKeyIndexForHero(npcActionHeroKey, value);
			}
    }

	internal void RefreshNpcRecentActionStableKeyIndexForHero(string heroKey, List<NpcActionEntry> entries)
	{
		string text = (heroKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (!RecentStableKeys.TryGetValue(text, out HashSet<string> stableKeys) || stableKeys == null)
		{
			stableKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
		else
		{
			stableKeys.Clear();
		}
		if (entries != null)
		{
			for (int index = 0; index < entries.Count; index++)
			{
				string stableKey = (entries[index]?.StableKey ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(stableKey))
				{
					stableKeys.Add(stableKey);
				}
			}
		}
		if (stableKeys.Count > 0)
		{
			RecentStableKeys[text] = stableKeys;
		}
		else
		{
			RecentStableKeys.Remove(text);
		}
	}

	internal bool IsNpcRecentActionStableKeyKnown(string heroKey, string stableKey)
	{
		string text = (heroKey ?? "").Trim();
		string text2 = (stableKey ?? "").Trim();
		return !string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2) && RecentStableKeys.TryGetValue(text, out var value) && value != null && value.Contains(text2);
	}
    internal static int CompareTimeline(NpcActionEntry left, NpcActionEntry right)
    {
        if (ReferenceEquals(left, right)) return 0;
        if (left == null) return -1;
        if (right == null) return 1;
        return NpcActionLedger.CompareTimeline(left.Day, left.Sequence, left.Order, left.GameDate,
            right.Day, right.Sequence, right.Order, right.GameDate);
    }

    internal static NpcActionEntry Create(NpcActionEntry capture, string text, string stableKey,
        int day, int order, int sequence, bool isMajor)
    {
        var result = new NpcActionEntry
		{
			Day = Math.Max(0, day),
			Order = Math.Max(1, order),
			Sequence = Math.Max(0, sequence),
			GameDate = capture.GameDate,
			Text = (text ?? "").Trim(),
			StableKey = NpcActionLedger.NormalizeStableKey(stableKey, text),
			ActionKind = (capture.ActionKind ?? "").Trim(),
			ActorHeroId = (capture.ActorHeroId ?? "").Trim(),
			ActorClanId = (capture.ActorClanId ?? "").Trim(),
			ActorKingdomId = (capture.ActorKingdomId ?? "").Trim(),
			TargetHeroId = (capture.TargetHeroId ?? "").Trim(),
			TargetClanId = (capture.TargetClanId ?? "").Trim(),
			TargetKingdomId = (capture.TargetKingdomId ?? "").Trim(),
			SettlementId = (capture.SettlementId ?? "").Trim(),
			SettlementName = (capture.SettlementName ?? "").Trim(),
			SettlementOwnerHeroId = (capture.SettlementOwnerHeroId ?? "").Trim(),
			SettlementOwnerClanId = (capture.SettlementOwnerClanId ?? "").Trim(),
			SettlementOwnerKingdomId = (capture.SettlementOwnerKingdomId ?? "").Trim(),
			PreviousSettlementOwnerHeroId = (capture.PreviousSettlementOwnerHeroId ?? "").Trim(),
			PreviousSettlementOwnerClanId = (capture.PreviousSettlementOwnerClanId ?? "").Trim(),
			PreviousSettlementOwnerKingdomId = (capture.PreviousSettlementOwnerKingdomId ?? "").Trim(),
			LocationText = (capture.LocationText ?? "").Trim(),
			Won = capture.Won,
			IsMajor = isMajor
		};
        CopyIds(capture.RelatedHeroIds, result.RelatedHeroIds);
        CopyIds(capture.RelatedClanIds, result.RelatedClanIds);
        CopyIds(capture.RelatedKingdomIds, result.RelatedKingdomIds);
        return result;
    }

    private static void CopyIds(List<string> source, List<string> destination)
    {
        if (source == null) return;
        foreach (var raw in source)
        {
            string id = (raw ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(id) && !destination.Exists(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase))) destination.Add(id);
        }
    }

    internal static void ResetContainers(ref Dictionary<string,List<NpcActionEntry>> major, ref Dictionary<string,string> majorStorage,
        ref Dictionary<string,List<NpcActionEntry>> recent, ref Dictionary<string,string> recentStorage)
    {
        major = new Dictionary<string,List<NpcActionEntry>>(); majorStorage = new Dictionary<string,string>();
        recent = new Dictionary<string,List<NpcActionEntry>>(); recentStorage = new Dictionary<string,string>();
    }
    internal static void EnsureContainers(ref Dictionary<string,List<NpcActionEntry>> major, ref Dictionary<string,string> majorStorage,
        ref Dictionary<string,List<NpcActionEntry>> recent, ref Dictionary<string,string> recentStorage)
    {
        major ??= new Dictionary<string,List<NpcActionEntry>>(); majorStorage ??= new Dictionary<string,string>();
        recent ??= new Dictionary<string,List<NpcActionEntry>>(); recentStorage ??= new Dictionary<string,string>();
    }


internal string BuildSummary(MemoryBusinessStateOwner state, string npcActionHeroKey, Func<string> actorName, bool recentOnly, Func<int> currentDay, Func<NpcActionEntry,string> metadata, Func<string,string> stripMarker)
	{
		if (string.IsNullOrWhiteSpace(npcActionHeroKey))
		{
			return "";
		}
		Dictionary<string, List<NpcActionEntry>> dictionary = (recentOnly ? state.RecentActions : state.MajorActions);
		if (dictionary == null || !dictionary.TryGetValue(npcActionHeroKey, out var value) || value == null || value.Count <= 0)
		{
			return "";
		}
		List<NpcActionEntry> list = NpcActionLedger.SanitizeNpcActionEntries(value, recentOnly, currentDay());
		if (list.Count <= 0)
		{
			return "";
		}
		if (!recentOnly)
		{
			MajorActionSummaryState summaryState = state.GetMajorActionSummaryState(npcActionHeroKey);
			if (summaryState != null && !string.IsNullOrWhiteSpace(summaryState.Summary))
			{
				string summary = stripMarker(summaryState.Summary.Trim());
				List<NpcActionEntry> pendingActions = list.Where((NpcActionEntry x) => MemoryBusinessStateOwner.IsNpcActionAfterSummaryCursor(x, summaryState)).ToList();
				string pendingRaw = RenderEntries(actorName, pendingActions, metadata, stripMarker);
				if (!string.IsNullOrWhiteSpace(pendingRaw))
				{
					summary += "\n\n【尚未压缩的新重大履历原始记录】\n" + pendingRaw;
				}
				return summary;
			}
			return RenderEntries(actorName, list, metadata, stripMarker);
		}
		return RenderEntries(actorName, list, metadata, stripMarker);
	}

internal static string RenderEntries(Func<string> actorName, List<NpcActionEntry> entries, Func<NpcActionEntry,string> metadata, Func<string,string> stripMarker)
	{
		if (entries == null || entries.Count <= 0)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		int num = int.MinValue;
		string text = null;
		foreach (NpcActionEntry item in entries)
		{
			if (item == null || string.IsNullOrWhiteSpace(item.Text))
			{
				continue;
			}
			string text2 = !string.IsNullOrWhiteSpace(item.GameDate) ? item.GameDate.Trim() : ("第 " + item.Day + " 日");
			if (item.Day != num || !string.Equals(text, text2, StringComparison.Ordinal))
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.AppendLine();
				}
				stringBuilder.AppendLine("—— " + text2 + " ——");
				num = item.Day;
				text = text2;
			}
			stringBuilder.AppendLine("- " + RenderText(actorName(), item.Text, stripMarker) + metadata(item));
		}
		return stringBuilder.ToString().TrimEnd();
	}

internal static string RenderText(string actorName, string rawText, Func<string,string> stripMarker)
	{
		string text = stripMarker((rawText ?? "").Trim());
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string text2 = actorName;
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "该人物";
		}
		text = RewriteNpcActionSecondPersonPronouns(text, text2);
		if (text.StartsWith("你", StringComparison.Ordinal))
		{
			return text2 + text.Substring(1);
		}
		if (text.StartsWith(text2, StringComparison.Ordinal))
		{
			return text;
		}
		return text2 + "：" + text;
	}

internal static string RewriteNpcActionSecondPersonPronouns(string rawText, string actorName)
	{
		string text = (rawText ?? "").Trim();
		string name = string.IsNullOrWhiteSpace(actorName) ? "该人物" : actorName.Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return text
			.Replace("你们的", name + "一方的")
			.Replace("你们", name + "一方")
			.Replace("你的", name + "的")
			.Replace("你", name);
	}

internal static string BuildMetadata(NpcActionEntry entry, Func<NpcActionEntry,string> settlementName, Func<string,string> heroName, Func<string,string> clanName, Func<string,string> kingdomName)
	{
		if (entry == null)
		{
			return "";
		}
		List<string> list = new List<string>();
		string text = WeeklyAggregateEventLineOwner.TranslateNpcActionKindForPrompt(entry.ActionKind);
		if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add("这属于" + text);
		}
		string text2 = settlementName(entry);
		if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add("事情发生在" + text2);
		}
		string text3 = heroName(entry.TargetHeroId);
		if (!string.IsNullOrWhiteSpace(text3))
		{
			list.Add("主要涉及的人物是" + text3);
		}
		string text4 = clanName(entry.TargetClanId);
		if (!string.IsNullOrWhiteSpace(text4))
		{
			list.Add("对方家族是" + text4);
		}
		string text5 = kingdomName(entry.TargetKingdomId);
		if (!string.IsNullOrWhiteSpace(text5))
		{
			list.Add("对方所属王国是" + text5);
		}
		string text6 = clanName(entry.SettlementOwnerClanId);
		string text7 = kingdomName(entry.SettlementOwnerKingdomId);
		if (!string.IsNullOrWhiteSpace(text6) && !string.IsNullOrWhiteSpace(text7))
		{
			list.Add("当时该定居点由" + text6 + "掌控，隶属于" + text7);
		}
		else if (!string.IsNullOrWhiteSpace(text6))
		{
			list.Add("当时该定居点由" + text6 + "掌控");
		}
		else if (!string.IsNullOrWhiteSpace(text7))
		{
			list.Add("当时该定居点隶属于" + text7);
		}
		string text8 = clanName(entry.PreviousSettlementOwnerClanId);
		string text9 = kingdomName(entry.PreviousSettlementOwnerKingdomId);
		if (!string.IsNullOrWhiteSpace(text8) && !string.IsNullOrWhiteSpace(text9))
		{
			list.Add("此前这里由" + text8 + "掌控，归属" + text9);
		}
		else if (!string.IsNullOrWhiteSpace(text8))
		{
			list.Add("此前这里由" + text8 + "掌控");
		}
		else if (!string.IsNullOrWhiteSpace(text9))
		{
			list.Add("此前这里归属" + text9);
		}
		if (entry.Won.HasValue)
		{
			list.Add("结果是" + (entry.Won.Value ? "获胜" : "失利"));
		}
		if (entry.IsMajor)
		{
			list.Add("这是一件重大行动");
		}
		if (list.Count <= 0)
		{
			return "";
		}
		return " " + string.Join("；", list) + "。";
	}

internal static bool DoesNpcActionRelateToKingdom(NpcActionEntry entry, string kingdomId)
	{
		string text = (kingdomId ?? "").Trim();
		if (entry == null || string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (string.Equals((entry.ActorKingdomId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) || string.Equals((entry.TargetKingdomId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) || string.Equals((entry.SettlementOwnerKingdomId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) || string.Equals((entry.PreviousSettlementOwnerKingdomId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return entry.RelatedKingdomIds != null && entry.RelatedKingdomIds.Any((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
	}

internal bool HasRecentNpcActionStableKeyWithinWindow(Dictionary<string,List<NpcActionEntry>> recentActions,string heroKey,string stableKey,int currentDay)
	{
		string normalizedStableKey = NpcActionLedger.NormalizeStableKey(stableKey, "");
		if (string.IsNullOrWhiteSpace(heroKey)
			|| string.IsNullOrWhiteSpace(normalizedStableKey)
			|| recentActions == null
			|| !recentActions.TryGetValue(heroKey, out List<NpcActionEntry> entries)
			|| entries == null)
		{
			return false;
		}
		int minimumDay = currentDay - NpcActionLedger.RecentWindowDays + 1;
		for (int index = entries.Count - 1; index >= 0; index--)
		{
			NpcActionEntry entry = entries[index];
			if (entry != null
				&& entry.Day >= minimumDay
				&& !string.IsNullOrWhiteSpace(entry.Text)
				&& string.Equals(entry.StableKey ?? "", normalizedStableKey, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}


internal static void NormalizeNpcActionSequences(Dictionary<string, List<NpcActionEntry>> storage)
	{
		if (storage == null)
		{
			return;
		}
		int num = 0;
		foreach (KeyValuePair<string, List<NpcActionEntry>> item in storage.OrderBy((KeyValuePair<string, List<NpcActionEntry>> x) => x.Key ?? "", StringComparer.OrdinalIgnoreCase))
		{
			if (item.Value == null)
			{
				continue;
			}
			foreach (NpcActionEntry item2 in item.Value.OrderBy((NpcActionEntry x) => x?.Day ?? int.MinValue).ThenBy((NpcActionEntry x) => x?.Order ?? int.MinValue).ThenBy((NpcActionEntry x) => x?.GameDate ?? "", StringComparer.Ordinal))
			{
				if (item2 != null && item2.Sequence <= 0)
				{
					item2.Sequence = ++num;
				}
			}
		}
	}

internal static int GetMaxNpcActionSequence(params Dictionary<string, List<NpcActionEntry>>[] storages)
	{
		int num = 0;
		foreach (Dictionary<string, List<NpcActionEntry>> dictionary in storages ?? Array.Empty<Dictionary<string, List<NpcActionEntry>>>())
		{
			if (dictionary == null)
			{
				continue;
			}
			foreach (List<NpcActionEntry> value in dictionary.Values)
			{
				if (value == null)
				{
					continue;
				}
				foreach (NpcActionEntry item in value)
				{
					if (item != null && item.Sequence > num)
					{
						num = item.Sequence;
					}
				}
			}
		}
		return num;
	}

internal static int GetMaxNpcActionSequence(Dictionary<string, List<NpcActionEntry>> majorStorage, Dictionary<string, List<NpcActionEntry>> recentStorage, List<EventSourceMaterialEntry> sourceMaterials)
	{
		int num = GetMaxNpcActionSequence(majorStorage, recentStorage);
		foreach (EventSourceMaterialEntry item in sourceMaterials ?? new List<EventSourceMaterialEntry>())
		{
			if (item != null && item.Sequence > num)
			{
				num = item.Sequence;
			}
		}
		return num;
	}

internal List<NpcActionEntry> ReadEntries(MemoryBusinessStateOwner state, string heroKey, bool recentOnly, Func<int> currentDay)
	{
		string npcActionHeroKey = heroKey;
		if (string.IsNullOrWhiteSpace(npcActionHeroKey))
		{
			return new List<NpcActionEntry>();
		}
		Dictionary<string, List<NpcActionEntry>> dictionary = (recentOnly ? state.RecentActions : state.MajorActions);
		if (dictionary == null || !dictionary.TryGetValue(npcActionHeroKey, out var value) || value == null)
		{
			return new List<NpcActionEntry>();
		}
		return NpcActionLedger.SanitizeNpcActionEntries(value, recentOnly, currentDay());
	}
}
