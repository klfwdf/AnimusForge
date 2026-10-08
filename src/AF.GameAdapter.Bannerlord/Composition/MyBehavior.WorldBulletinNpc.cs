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
        return AnimusForge.Refactor.Adapters.WorldBulletinNpcPromptCaptureAdapter.CaptureWorldBulletinNpcSnapshot(WeeklyCapturePorts, targetHero, targetCharacter, kingdomIdOverride);
    }

	private static List<KeyValuePair<string, string>> ToKingdomDisplayPairs(IEnumerable<string> kingdomIds)
    {
        return AnimusForge.Refactor.Adapters.WorldBulletinNpcPromptCaptureAdapter.ToKingdomDisplayPairs(kingdomIds);
    }

	// Saves sort records newest-first; publishing appends. Compare dates and numeric issue numbers,
	// then cache the index. Normal prompts are O(1); load/list replacement/append/reorder trigger a scan.
	private EventRecordEntry FindLatestWorldBulletinRecord() => WorldBulletinState.FindLatestWorldBulletinRecord();

	private static int GetWorldBulletinRecordSequence(string eventId) => WorldBulletinStateOwner.GetWorldBulletinRecordSequence(eventId);
}
