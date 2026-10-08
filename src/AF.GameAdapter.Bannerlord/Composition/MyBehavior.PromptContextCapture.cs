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
    private static readonly WeeklyPromptCaptureAdapter.RequestPromptCapturePorts WeeklyRequestPromptSettings = new WeeklyPromptCaptureAdapter.RequestPromptCapturePorts
    {
        Profile = GetWeeklyReportPromptProfile,
        WritingRequirements = CaptureWeeklyReportWritingRequirements
    };
    private WeeklyPromptCaptureAdapter.RequestPromptCapturePorts _weeklyRequestPromptCapturePorts;
    private WeeklyPromptCaptureAdapter.RequestPromptCapturePorts WeeklyRequestPromptCapturePorts => _weeklyRequestPromptCapturePorts ??= new WeeklyPromptCaptureAdapter.RequestPromptCapturePorts
    {
        Profile = GetWeeklyReportPromptProfile,
        WritingRequirements = CaptureWeeklyReportWritingRequirements,
        Stability = _kingdomStabilityGameAdapter.BuildWeeklyReportCurrentKingdomStabilityTierText,
        Previous = (group, week) => _weeklyEventRecords.GetPreviousWeeklyReportText(group, week, FormatNewsCalendarDate),
        Materials = WeeklyPromptCaptureAdapter.BuildWeeklyReportPromptMaterialLines,
        DefaultTitle = (group, week) => WeeklyEventRecordStateOwner.BuildDefaultWeeklyReportTitle(group, week, FormatNewsCalendarDate, MemoryEntityIdentityBannerlordAdapter.ResolveKingdomDisplay)
    };
    private static WeeklyPromptCaptureAdapter.CapturePorts ResolveWeeklyCapturePorts() => Campaign.Current?.GetCampaignBehavior<MyBehavior>()?.WeeklyCapturePorts;

    private WeeklyPromptCaptureAdapter.CapturePorts _weeklyCapturePorts;
    private WeeklyPromptCaptureAdapter.CapturePorts WeeklyCapturePorts => _weeklyCapturePorts ??= new WeeklyPromptCaptureAdapter.CapturePorts
    {
        BulletinEnabled = IsWorldBulletinPublishingEnabled,
        BulletinEvents = () => _worldBulletinOwner.State?.Events,
        LatestBulletin = () => WorldBulletinState.FindLatestWorldBulletinRecord(),
        Records = () => _weeklyEventRecords.Records,
        EnsureOpening = () => _weekZeroShortSummaries.EnsureWeekZeroOpeningSummaryEvents(),
        NpcKingdom = MemoryEntityIdentityBannerlordAdapter.ResolveWeeklyReportNpcKingdomId,
        SurroundingsKingdom = MemoryEntityIdentityBannerlordAdapter.ResolveWeeklyReportSurroundingsKingdomId,
        EditableKingdoms = EventEditorProjection.GetDevEditableKingdoms,
        Eligible = MemoryEntityIdentityBannerlordAdapter.IsKingdomEligibleForWeeklyReport,
        Proximity = MemoryEntityIdentityBannerlordAdapter.GetKingdomIdsByPlayerProximity,
        SelectSnapshot = WeeklyEventRecordStateOwner.SelectWeeklyShortReportKingdomIdsFromSnapshot,
        SelectLive = MemoryEntityIdentityBannerlordAdapter.SelectWeeklyShortReportKingdomIds,
        Latest = _weekZeroShortSummaries.FindLatestWeeklyReportRecord
    };
    private SharedPromptCaptureBannerlordAdapter.RequestCapturePorts _sharedRequestCapturePorts;
    private SharedPromptCaptureBannerlordAdapter.RequestCapturePorts SharedRequestCapturePorts => _sharedRequestCapturePorts ??=
        new SharedPromptCaptureBannerlordAdapter.RequestCapturePorts(() => _cachedPlayerClanTier, _memoryHistoryCommit.GetLatestNpcDialogueUtterance, _memoryHistoryCommit.LoadDialogueHistory);

	private void CapturePromptSections(PromptBuildRequest request, PromptRoutingResult routing, PromptRetrievalCapture retrieval, MentionedWorldEntities directPreprocessMentionedEntities, Hero targetHero, CharacterObject targetCharacter, WeeklyPromptSnapshot weeklyPromptSnapshot, Stopwatch promptContextTotalSw, Stopwatch promptContextStageSw,
		out PromptContextFlags contextFlags, out PromptExtrasSections extrasSections, out PromptEntityCapture entityCapture, out MentionedWorldEntities mentionedEntities)
	{
        PromptContextCaptureBannerlordAdapter.CapturePromptSections(
            CreatePromptContextCapturePorts(request, routing, retrieval, targetHero, targetCharacter, promptContextTotalSw, promptContextStageSw),
            request, routing, retrieval, directPreprocessMentionedEntities, targetHero, targetCharacter, weeklyPromptSnapshot,
            out contextFlags, out extrasSections, out entityCapture, out mentionedEntities);
        extrasSections.CurrentFamilyStatus = WorldEntityRetrievalService.BuildCurrentFamilyPrompt(targetHero ?? targetCharacter?.HeroObject);
    }

    private SharedPromptCaptureBannerlordAdapter.ExternalPromptBuildPorts _externalPromptBuildCapture;
    private SharedPromptCaptureBannerlordAdapter.ExternalPromptBuildPorts ExternalPromptBuildCapture => _externalPromptBuildCapture ??=
        new SharedPromptCaptureBannerlordAdapter.ExternalPromptBuildPorts(SharedRequestCapturePorts, CaptureSharedPromptRoutingWork,
            (phases,hero,character) => CreatePromptContextCapturePorts(phases.Request, phases.Routing, phases.Retrieval,
                hero ?? character?.HeroObject, character, phases.TotalStopwatch, phases.StageStopwatch));

    private PromptContextCaptureBannerlordPorts CreatePromptContextCapturePorts(PromptBuildRequest request,
        PromptRoutingResult routing, PromptRetrievalCapture retrieval, Hero targetHero, CharacterObject targetCharacter,
        Stopwatch total, Stopwatch stage)
    {
        return new PromptContextCaptureBannerlordPorts
        {
            IsPartyTransferEligible = () => IsPartyTransferRuleEligible(targetHero, targetCharacter, request.TargetAgentIndex),
            HasDuelRuntimeTarget = () => HasDuelRuntimeTarget(targetHero, targetCharacter, request.TargetAgentIndex),
            WasRecentlyDefeated = id => _recentlyDefeatedByPlayer.Contains(id),
            WasRecentlyReleased = id => _recentlyReleasedPrisoners.Contains(id),
            BuildPlayerDisplayName = (hero, character, index) => BuildPlayerPublicDisplayNameForPrompt(hero, character, index),
            BuildPrisonerStatus = SharedPromptCaptureBannerlordAdapter.BuildHeroPrisonerStatusPromptLineForExternal,
            BuildHeroArmyFact = () => BuildHeroArmyRuntimeFactForPrompt(targetHero),
            BuildPlayerArmyFact = () => BuildPlayerArmyRuntimeFactForPrompt(targetHero, targetCharacter, request.TargetAgentIndex),
            BuildResidentRecentActions = () => MemoryEntityIdentityBannerlordAdapter.BuildResidentRecentActionsPrompt(_memoryBusinessState, _npcActionRecords, MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe, BuildRuleTargetKeyForExternal, targetHero, targetCharacter, request.TargetAgentIndex),
            BuildTriggeredRules = flags => PromptRuleCaptureBannerlordAdapter.BuildTriggeredRuleInstructions(NpcMajorRuleCapture, request.Input, targetHero, flags.UseDuelContext,
                request.IsQualified, request.PlayerClanTier, flags.UseRewardContext, flags.IsLoanContext,
                routing.Surroundings.Hit, request.HasAnyHero, targetCharacter, request.KingdomIdOverride,
                request.TargetAgentIndex, request.NpcLastUtterance, flags.IncludeDuelStakeContext,
                flags.PlayerWonLastDuel, routing.WorldMapPartyCommand.Hit, request.ExcludedRuleIds,
                routing.AuxiliaryRuleHitIds, PromptRuleIdPolicy.IsExcluded(request.ExplicitExcludedRuleIds, "meeting_taunt"),
                retrieval?.FallbackExtraRuleHits),
            IsWorldBulletinEnabled = IsWorldBulletinPublishingEnabled,
            CaptureWorldBulletinSnapshot = () => CaptureWorldBulletinNpcSnapshot(targetHero, targetCharacter, request.KingdomIdOverride),
            ShouldExcludeWeeklyShortReport = (rules, snapshot) => WeekZeroOpeningSummaryGenerationController.ShouldExcludeNpcShortReportFromWeeklyShortLayer(rules, targetHero, targetCharacter, request.KingdomIdOverride, snapshot),
            BuildWeeklyShortReports = (exclude, snapshot) => BuildWeeklyShortReportsPromptBlock(targetHero, targetCharacter, request.KingdomIdOverride, exclude, snapshot),
            BuildWeeklyFullReports = (rules, snapshot) => BuildTriggeredWeeklyFullReportsPromptBlock(rules, targetHero, targetCharacter, request.KingdomIdOverride, snapshot),
            ObserverKnowsPlayer = () => DoesPlayerNotorietyObserverKnowPlayer(targetHero, targetCharacter, request.TargetAgentIndex),
            LogStage = (name, detail, immediate) => LogShoutPromptContextStage(name, total, stage, targetHero, targetCharacter, request.TargetAgentIndex, detail, immediate)
        };
    }
}
