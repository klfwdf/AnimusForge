using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

// Bulletin mode's NPC knowledge: assembled locally from captured facts on each prompt capture, no LLM.
// Keeps the legacy WeeklyPromptSnapshot contract, so the prompt pipeline downstream is untouched:
//   always-on  -> "近期三个王国发生的事": NPC kingdom first, then kingdoms nearest the player (3 total)
//   npc_major  -> NPC kingdom facts with details + world headlines and the latest bulletin
//   surroundings -> facts of the kingdom owning the current settlement
// Cost: one pass over <= MaxEvents facts per kingdom block, main thread, a handful of kingdoms.
public partial class MyBehavior
{
	private List<EventRecordEntry> _worldBulletinCachedRecords;
	private int _worldBulletinCachedRecordCount = -1;
	private int _worldBulletinCachedRecordIndex = -1;

	private WeeklyPromptSnapshot CaptureWorldBulletinNpcSnapshot(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride)
	{
		List<WorldBulletinEvent> events = _worldBulletinState?.Events ?? new List<WorldBulletinEvent>();
		int day = GetCurrentGameDayIndexSafe();
		string npcKingdomId = ResolveWeeklyReportNpcKingdomId(targetHero, targetCharacter, kingdomIdOverride);
		string surroundingsKingdomId = ResolveWeeklyReportSurroundingsKingdomId(targetHero, targetCharacter, kingdomIdOverride);
		List<string> eligible = GetDevEditableKingdoms().Where(IsKingdomEligibleForWeeklyReport).Select(k => GetKingdomId(k)).ToList();
		List<string> nearest = GetKingdomIdsByPlayerProximity(eligible);
		bool npcEligible = !string.IsNullOrWhiteSpace(npcKingdomId) && eligible.Contains(npcKingdomId, StringComparer.OrdinalIgnoreCase);
		List<string> including = SelectWeeklyShortReportKingdomIdsFromSnapshot(npcKingdomId, excludeNpcKingdom: false, npcEligible, nearest, eligible);
		List<string> excluding = SelectWeeklyShortReportKingdomIdsFromSnapshot(npcKingdomId, excludeNpcKingdom: true, npcEligible, nearest, eligible);
		string npcDetail = string.IsNullOrWhiteSpace(npcKingdomId) ? "" : WorldBulletinPolicy.BuildNpcDetailBlock(WorldBulletinPolicy.NpcKingdomHeader, ResolveKingdomDisplay(npcKingdomId), npcKingdomId, events, day);
		string surroundingsDetail = string.IsNullOrWhiteSpace(surroundingsKingdomId) ? "" : WorldBulletinPolicy.BuildNpcDetailBlock(WorldBulletinPolicy.NpcSurroundingsHeader, ResolveKingdomDisplay(surroundingsKingdomId), surroundingsKingdomId, events, day);
		EventRecordEntry latest = FindLatestWorldBulletinRecord();
		// World headlines are emitted next to the NPC-kingdom block (npc_major_actions), so its facts are skipped there.
		string world = WorldBulletinPolicy.BuildNpcWorldBlock(events, day, latest?.Title, latest?.ShortSummary, latest?.CreatedDay ?? -1, npcKingdomId);
		return new WeeklyPromptSnapshot(
			WorldBulletinPolicy.BuildNpcBriefBlock(ToKingdomDisplayPairs(including), events, day),
			WorldBulletinPolicy.BuildNpcBriefBlock(ToKingdomDisplayPairs(excluding), events, day),
			npcDetail,
			world,
			surroundingsDetail,
			npcKingdomId,
			surroundingsKingdomId);
	}

	private static List<KeyValuePair<string, string>> ToKingdomDisplayPairs(IEnumerable<string> kingdomIds)
	{
		return (kingdomIds ?? Enumerable.Empty<string>())
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => new KeyValuePair<string, string>(x.Trim(), ResolveKingdomDisplay(x)))
			.ToList();
	}

	// Saves sort records newest-first; publishing appends. Compare dates and numeric issue numbers,
	// then cache the index. Normal prompts are O(1); load/list replacement/append/reorder trigger a scan.
	private EventRecordEntry FindLatestWorldBulletinRecord()
	{
		if ((_worldBulletinState?.World?.Sequence ?? 0) <= 0)
		{
			return null;
		}
		List<EventRecordEntry> entries = _eventRecordEntries;
		if (entries == null)
		{
			return null;
		}
		if (ReferenceEquals(entries, _worldBulletinCachedRecords) && entries.Count == _worldBulletinCachedRecordCount)
		{
			if (_worldBulletinCachedRecordIndex < 0)
			{
				return null;
			}
			EventRecordEntry cached = entries[_worldBulletinCachedRecordIndex];
			if (cached != null && string.Equals(cached.EventId, _worldBulletinLatestEventId, StringComparison.OrdinalIgnoreCase))
			{
				return cached;
			}
		}
		EventRecordEntry latest = null;
		int latestIndex = -1;
		int latestSequence = -1;
		for (int i = 0; i < entries.Count; i++)
		{
			EventRecordEntry entry = entries[i];
			if (entry != null && IsWorldBulletinEventId(entry.EventId) && string.Equals((entry.EventKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase))
			{
				int sequence = GetWorldBulletinRecordSequence(entry.EventId);
				if (latest == null || entry.CreatedDay > latest.CreatedDay || (entry.CreatedDay == latest.CreatedDay && sequence > latestSequence))
				{
					latest = entry;
					latestIndex = i;
					latestSequence = sequence;
				}
			}
		}
		_worldBulletinCachedRecords = entries;
		_worldBulletinCachedRecordCount = entries.Count;
		_worldBulletinCachedRecordIndex = latestIndex;
		_worldBulletinLatestEventId = latest?.EventId ?? "";
		return latest;
	}

	private static int GetWorldBulletinRecordSequence(string eventId)
	{
		string[] parts = (eventId ?? "").Split(':');
		return parts.Length >= 5 && int.TryParse(parts[parts.Length - 2], out int sequence) ? sequence : -1;
	}
}
