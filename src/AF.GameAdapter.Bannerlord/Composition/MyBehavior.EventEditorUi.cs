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
	internal sealed class DevKingdomSummaryMenuItem
	{
		public string KingdomId;

		public string DisplayName;
	}

 private static readonly EventEditorDisplayPort EventEditorDisplay = new EventEditorDisplayPort
 {
  GetKingdomDisplayName = GetKingdomDisplayName,
  GetClanDisplayName = GetClanDisplayName,
  FormatKingdomRebellionChance = FormatKingdomRebellionChance,
  TranslateEventKindForDev = TranslateEventKindForDev,
  ResolveKingdomDisplay = ResolveKingdomDisplay,
  TranslateEventMaterialTypeForDev = TranslateEventMaterialTypeForDev,
 };
 private EventEditorController _eventEditor;
 private EventEditorController EventEditor
 {
 get {
 var owner = _eventEditor ?? (_eventEditor = new EventEditorController(new EventEditorPort
 {
 CaptureGeneration = SaveRuntimeGuard.CaptureGeneration, IsCurrent = IsMemorySourceEditorCurrent,
 GetKingdomDisplayName = GetKingdomDisplayName,
 GetClanDisplayName = GetClanDisplayName,
 FormatKingdomRebellionChance = FormatKingdomRebellionChance,
 TranslateEventKindForDev = TranslateEventKindForDev,
 ResolveKingdomDisplay = ResolveKingdomDisplay,
 TranslateEventMaterialTypeForDev = TranslateEventMaterialTypeForDev,
 EnsureWeekZeroOpeningSummaryEvents = EnsureWeekZeroOpeningSummaryEvents,
 OpenDevWeeklyEventMaterialPreviewMenu = OpenDevWeeklyEventMaterialPreviewMenu,
 OpenDevWeeklyReportPromptPreviewMenu = OpenDevWeeklyReportPromptPreviewMenu,
 ConfirmGenerateDevWeeklyReports = ConfirmGenerateDevWeeklyReports,
 OpenDevKingdomStabilityLabMenu = OpenDevKingdomStabilityLabMenu,
 OpenExportFolderPicker = OpenExportFolderPicker,
 OpenImportFolderPicker = OpenImportFolderPicker,
 FindKingdomById = FindKingdomById,
 GetKingdomOpeningSummary = GetKingdomOpeningSummary,
 SaveKingdomOpeningSummary = SaveKingdomOpeningSummary,
 GetKingdomStabilityValue = GetKingdomStabilityValue,
 SanitizeEventRecordEntries = SanitizeEventRecordEntries,
 AppendDevNpcActionField = AppendDevNpcActionField,
 ShowDevLargeTextOrInquiry = ShowDevLargeTextOrInquiry,
 BuildDevEventMaterialDetailText = BuildDevEventMaterialDetailText,
 WorldOpeningSummary = () => _eventWorldOpeningSummary, PromptProfileLabel = () => GetWeeklyReportPromptProfile().Label,
 CountConfiguredOpeningSummaries = () => _eventKingdomOpeningSummaries?.Count(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value)) ?? 0,
 EventRecords = () => _eventRecordEntries, StabilityDefault = KingdomStabilityDefaultValue,
 SetDeveloperWorldOpeningSummary = SetDeveloperWorldOpeningSummary, ClearDeveloperOpeningSummaries = ClearDeveloperOpeningSummaries,
 ApplyDeveloperEventTitle = ApplyDeveloperEventTitle, ApplyDeveloperEventReport = ApplyDeveloperEventReport,
 }));
 owner.SynchronizeGeneration(SaveRuntimeGuard.CaptureGeneration());
 return owner;
 }
 }
 private KingdomStabilityLabController _kingdomStabilityLab;
 private KingdomStabilityLabController KingdomStabilityLab
 {
 get {
 var owner = _kingdomStabilityLab ?? (_kingdomStabilityLab = new KingdomStabilityLabController(new KingdomStabilityLabPort
 {
 CaptureGeneration = SaveRuntimeGuard.CaptureGeneration, IsCurrent = IsMemorySourceEditorCurrent,
 GetKingdomDisplayName = GetKingdomDisplayName,
 GetClanDisplayName = GetClanDisplayName,
 FormatKingdomRebellionChance = FormatKingdomRebellionChance,
 TranslateEventKindForDev = TranslateEventKindForDev,
 ResolveKingdomDisplay = ResolveKingdomDisplay,
 TranslateEventMaterialTypeForDev = TranslateEventMaterialTypeForDev,
 GetKingdomStabilityValue = GetKingdomStabilityValue,
 GetKingdomStabilityTierText = GetKingdomStabilityTierText,
 GetKingdomRebellionWeeklyChance = GetKingdomRebellionWeeklyChance,
 GetKingdomStabilityRelationTargetOffset = GetKingdomStabilityRelationTargetOffset,
 GetKingdomStabilityWeeklyBalancingDelta = GetKingdomStabilityWeeklyBalancingDelta,
 CountActiveKingdomClansForLowClanCountRule = CountActiveKingdomClansForLowClanCountRule,
 GetLowClanCountRoyalDomainLoyaltyAdjustment = GetLowClanCountRoyalDomainLoyaltyAdjustment,
 EvaluateKingdomRebellionCandidates = (kingdom, forceTrigger) => EvaluateKingdomRebellionCandidates(kingdom, forceTrigger),
 GetHeroDisplayName = GetHeroDisplayName,
 FormatKingdomStabilityRelationOffsetText = FormatKingdomStabilityRelationOffsetText,
 ClampKingdomStabilityValue = ClampKingdomStabilityValue,
 SetKingdomStabilityValue = SetKingdomStabilityValue,
 ResolveKingdomRebellion = ResolveKingdomRebellion,
 StartDevForcedKingdomRebellionAsync = StartDevForcedKingdomRebellionAsync,
 FindKingdomById = FindKingdomById,
 GetCurrentGameDayIndexSafe = GetCurrentGameDayIndexSafe,
 OpenDevEventEditorMenu = OpenDevEventEditorMenu,
 }));
 return owner;
 }
 }
	private void OpenDevEventEditorMenu()
		=> EventEditor.OpenDevEventEditorMenu();

	private void OnDevEventEditorMenuSelected(List<InquiryElement> selected)
		=> EventEditor.OnDevEventEditorMenuSelected(selected);

	private string BuildDevEventEditorMenuDescription()
		=> EventEditor.BuildDevEventEditorMenuDescription();

	private void OpenDevEditWorldOpeningSummary()
		=> EventEditor.OpenDevEditWorldOpeningSummary();

	private void OpenDevKingdomOpeningSummaryMenu()
		=> EventEditor.OpenDevKingdomOpeningSummaryMenu();

	private void OnDevKingdomOpeningSummaryMenuSelected(List<InquiryElement> selected)
		=> EventEditor.OnDevKingdomOpeningSummaryMenuSelected(selected);

	private void OpenDevEditKingdomOpeningSummary(Kingdom kingdom)
		=> EventEditor.OpenDevEditKingdomOpeningSummary(kingdom);

	private void ConfirmClearAllEventOpeningSummaries()
		=> EventEditor.ConfirmClearAllEventOpeningSummaries();

	private void OpenDevKingdomStabilityLabMenu()
		=> KingdomStabilityLab.OpenDevKingdomStabilityLabMenu();

	private string BuildDevKingdomStabilityLabel(Kingdom kingdom)
		=> KingdomStabilityLab.BuildDevKingdomStabilityLabel(kingdom);

	private string BuildDevKingdomStabilityDetailText(Kingdom kingdom)
		=> KingdomStabilityLab.BuildDevKingdomStabilityDetailText(kingdom);

	private void OpenDevKingdomStabilityDetailMenu(Kingdom kingdom)
		=> KingdomStabilityLab.OpenDevKingdomStabilityDetailMenu(kingdom);

	private void OpenDevEditKingdomStability(Kingdom kingdom)
		=> KingdomStabilityLab.OpenDevEditKingdomStability(kingdom);

	private static string BuildKingdomRebellionResolutionText(KingdomRebellionResolutionResult result)
		=> EventEditorProjection.BuildKingdomRebellionResolutionText(EventEditorDisplay, result);

	private void RunDevKingdomRebellionTest(Kingdom kingdom)
		=> KingdomStabilityLab.RunDevKingdomRebellionTest(kingdom);

	private void ConfirmForceDevKingdomRebellion(Kingdom kingdom)
		=> KingdomStabilityLab.ConfirmForceDevKingdomRebellion(kingdom);

	private static List<Kingdom> GetDevEditableKingdoms()
		=> EventEditorProjection.GetDevEditableKingdoms();

	private string BuildDevKingdomSummaryLabel(Kingdom kingdom)
		=> EventEditor.BuildDevKingdomSummaryLabel(kingdom);

	private static string BuildDevSummaryPreview(string text, int maxLen)
		=> EventEditorProjection.BuildDevSummaryPreview(text, maxLen);

	private void OpenDevEventViewerMenu(int page)
		=> EventEditor.OpenDevEventViewerMenu(page);

	private string BuildDevEventViewerDescription(List<EventRecordEntry> entries, int page, int totalPages)
		=> EventEditor.BuildDevEventViewerDescription(entries, page, totalPages);

	private static string BuildDevEventRecordItemLabel(EventRecordEntry entry)
		=> EventEditorProjection.BuildDevEventRecordItemLabel(EventEditorDisplay, entry);

	private static string BuildDevEventRecordMenuLabel(EventRecordEntry entry)
		=> EventEditorProjection.BuildDevEventRecordMenuLabel(EventEditorDisplay, entry);

	private void OpenDevEventRecordDetail(EventRecordEntry entry, int returnPage)
		=> EventEditor.OpenDevEventRecordDetail(entry, returnPage);

	private string BuildDevEventRecordDetailText(EventRecordEntry entry)
		=> EventEditor.BuildDevEventRecordDetailText(entry);

	private string BuildDevEventRecordCompactDetailText(EventRecordEntry entry)
		=> EventEditor.BuildDevEventRecordCompactDetailText(entry);

	private void OpenDevEventReportDetail(EventRecordEntry entry, int returnPage)
		=> EventEditor.OpenDevEventReportDetail(entry, returnPage);

	private void OpenDevEventShortSummaryDetail(EventRecordEntry entry, int returnPage)
		=> EventEditor.OpenDevEventShortSummaryDetail(entry, returnPage);

	private void OpenDevEventTagDetail(EventRecordEntry entry, int returnPage)
		=> EventEditor.OpenDevEventTagDetail(entry, returnPage);

	private void OpenDevEventMaterialList(EventRecordEntry entry, int returnPage, int page)
		=> EventEditor.OpenDevEventMaterialList(entry, returnPage, page);

	private string BuildDevEventMaterialListText(EventRecordEntry entry, int page, int totalPages)
		=> EventEditor.BuildDevEventMaterialListText(entry, page, totalPages);

	private void OpenDevEditEventRecordTitle(EventRecordEntry entry, int returnPage)
		=> EventEditor.OpenDevEditEventRecordTitle(entry, returnPage);

	private void OpenDevEditEventRecordReport(EventRecordEntry entry, int returnPage)
		=> EventEditor.OpenDevEditEventRecordReport(entry, returnPage);

	private void OpenDevEventPromptDetail(EventRecordEntry entry, int returnPage)
		=> EventEditor.OpenDevEventPromptDetail(entry, returnPage);

	private static string BuildDevEventMaterialItemLabel(EventMaterialReference material)
		=> EventEditorProjection.BuildDevEventMaterialItemLabel(EventEditorDisplay, material);

	private void OpenDevEventMaterialDetail(EventRecordEntry entry, EventMaterialReference material, int returnPage, int returnMaterialPage)
		=> EventEditor.OpenDevEventMaterialDetail(entry, material, returnPage, returnMaterialPage);

}
