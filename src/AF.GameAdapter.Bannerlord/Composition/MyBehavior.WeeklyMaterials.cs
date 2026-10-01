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

	private const long WeeklyMemoryMaterialValueThresholdDenars = WeeklyMemoryMaterialPolicy.ValueThresholdDenars;

	private void MarkWeeklyMemoryMaterialTriggerInternal(Hero targetHero, string nonHeroMemoryId, string npcName, string normalizedTagText, int sceneSessionId, int nativeDialogueSessionId, int targetAgentIndex, List<RewardSystemBehavior.RewardItemInfo> rewardOptions, List<PartyTransferPromptEntry> partyTransferTroopOptions, List<PartyTransferPromptEntry> partyTransferPrisonerOptions, List<SettlementTransferPromptEntry> settlementTransferNpcOptions, bool suppressImplicitDialogueSession = false, List<PartyTransferPromptEntry> partyTransferAllTroopOptions = null, List<PartyTransferPromptEntry> partyTransferAllPrisonerOptions = null)
	{
		string memoryId = !string.IsNullOrWhiteSpace(nonHeroMemoryId) ? NormalizeMemoryHeroId(nonHeroMemoryId) : GetMemoryHeroId(targetHero);
		if (!IsMemoryEntityEligibleForCompressedMemory(memoryId))
		{
			return;
		}
		string tagText = WeeklyMemoryMaterialPolicy.NormalizeWeeklyMemoryMaterialTagText(normalizedTagText);
		List<string> tags = WeeklyMemoryMaterialPolicy.ExtractWeeklyMemoryMaterialTags(tagText);
		if (tags.Count == 0)
		{
			return;
		}
		int day = GetCurrentGameDayIndexSafe();
		string gameDate = GetCurrentGameDateTextSafe();
		List<DailyMemoryDraft> drafts = LoadDailyMemoryDraftsById(memoryId);
		DailyMemoryDraft draft = drafts.FirstOrDefault((DailyMemoryDraft x) => x != null && x.GameDayIndex == day);
        var valuePort = WeeklyMaterialValueBannerlordAdapter.Capture(targetHero, rewardOptions,
            partyTransferTroopOptions, partyTransferPrisonerOptions, settlementTransferNpcOptions,
            partyTransferAllTroopOptions, partyTransferAllPrisonerOptions);
		WeeklyMemoryMaterialEvaluation evaluation = WeeklyMemoryMaterialPolicy.EvaluateWeeklyMemoryMaterialTags(tags, tag => WeeklyMemoryMaterialValuePolicy.EstimateWeeklyMemoryMaterialTagValue(tag, valuePort));
		WeeklyMemoryMaterialPolicy.TryApplyPlayerTransferredValueToWeeklyMemoryMaterialEvaluation(evaluation, tags, draft, npcName, sceneSessionId, nativeDialogueSessionId);
		if (evaluation == null || !evaluation.Eligible)
		{
			Logger.Log("EventWeeklyReport", "[WeeklyMemoryMaterial][SKIP] tags_not_eligible memory=" + memoryId + " value=" + (evaluation?.EstimatedValueDenars ?? 0L) + " threshold=" + WeeklyMemoryMaterialValueThresholdDenars + " tags=" + string.Join("|", tags));
			return;
		}
		if (!ResolvePlayerFootholdKingdomForWeeklyMemoryMaterial(out var footholdKingdomId, out var footholdSettlementId))
		{
			Logger.Log("EventWeeklyReport", "[WeeklyMemoryMaterial][SKIP] foothold_kingdom_missing memory=" + memoryId + " tags=" + string.Join("|", tags));
			return;
		}
		if (!suppressImplicitDialogueSession && sceneSessionId < 0 && nativeDialogueSessionId < 0)
		{
			nativeDialogueSessionId = GetOrStartActiveNativeConversationMemorySessionId();
		}
		WeeklyMemoryMaterialTrigger trigger = WeeklyMemoryMaterialPolicy.CreateTrigger(memoryId,
            npcName, day, gameDate, sceneSessionId, nativeDialogueSessionId, targetAgentIndex,
            footholdKingdomId, footholdSettlementId, tagText, evaluation, DateTime.UtcNow.Ticks);
        bool attached = _memoryBusinessState.StageOrAttachWeeklyTrigger(trigger, day);
        Logger.Log("EventWeeklyReport", "[WeeklyMemoryMaterial] trigger_" + (attached ? "attached" : "pending")
            + " memory=" + memoryId + " day=" + day + " kingdom=" + footholdKingdomId
            + " value=" + evaluation.EstimatedValueDenars + " tags=" + string.Join("|", evaluation.Tags));
	}

	private void AddWeeklyMemoryMaterialTriggerToDraft(DailyMemoryDraft draft, WeeklyMemoryMaterialTrigger trigger)
		=> _memoryBusinessState.AddWeeklyTrigger(draft, trigger);

	private void AttachPendingWeeklyMemoryMaterialTriggers(DailyMemoryDraft draft, DailyMemoryLine line)
		=> _memoryBusinessState.AttachPendingWeeklyTriggers(draft, line, GetCurrentGameDayIndexSafe());

	private void PrunePendingWeeklyMemoryMaterialTriggers()
		=> _memoryBusinessState.PrunePendingWeeklyTriggers(GetCurrentGameDayIndexSafe());

	private void RecordPublicDailyMemoryWeeklyMaterial(CompressedMemoryBlock block)
	{
		string publicity = (block?.PlayerPublicity ?? "").Trim().ToLowerInvariant();
		if (block == null || (publicity != "public" && publicity != "leaked_public"))
		{
			return;
		}
		string material = (block.PlayerHistoryMaterial ?? "").Trim();
		if (string.IsNullOrWhiteSpace(material))
		{
			return;
		}
		if (!ResolvePlayerFootholdKingdomForWeeklyMemoryMaterial(out var kingdomId, out var settlementId))
		{
			Logger.Log("EventWeeklyReport", "[PublicDailyMemory][SKIP] foothold_kingdom_missing block=" + (block.Id ?? "") + " memory=" + (block.HeroId ?? ""));
			return;
		}
		string blockId = (block.Id ?? BuildCompressedMemoryBlockId(block.HeroId, block.GameDayIndex)).Trim();
		string stableHash = WeeklyMemoryMaterialPolicy.ComputeWeeklyMemoryMaterialHash(blockId + "|" + kingdomId + "|" + material);
		string stableKey = "public_daily_memory:" + kingdomId + ":" + blockId + ":" + stableHash;
		string label = "公开聊天日结 - " + (string.IsNullOrWhiteSpace(block.HeroName) ? "NPC" : block.HeroName.Trim());
		string snapshot = WeeklyMemoryMaterialPolicy.BuildPublicDailyMemoryWeeklyMaterialSnapshotText(block, material, FormatMemoryHourRange(block?.StartHour ?? 0, block?.EndHour ?? 0));
		RecordEventSourceMaterial("public_daily_memory", label, snapshot, stableKey, kingdomId, settlementId, includeInWorld: false, includeInKingdom: true, actorHeroId: GetHeroId(Hero.MainHero), actorKingdomId: kingdomId, dayOverride: block.GameDayIndex, gameDateOverride: block.GameDate);
		Logger.Log("EventWeeklyReport", "[PublicDailyMemory] source_material_recorded block=" + blockId + " kingdom=" + kingdomId + " publicity=" + publicity);
	}

	private void RecordWeeklyMemoryMaterialForBlock(CompressedMemoryBlock block)
	{
		List<WeeklyMemoryMaterialTrigger> triggers = SanitizeWeeklyMemoryMaterialTriggers(block?.WeeklyMaterialTriggers);
		if (block == null || triggers.Count == 0)
		{
			return;
		}
		string publicity = (block.PlayerPublicity ?? "").Trim().ToLowerInvariant();
		if (publicity != "public" && publicity != "leaked_public")
		{
			Logger.Log("EventWeeklyReport", "[WeeklyMemoryMaterial][SKIP] hidden_by_daily_publicity block=" + (block.Id ?? "") + " publicity=" + (string.IsNullOrWhiteSpace(publicity) ? "empty" : publicity) + " triggers=" + triggers.Count);
			return;
		}
        string blockId = (block.Id ?? BuildCompressedMemoryBlockId(block.HeroId, block.GameDayIndex)).Trim();
        foreach (var material in WeeklyMemoryMaterialPolicy.BuildDialogueMaterials(block, triggers, blockId))
        {
            RecordEventSourceMaterial("player_dialogue_memory", material.Label, material.Snapshot,
                material.StableKey, material.KingdomId, material.SettlementId,
                includeInWorld: false, includeInKingdom: true, actorHeroId: GetHeroId(Hero.MainHero),
                actorKingdomId: material.KingdomId, dayOverride: block.GameDayIndex, gameDateOverride: block.GameDate);
            Logger.Log("EventWeeklyReport", "[WeeklyMemoryMaterial] source_material_recorded block="
                + (block.Id ?? "") + " kingdom=" + material.KingdomId + " triggers=" + material.TriggerCount
                + " value=" + material.Value);
        }
	}

	private static bool ResolvePlayerFootholdKingdomForWeeklyMemoryMaterial(out string kingdomId, out string settlementId)
	{
		kingdomId = "";
		settlementId = "";
		try
		{
			Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
			settlementId = GetSettlementId(settlement);
			kingdomId = GetKingdomId(settlement?.MapFaction);
			if (string.IsNullOrWhiteSpace(kingdomId))
			{
				kingdomId = GetKingdomId(settlement?.OwnerClan?.Kingdom);
			}
			if (!string.IsNullOrWhiteSpace(kingdomId))
			{
				return true;
			}
			List<string> nearest = GetKingdomIdsByPlayerProximity(GetDevEditableKingdoms().Select((Kingdom x) => x?.StringId));
			kingdomId = nearest.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)) ?? "";
			return !string.IsNullOrWhiteSpace(kingdomId);
		}
		catch
		{
			kingdomId = "";
			settlementId = "";
			return false;
		}
	}

	private static string BuildWeeklyMemoryMaterialTagLabel(string tag)
		=> WeeklyMemoryMaterialPolicy.BuildWeeklyMemoryMaterialTagLabel(tag);
}
