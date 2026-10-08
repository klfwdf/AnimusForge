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
 private MemoryEditorController CreateMemoryEditor() => new MemoryEditorController(new MemoryEditorPort
 {
  GetSelectedHero = () => _devEditingHero, SetSelectedHero = hero => _devEditingHero = hero,
  SummaryJobs = () => _memorySummaryQueue, OverviewJobs = () => _memoryOverviewQueue,
  ShowDevEditInquiry = ShowDevEditInquiry,
  GetMemoryHeroId = GetMemoryHeroId,
  GetMemoryOverviewState = GetMemoryOverviewState,
  LoadDailyMemoryDrafts = LoadDailyMemoryDrafts,
  LoadCompressedMemoryBlocks = LoadCompressedMemoryBlocks,
  LoadDialogueHistory = LoadDialogueHistory,
  SanitizeDailyMemoryDrafts = SanitizeDailyMemoryDrafts,
  SanitizeCompressedMemoryBlocks = SanitizeCompressedMemoryBlocks,
  NormalizeMemoryHeroId = NormalizeMemoryHeroId,
  TrySealPastDailyMemoryDrafts = TrySealPastDailyMemoryDrafts,
  QueueAllMemoryOverviewCandidatesForDeferredScan = QueueAllMemoryOverviewCandidatesForDeferredScan,
  ProcessMemoryOverviewCandidateScanBudget = ProcessMemoryOverviewCandidateScanBudget,
  TryStartMemorySummaryQueue = TryStartMemorySummaryQueue,
  FindDevDailyMemoryDraft = FindDevDailyMemoryDraft,
  FindDevDailyMemoryLine = FindDevDailyMemoryLine,
  ComputeMemorySummaryFingerprint = ComputeMemorySummaryFingerprint,
  TryParseDevSelectionInt = TryParseDevSelectionInt,
  ShowDevLargeTextOrInquiry = ShowDevLargeTextOrInquiry,
  ShowDevLargeSelectionOrInquiry = ShowDevLargeSelectionOrInquiry,
  ShowDevLargeConfirmOrInquiry = ShowDevLargeConfirmOrInquiry,
  TryApplyDevCompressedMemoryBlockDataMutation = TryApplyDevCompressedMemoryBlockDataMutation,
  DeleteDevCompressedMemoryBlockData = DeleteDevCompressedMemoryBlockData,
  TryApplyDevDialogueHistoryLineDataMutation = TryApplyDevDialogueHistoryLineDataMutation,
  FormatMemoryHourRange = FormatMemoryHourRange,
  BuildDailyMemoryLineForPrompt = BuildDailyMemoryLineForPrompt,
  BuildDevHistoryPreview = BuildDevHistoryPreview,
  TryApplyDevDailyMemoryDraftDataMutation = TryApplyDevDailyMemoryDraftDataMutation,
  TryApplyDevDailyMemoryLineDataMutation = TryApplyDevDailyMemoryLineDataMutation,
  DeleteDevDailyMemoryDraftData = DeleteDevDailyMemoryDraftData,
  ClearDevCompressedMemoryData = ClearDevCompressedMemoryData,
  ClearDevDialogueHistoryData = ClearDevDialogueHistoryData,
  IsMemorySourceEditorCurrent = IsMemorySourceEditorCurrent,
  SaveDevMemoryOverviewCore = SaveDevMemoryOverviewCore,
 }, MemoryEditorDisplay);

 private MemoryEditorController _memoryEditor;
 private MemoryEditorController MemoryEditor
 {
  get
  {
   var owner = _memoryEditor ?? (_memoryEditor = CreateMemoryEditor());
   owner.SynchronizeGeneration(SaveRuntimeGuard.CaptureGeneration());
   return owner;
  }
 }

 private static readonly MemoryEditorDisplayPort MemoryEditorDisplay = new MemoryEditorDisplayPort
 {
  BuildDailyMemoryLineForPrompt = BuildDailyMemoryLineForPrompt,
  SanitizeWeeklyMemoryMaterialTriggers = SanitizeWeeklyMemoryMaterialTriggers,
  BuildWeeklyMemoryMaterialTagLabel = BuildWeeklyMemoryMaterialTagLabel,
  BuildCompressedMemoryBlockId = BuildCompressedMemoryBlockId,
  FormatMemoryHourRange = FormatMemoryHourRange,
  BuildDevHistoryPreview = BuildDevHistoryPreview,
  GetCurrentGameDayIndexSafe = GetCurrentGameDayIndexSafe,
  GetCurrentHourOfDaySafeForPrompt = GetCurrentHourOfDaySafeForPrompt,
  ResolveCurrentMemorySceneLabel = ResolveCurrentMemorySceneLabel
 };

	private string _devHistorySearchQuery { get => MemoryEditor.HistoryQuery; set => MemoryEditor.HistoryQuery = value; }

	private string _devDailyMemorySearchQuery { get => MemoryEditor.DailyQuery; set => MemoryEditor.DailyQuery = value; }

	private string _devCompressedMemorySearchQuery { get => MemoryEditor.CompressedQuery; set => MemoryEditor.CompressedQuery = value; }

	private int _devDailyMemoryDraftPage { get => MemoryEditor.DailyPage; set => MemoryEditor.DailyPage = value; }

	private int _devCompressedMemoryBlockPage { get => MemoryEditor.CompressedPage; set => MemoryEditor.CompressedPage = value; }

	private void OpenDevEditLine(Hero npc, int dayIndex, int lineIndex)
		=> MemoryEditor.OpenDevEditLine(npc, dayIndex, lineIndex);

	private void OpenDevEditLineInput(Hero npc, int dayIndex, int lineIndex, string currentValue, string displayDate)
		=> MemoryEditor.OpenDevEditLineInput(npc, dayIndex, lineIndex, currentValue, displayDate);

	private void OpenDevCompressedMemoryMenu(Hero npc)
		=> MemoryEditor.OpenDevCompressedMemoryMenu(npc);

	private void OnDevCompressedMemoryMenuSelected(List<InquiryElement> selected)
		=> MemoryEditor.OnDevCompressedMemoryMenuSelected(selected);

	private void ShowDevCompressedMemoryText(Hero npc, string title, string text)
		=> MemoryEditor.ShowDevCompressedMemoryText(npc, title, text);

	private void OpenDevDailyMemoryDraftList(Hero npc, int page, string query)
		=> MemoryEditor.OpenDevDailyMemoryDraftList(npc, page, query);

	private void OpenDevDailyMemoryDraftEditor(Hero npc, int dayIndex, int returnPage, string returnQuery)
		=> MemoryEditor.OpenDevDailyMemoryDraftEditor(npc, dayIndex, returnPage, returnQuery);

	private void OpenDevDailyMemoryLineList(Hero npc, int dayIndex, int returnPage, string returnQuery)
		=> MemoryEditor.OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);

	private void OpenDevDailyMemoryLineEditor(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
		=> MemoryEditor.OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);

	private void OpenDevDailyMemoryLineTextEditor(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
		=> MemoryEditor.OpenDevDailyMemoryLineTextEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);

	private void OpenDevDailyMemoryLineSpeakerEditor(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
		=> MemoryEditor.OpenDevDailyMemoryLineSpeakerEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);

	private void OpenDevDailyMemoryLineSceneEditor(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
		=> MemoryEditor.OpenDevDailyMemoryLineSceneEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);

	private void OpenDevDailyMemoryLineHourEditor(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
		=> MemoryEditor.OpenDevDailyMemoryLineHourEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);

	private void ToggleDevDailyMemoryLineAfef(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
		=> MemoryEditor.ToggleDevDailyMemoryLineAfef(npc, dayIndex, lineIndex, returnPage, returnQuery);

	private void ToggleDevDailyMemoryLineLlm(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
		=> MemoryEditor.ToggleDevDailyMemoryLineLlm(npc, dayIndex, lineIndex, returnPage, returnQuery);

	private void OpenDevAddDailyMemoryLine(Hero npc, int dayIndex, bool isAfef, int returnPage, string returnQuery)
		=> MemoryEditor.OpenDevAddDailyMemoryLine(npc, dayIndex, isAfef, returnPage, returnQuery);

	private void ConfirmDevDeleteDailyMemoryLine(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
		=> MemoryEditor.ConfirmDevDeleteDailyMemoryLine(npc, dayIndex, lineIndex, returnPage, returnQuery);

	private void ConfirmDevDeleteDailyMemoryDraft(Hero npc, int dayIndex, int returnPage, string returnQuery)
		=> MemoryEditor.ConfirmDevDeleteDailyMemoryDraft(npc, dayIndex, returnPage, returnQuery);

	private static bool IsDevDailyMemoryDraftMatch(DailyMemoryDraft draft, string[] terms)
		=> MemoryEditorProjection.IsDevDailyMemoryDraftMatch(MemoryEditorDisplay, draft, terms);

	private static string BuildDevDailyMemoryDraftSearchText(DailyMemoryDraft draft)
		=> MemoryEditorProjection.BuildDevDailyMemoryDraftSearchText(MemoryEditorDisplay, draft);

	private static string BuildDevDailyMemoryDraftListLabel(DailyMemoryDraft draft)
		=> MemoryEditorProjection.BuildDevDailyMemoryDraftListLabel(MemoryEditorDisplay, draft);

	private static string BuildDevDailyMemoryDraftSubtitle(DailyMemoryDraft draft)
		=> MemoryEditorProjection.BuildDevDailyMemoryDraftSubtitle(MemoryEditorDisplay, draft);

	private static string BuildDevStoredErrorReference(string error)
		=> MemoryEditorProjection.BuildDevStoredErrorReference(MemoryEditorDisplay, error);

	private static string BuildDevDailyMemoryDraftEditorDescription(DailyMemoryDraft draft)
		=> MemoryEditorProjection.BuildDevDailyMemoryDraftEditorDescription(MemoryEditorDisplay, draft);

	private static string BuildDevWeeklyMemoryMaterialTriggerText(IEnumerable<WeeklyMemoryMaterialTrigger> triggers)
		=> MemoryEditorProjection.BuildDevWeeklyMemoryMaterialTriggerText(MemoryEditorDisplay, triggers);

	private static string BuildDevDailyMemoryLineListLabel(DailyMemoryLine line)
		=> MemoryEditorProjection.BuildDevDailyMemoryLineListLabel(MemoryEditorDisplay, line);

	private static string BuildDevDailyMemoryLineSubtitle(DailyMemoryLine line)
		=> MemoryEditorProjection.BuildDevDailyMemoryLineSubtitle(MemoryEditorDisplay, line);

	private static string BuildDevDailyMemoryLineDescription(DailyMemoryLine line)
		=> MemoryEditorProjection.BuildDevDailyMemoryLineDescription(MemoryEditorDisplay, line);

	private static int GetDefaultDevDailyMemoryLineHour(DailyMemoryDraft draft)
		=> MemoryEditorProjection.GetDefaultDevDailyMemoryLineHour(MemoryEditorDisplay, draft);

	private static string GetDefaultDevDailyMemoryLineScene(DailyMemoryDraft draft)
		=> MemoryEditorProjection.GetDefaultDevDailyMemoryLineScene(MemoryEditorDisplay, draft);

	private void OpenDevMemoryOverviewEditor(Hero npc)
		=> MemoryEditor.OpenDevMemoryOverviewEditor(npc);

	private void ApplyDevMemoryOverviewInput(Hero npc, string input)
		=> MemoryEditor.ApplyDevMemoryOverviewInput(npc, input);

	private void OpenDevCompressedMemoryBlockList(Hero npc, int page, string query)
		=> MemoryEditor.OpenDevCompressedMemoryBlockList(npc, page, query);

	private void OpenDevCompressedMemoryBlockEditor(Hero npc, string blockId, int returnPage, string returnQuery)
		=> MemoryEditor.OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);

	private void OpenDevCompressedMemoryBlockTitleEditor(Hero npc, string blockId, int returnPage, string returnQuery)
		=> MemoryEditor.OpenDevCompressedMemoryBlockTitleEditor(npc, blockId, returnPage, returnQuery);

	private void OpenDevCompressedMemoryBlockSummaryEditor(Hero npc, string blockId, int returnPage, string returnQuery)
		=> MemoryEditor.OpenDevCompressedMemoryBlockSummaryEditor(npc, blockId, returnPage, returnQuery);

	private void OpenDevCompressedMemoryBlockScenesEditor(Hero npc, string blockId, int returnPage, string returnQuery)
		=> MemoryEditor.OpenDevCompressedMemoryBlockScenesEditor(npc, blockId, returnPage, returnQuery);

	private void OpenDevCompressedMemoryBlockAfefEditor(Hero npc, string blockId, int returnPage, string returnQuery)
		=> MemoryEditor.OpenDevCompressedMemoryBlockAfefEditor(npc, blockId, returnPage, returnQuery);

	private void ConfirmDevDeleteCompressedMemoryBlock(Hero npc, string blockId, int returnPage, string returnQuery)
		=> MemoryEditor.ConfirmDevDeleteCompressedMemoryBlock(npc, blockId, returnPage, returnQuery);

	private static CompressedMemoryBlock FindDevCompressedMemoryBlock(List<CompressedMemoryBlock> blocks, string blockId)
		=> MemoryEditorProjection.FindDevCompressedMemoryBlock(MemoryEditorDisplay, blocks, blockId);

	private static string GetDevCompressedMemoryBlockId(CompressedMemoryBlock block)
		=> MemoryEditorProjection.GetDevCompressedMemoryBlockId(MemoryEditorDisplay, block);

	private static string BuildDevCompressedMemoryBlockListLabel(CompressedMemoryBlock block, int displayIndex)
		=> MemoryEditorProjection.BuildDevCompressedMemoryBlockListLabel(MemoryEditorDisplay, block, displayIndex);

	private static string BuildDevCompressedMemoryBlockSubtitle(CompressedMemoryBlock block)
		=> MemoryEditorProjection.BuildDevCompressedMemoryBlockSubtitle(MemoryEditorDisplay, block);

	private static string BuildDevCompressedMemoryBlockEditorBody(CompressedMemoryBlock block)
		=> MemoryEditorProjection.BuildDevCompressedMemoryBlockEditorBody(MemoryEditorDisplay, block);

	private static string[] SplitDevCompressedMemorySearchTerms(string query)
		=> MemoryEditorProjection.SplitDevCompressedMemorySearchTerms(MemoryEditorDisplay, query);

	private static bool IsDevCompressedMemoryBlockMatch(CompressedMemoryBlock block, string[] terms)
		=> MemoryEditorProjection.IsDevCompressedMemoryBlockMatch(MemoryEditorDisplay, block, terms);

	private static string NormalizeDevCompressedMemoryMultilineInput(string input)
		=> MemoryEditorProjection.NormalizeDevCompressedMemoryMultilineInput(MemoryEditorDisplay, input);

	private static List<string> ParseDevCompressedMemoryLineList(string input, int maxCount, bool ignoreCase)
		=> MemoryEditorProjection.ParseDevCompressedMemoryLineList(MemoryEditorDisplay, input, maxCount, ignoreCase);

	private string BuildDevCompressedMemoryRawText(Hero npc)
		=> MemoryEditor.BuildDevCompressedMemoryRawText(npc);

	private string BuildDevCompressedMemoryBlockText(Hero npc)
		=> MemoryEditor.BuildDevCompressedMemoryBlockText(npc);

	private string BuildDevCompressedMemoryQueueText(Hero npc)
		=> MemoryEditor.BuildDevCompressedMemoryQueueText(npc);

	private string BuildDevMemoryOverviewText(Hero npc)
		=> MemoryEditor.BuildDevMemoryOverviewText(npc);

	private void ConfirmDevClearCompressedMemory(Hero npc)
		=> MemoryEditor.ConfirmDevClearCompressedMemory(npc);

	private void OpenDevHistorySearchInput(Hero npc)
		=> MemoryEditor.OpenDevHistorySearchInput(npc);

	private void ConfirmDevClearAllDialogueHistory(Hero npc)
		=> MemoryEditor.ConfirmDevClearAllDialogueHistory(npc);

	private void OpenDevHistoryDateSelection(Hero npc)
		=> MemoryEditor.OpenDevHistoryDateSelection(npc);

	private void OnDevHistoryDateSelected(List<InquiryElement> selected)
		=> MemoryEditor.OnDevHistoryDateSelected(selected);

	private void OpenDevHistoryLineSelection(Hero npc, int dayIndex)
		=> MemoryEditor.OpenDevHistoryLineSelection(npc, dayIndex);

	private void OnDevHistoryLineSelected(List<InquiryElement> selected)
		=> MemoryEditor.OnDevHistoryLineSelected(selected);

	private void ApplyDevDailyMemoryLineMutation(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery, Action<DailyMemoryDraft, DailyMemoryLine> mutate, string successMessage)
		=> MemoryEditor.ApplyDevDailyMemoryLineMutation(npc, dayIndex, lineIndex, returnPage, returnQuery, mutate, successMessage);

	private void ApplyDevDailyMemoryDraftMutation(Hero npc, int dayIndex, Action<DailyMemoryDraft> mutate, string reason, string successMessage, int returnPage, string returnQuery, bool returnToDraftEditor)
		=> MemoryEditor.ApplyDevDailyMemoryDraftMutation(npc, dayIndex, mutate, reason, successMessage, returnPage, returnQuery, returnToDraftEditor);


 private int SaveDevMemoryOverviewCore(Hero npc, string summary)
  => MemoryDeveloperEditOwner.SaveOverviewForAuthority(() => GetMemoryHeroId(npc), () => npc.Name?.ToString(), summary,
   () => LoadCompressedMemoryBlocks(npc), () => DateTime.UtcNow.Ticks, _memoryBusinessState,
   blocks => TryEnqueueMemoryOverviewForHero(npc,blocks));
 private void ApplyDevEditLineInput(Hero npc, int dayIndex, int lineIndex, string input)
  => MemoryEditor.ApplyDevEditLineInput(npc, dayIndex, lineIndex, input, SaveRuntimeGuard.CaptureGeneration());
 private void DeleteDevCompressedMemoryBlock(Hero npc, string blockId, int returnPage, string returnQuery)
  => MemoryEditor.DeleteDevCompressedMemoryBlock(npc, blockId, returnPage, returnQuery, SaveRuntimeGuard.CaptureGeneration());
 private void ApplyDevCompressedMemoryBlockMutation(Hero npc, string blockId, int returnPage, string returnQuery, Action<CompressedMemoryBlock> mutate, string successMessage)
  => MemoryEditor.ApplyDevCompressedMemoryBlockMutation(npc, blockId, returnPage, returnQuery, mutate, successMessage, SaveRuntimeGuard.CaptureGeneration());
}
