using System;
using System.Collections.Generic;

namespace AnimusForge;

// One action-time path; owns recent-key index, not a duplicate of the saved action stores.
internal sealed class NpcActionRecordOwner
{
    internal readonly Dictionary<string, HashSet<string>> RecentStableKeys = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
    internal void Record(Dictionary<string, List<NpcActionEntry>> storage, string heroKey, string text,
        string stableKey, int currentDay, bool keepOnlyRecentWindow, bool dedupeAcrossWindow,
        int maxEntries, Func<int> nextSequence, Func<string, string, int, int, int, NpcActionEntry> capture,
        Action markAll, Action<int> markDay, Action<int> markAppend = null)
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
			(markAppend ?? markDay)(currentGameDayIndexSafe);
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
    private static int CompareTimeline(NpcActionEntry left, NpcActionEntry right)
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

}
