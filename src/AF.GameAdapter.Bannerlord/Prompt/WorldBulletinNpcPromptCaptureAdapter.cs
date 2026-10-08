using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using WeeklyPromptSnapshot = AnimusForge.MyBehavior.WeeklyPromptSnapshot;
using EventRecordEntry = AnimusForge.MyBehavior.EventRecordEntry;
namespace AnimusForge.Refactor.Adapters;
internal static class WorldBulletinNpcPromptCaptureAdapter
{
internal static WeeklyPromptSnapshot CaptureWorldBulletinNpcSnapshot(WeeklyPromptCaptureAdapter.CapturePorts ports, Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride)
	{
		List<WorldBulletinEvent> events = ports.BulletinEvents() ?? new List<WorldBulletinEvent>();
		int day = MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();
		string npcKingdomId = ports.NpcKingdom(targetHero, targetCharacter, kingdomIdOverride);
		string surroundingsKingdomId = ports.SurroundingsKingdom(targetHero, targetCharacter, kingdomIdOverride);
		List<string> eligible = ports.EditableKingdoms().Where(ports.Eligible).Select(k => MemoryEntityIdentityBannerlordAdapter.GetKingdomId(k)).ToList();
		List<string> nearest = ports.Proximity(eligible);
		bool npcEligible = !string.IsNullOrWhiteSpace(npcKingdomId) && eligible.Contains(npcKingdomId, StringComparer.OrdinalIgnoreCase);
		List<string> including = ports.SelectSnapshot(npcKingdomId, false, npcEligible, nearest, eligible);
		List<string> excluding = ports.SelectSnapshot(npcKingdomId, true, npcEligible, nearest, eligible);
		EventRecordEntry latest = ports.LatestBulletin();
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
		string npcDetail = string.IsNullOrWhiteSpace(npcKingdomId) ? "" : WorldBulletinPolicy.BuildNpcDetailBlock(WorldBulletinPolicy.NpcKingdomHeader, MyBehavior.ResolveKingdomDisplay(npcKingdomId), npcKingdomId, events, day);
		string surroundingsDetail = string.IsNullOrWhiteSpace(surroundingsKingdomId) ? "" : WorldBulletinPolicy.BuildNpcDetailBlock(WorldBulletinPolicy.NpcSurroundingsHeader, MyBehavior.ResolveKingdomDisplay(surroundingsKingdomId), surroundingsKingdomId, events, day);
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
internal static List<KeyValuePair<string, string>> ToKingdomDisplayPairs(IEnumerable<string> kingdomIds)
	{
		return (kingdomIds ?? Enumerable.Empty<string>())
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => new KeyValuePair<string, string>(x.Trim(), MyBehavior.ResolveKingdomDisplay(x)))
			.ToList();
	}
}
