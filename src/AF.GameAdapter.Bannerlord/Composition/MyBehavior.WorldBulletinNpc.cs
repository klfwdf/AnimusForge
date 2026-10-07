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
	private List<EventRecordEntry> _worldBulletinCachedRecords { get => _worldBulletinOwner.CachedRecords; set => _worldBulletinOwner.CachedRecords = value; }
	private int _worldBulletinCachedRecordCount { get => _worldBulletinOwner.CachedRecordCount; set => _worldBulletinOwner.CachedRecordCount = value; }
	private int _worldBulletinCachedRecordIndex { get => _worldBulletinOwner.CachedRecordIndex; set => _worldBulletinOwner.CachedRecordIndex = value; }

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
		EventRecordEntry latest = FindLatestWorldBulletinRecord();
        bool requiresDiplomacy = events.Any(e => e != null && WorldBulletinPolicy.IsDiplomacyFact(e.Kind, e.Key))
            || WeeklyReportArchivePolicy.RequiresNpcDiplomacyKnowledge(latest);
        ISet<string> knownDocumentIds = null;
        if (requiresDiplomacy)
        {
            try
            {
                knownDocumentIds = DiplomacyModuleServices.World.CaptureKnownDocumentIds(
                    (targetHero ?? targetCharacter?.HeroObject)?.StringId, npcKingdomId);
            }
            catch (Exception ex) { Logger.Log("WorldBulletin", "[WARN] NPC diplomacy knowledge unavailable: " + ex.Message); }
            // One bounded pass over retained facts, one knowledge capture, O(1) ID lookups per fact.
            events = events.Where(e => e != null && WorldBulletinPolicy.IsNpcFactVisible(e.Kind, e.Key, knownDocumentIds)).ToList();
        }
		var latestVisible = WeeklyReportArchivePolicy.ProjectBulletinForNpc(latest, knownDocumentIds);
		string npcDetail = string.IsNullOrWhiteSpace(npcKingdomId) ? "" : WorldBulletinPolicy.BuildNpcDetailBlock(WorldBulletinPolicy.NpcKingdomHeader, ResolveKingdomDisplay(npcKingdomId), npcKingdomId, events, day);
		string surroundingsDetail = string.IsNullOrWhiteSpace(surroundingsKingdomId) ? "" : WorldBulletinPolicy.BuildNpcDetailBlock(WorldBulletinPolicy.NpcSurroundingsHeader, ResolveKingdomDisplay(surroundingsKingdomId), surroundingsKingdomId, events, day);
		// World headlines are emitted next to the NPC-kingdom block (npc_major_actions), so its facts are skipped there.
		string world = WorldBulletinPolicy.BuildNpcWorldBlock(events, day, latestVisible.Title, latestVisible.Facts, latest?.CreatedDay ?? -1, npcKingdomId, latestVisible.Anecdote);
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
	private EventRecordEntry FindLatestWorldBulletinRecord() => WorldBulletinState.FindLatestWorldBulletinRecord();

	private static int GetWorldBulletinRecordSequence(string eventId) => WorldBulletinStateOwner.GetWorldBulletinRecordSequence(eventId);
}
