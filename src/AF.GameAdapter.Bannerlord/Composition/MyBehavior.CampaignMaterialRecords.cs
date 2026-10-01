using AnimusForge.Refactor.Runtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SandBox.Tournaments.MissionLogics;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace AnimusForge;

public partial class MyBehavior
{
		private void RecordEventSourceMaterial(string materialKind, string label, string snapshotText, string stableKey, string kingdomId, string settlementId, bool includeInWorld, bool includeInKingdom, string actorHeroId = "", string actorKingdomId = "", int dayOverride = -1, string gameDateOverride = "")
	{
		string normalizedMaterialKind = (materialKind ?? "").Trim();
		string normalizedActorHeroId = (actorHeroId ?? "").Trim();
		bool isPlayerMaterial = IsPlayerWeeklySourceMaterial(normalizedMaterialKind, normalizedActorHeroId, stableKey);
		string text = isPlayerMaterial
			? PlayerNotorietyBehavior.RenderPlayerHistoryMaterialForExternal(snapshotText)
			: PlayerNotorietyBehavior.RenderPlayerNamedReferenceForExternal(snapshotText);
		text = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
        int day = dayOverride >= 0 ? dayOverride : GetCurrentGameDayIndexSafe();
        _campaignMaterialRecords.Record(new EventSourceMaterialEntry {
            Day = day,
            GameDate = string.IsNullOrWhiteSpace(gameDateOverride) ? GetCurrentGameDateTextSafe() : gameDateOverride.Trim(),
            MaterialKind = normalizedMaterialKind,
            Label = PlayerNotorietyBehavior.RenderPlayerNamedReferenceForExternal(label).Trim(),
            SnapshotText = text, StableKey = stableKey, KingdomId = kingdomId, SettlementId = settlementId,
            ActorHeroId = normalizedActorHeroId, ActorKingdomId = actorKingdomId,
            IncludeInWorld = includeInWorld, IncludeInKingdom = includeInKingdom
        }, () => ++_npcActionGlobalOrderCounter, _weeklyReportMaterialRevisions.MarkDay);
    }

