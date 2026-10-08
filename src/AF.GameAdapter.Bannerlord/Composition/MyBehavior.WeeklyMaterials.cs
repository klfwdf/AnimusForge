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

	private void MarkWeeklyMemoryMaterialTriggerInternal(Hero targetHero, string nonHeroMemoryId, string npcName, string normalizedTagText, int sceneSessionId, int nativeDialogueSessionId, int targetAgentIndex, List<RewardSystemBehavior.RewardItemInfo> rewardOptions, List<PartyTransferPromptEntry> partyTransferTroopOptions, List<PartyTransferPromptEntry> partyTransferPrisonerOptions, List<SettlementTransferPromptEntry> settlementTransferNpcOptions, bool suppressImplicitDialogueSession = false, List<PartyTransferPromptEntry> partyTransferAllTroopOptions = null, List<PartyTransferPromptEntry> partyTransferAllPrisonerOptions = null) => _memoryHistoryCommit.MarkWeeklyMemoryMaterialTriggerInternal(targetHero, nonHeroMemoryId, npcName, normalizedTagText, sceneSessionId, nativeDialogueSessionId, targetAgentIndex, rewardOptions, partyTransferTroopOptions, partyTransferPrisonerOptions, settlementTransferNpcOptions, suppressImplicitDialogueSession, partyTransferAllTroopOptions, partyTransferAllPrisonerOptions);

	private void AddWeeklyMemoryMaterialTriggerToDraft(DailyMemoryDraft draft, WeeklyMemoryMaterialTrigger trigger)
		=> _memoryBusinessState.AddWeeklyTrigger(draft, trigger);

	private void AttachPendingWeeklyMemoryMaterialTriggers(DailyMemoryDraft draft, DailyMemoryLine line)
		=> _memoryBusinessState.AttachPendingWeeklyTriggers(draft, line, GetCurrentGameDayIndexSafe());

	private void PrunePendingWeeklyMemoryMaterialTriggers()
		=> _memoryBusinessState.PrunePendingWeeklyTriggers(GetCurrentGameDayIndexSafe());

	private void RecordPublicDailyMemoryWeeklyMaterial(CompressedMemoryBlock block) => _campaignCharacterRecordCapture.RecordPublicDailyMemoryWeeklyMaterial(block);

	private void RecordWeeklyMemoryMaterialForBlock(CompressedMemoryBlock block) => _campaignCharacterRecordCapture.RecordWeeklyMemoryMaterialForBlock(block);

    private static bool ResolvePlayerFootholdKingdomForWeeklyMemoryMaterial(out string kingdomId, out string settlementId) => MemoryEntityIdentityBannerlordAdapter.ResolvePlayerFootholdKingdomForWeeklyMemoryMaterial(out kingdomId, out settlementId);

	private static string BuildWeeklyMemoryMaterialTagLabel(string tag)
		=> WeeklyMemoryMaterialPolicy.BuildWeeklyMemoryMaterialTagLabel(tag);
}