	private void RecordNpcActionInternal(Dictionary<string, List<NpcActionEntry>> storage, Hero hero, string text, string stableKey, bool keepOnlyRecentWindow, bool dedupeAcrossWindow, int maxEntries, NpcActionFacts facts, bool isMajor, bool allowNonLordHero = false)
	{
		try
		{
			if (storage == null || !ShouldTrackNpcActionHero(hero, allowNonLordHero))
			{
				return;
			}
			if (hero == Hero.MainHero)
			{
				RecordPlayerNotorietyActionFromNpcAction(text, stableKey, facts, isMajor);
				return;
			}
            _npcActionRecords.Record(storage, GetNpcActionHeroKey(hero), text, stableKey,
                GetCurrentGameDayIndexSafe(), keepOnlyRecentWindow, dedupeAcrossWindow, maxEntries,
                () => ++_npcActionGlobalOrderCounter,
                (normalizedText, normalizedKey, day, order, sequence) =>
                    CreateNpcActionEntry(hero, normalizedText, normalizedKey, day, order, sequence, facts, isMajor),
                _weeklyReportMaterialRevisions.MarkAll, _weeklyReportMaterialRevisions.MarkDay);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] RecordNpcActionInternal: " + ex.Message);
		}
	}

	private void RecordPlayerNotorietyActionFromNpcAction(string text, string stableKey, NpcActionFacts facts, bool isMajor)
	{
		try
		{
			string normalizedText = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (string.IsNullOrWhiteSpace(normalizedText))
			{
				return;
			}
			int day = GetCurrentGameDayIndexSafe();
			int sequence = ++_npcActionGlobalOrderCounter;
			string actionKind = (facts?.ActionKind ?? "").Trim();
			string settlementId = (facts?.SettlementId ?? "").Trim();
			string settlementName = (facts?.SettlementName ?? "").Trim();
			string locationText = (facts?.LocationText ?? "").Trim();
			Settlement settlement = ResolveSettlementById(settlementId);
			string settlementCultureId = settlement?.Culture?.StringId ?? "";
			string actorCultureId = Hero.MainHero?.Culture?.StringId ?? "";
			string targetCultureId = ResolveHeroCultureId(facts?.TargetHeroId);
			PlayerNotorietyBehavior.RecordPlayerActionForExternal(normalizedText, stableKey, actionKind, isMajor, day, GetCurrentGameDateTextSafe(), sequence, settlementId, settlementName, locationText, actorCultureId, targetCultureId, settlementCultureId, facts?.Won);
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "RecordPlayerNotorietyActionFromNpcAction failed: " + ex.Message);
		}
	}

	internal sealed class EventSourceMaterialEntry
	{
		public int Day;

		public int Sequence;

		public string GameDate;

		public string MaterialKind;

		public string Label;

		public string SnapshotText;

		public string StableKey;

		public string KingdomId;

		public string SettlementId;

		public string ActorHeroId;

		public string ActorKingdomId;

		public bool IncludeInWorld;

		public bool IncludeInKingdom;
	}

    private readonly CampaignMaterialRecordOwner _campaignMaterialRecords = new CampaignMaterialRecordOwner();
    private List<EventSourceMaterialEntry> _eventSourceMaterials {
        get => _campaignMaterialRecords.Materials; set => _campaignMaterialRecords.Materials = value;
    }
    private Dictionary<string, EventSourceMaterialEntry> _eventSourceMaterialIndex {
        get => _campaignMaterialRecords.Index; set => _campaignMaterialRecords.Index = value;
    }
    private EventSourceMaterialIndex<EventSourceMaterialEntry> _eventSourceMaterialIndexBinding => _campaignMaterialRecords.Binding;
    private void RebuildEventSourceMaterialIndex() => _campaignMaterialRecords.RebuildEventSourceMaterialIndex();
    private static List<EventSourceMaterialEntry> SanitizeEventSourceMaterials(List<EventSourceMaterialEntry> source)
        => CampaignMaterialRecordOwner.SanitizeEventSourceMaterials(source);
    private static string BuildEventSourceMaterialIndexKey(int day, string stableKey)
        => CampaignMaterialRecordOwner.BuildEventSourceMaterialIndexKey(day, stableKey);
	private static NpcActionEntry CreateNpcActionEntry(Hero hero, string text, string stableKey, int day, int order, int sequence, NpcActionFacts facts, bool isMajor)
	{
		NpcActionFacts npcActionFacts = facts ?? CreateNpcActionFacts("", hero);
		if (string.IsNullOrWhiteSpace(npcActionFacts.ActorHeroId))
		{
			ApplyActorFacts(npcActionFacts, hero);
		}
		npcActionFacts.IsMajor = isMajor;
		var capture = new NpcActionEntry { GameDate = GetCurrentGameDateTextSafe(),
            ActionKind = npcActionFacts.ActionKind,
            ActorHeroId = npcActionFacts.ActorHeroId,
            ActorClanId = npcActionFacts.ActorClanId,
            ActorKingdomId = npcActionFacts.ActorKingdomId,
            TargetHeroId = npcActionFacts.TargetHeroId,
            TargetClanId = npcActionFacts.TargetClanId,
            TargetKingdomId = npcActionFacts.TargetKingdomId,
            SettlementId = npcActionFacts.SettlementId,
            SettlementName = npcActionFacts.SettlementName,
            SettlementOwnerHeroId = npcActionFacts.SettlementOwnerHeroId,
            SettlementOwnerClanId = npcActionFacts.SettlementOwnerClanId,
            SettlementOwnerKingdomId = npcActionFacts.SettlementOwnerKingdomId,
            PreviousSettlementOwnerHeroId = npcActionFacts.PreviousSettlementOwnerHeroId,
            PreviousSettlementOwnerClanId = npcActionFacts.PreviousSettlementOwnerClanId,
            PreviousSettlementOwnerKingdomId = npcActionFacts.PreviousSettlementOwnerKingdomId,
            LocationText = npcActionFacts.LocationText,
            Won = npcActionFacts.Won,
            RelatedHeroIds = npcActionFacts.RelatedHeroIds,
            RelatedClanIds = npcActionFacts.RelatedClanIds,
            RelatedKingdomIds = npcActionFacts.RelatedKingdomIds
        };
        return NpcActionRecordOwner.Create(capture, text, stableKey, day, order, sequence, isMajor);
	}

    private readonly NpcActionRecordOwner _npcActionRecords = new NpcActionRecordOwner();
    private Dictionary<string, HashSet<string>> _npcRecentActionStableKeyIndex => _npcActionRecords.RecentStableKeys;
    private void RefreshNpcRecentActionStableKeyIndexForHero(string heroKey, List<NpcActionEntry> entries)
        => _npcActionRecords.RefreshNpcRecentActionStableKeyIndexForHero(heroKey, entries);
    private bool IsNpcRecentActionStableKeyKnown(string heroKey, string stableKey)
        => _npcActionRecords.IsNpcRecentActionStableKeyKnown(heroKey, stableKey);

}
