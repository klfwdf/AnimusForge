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
 private DeveloperRootEditorController _developerRootEditor;
 private DeveloperRootEditorController DeveloperRootEditor
 {
  get { var editor=_developerRootEditor ?? (_developerRootEditor = CreateDeveloperRootEditor()); editor.SynchronizeGeneration(SaveRuntimeGuard.CaptureGeneration()); return editor; }
 }
 private DeveloperRootEditorController CreateDeveloperRootEditor() => new DeveloperRootEditorController(new DeveloperRootEditorPort
 {
  CaptureGeneration = SaveRuntimeGuard.CaptureGeneration, IsCurrent = IsMemorySourceEditorCurrent,
  GetSelectedHero=()=>EditorSession.SelectedHero, SetSelectedHero=hero=>EditorSession.SelectedHero=hero,
  GetEditableHeroes=()=>EditorSession.EditableHeroes, SetEditableHeroes=heroes=>EditorSession.EditableHeroes=heroes,
  GetNpcEditorReturnAction=()=>_devNpcEditorReturnAction, SetNpcEditorReturnAction=action=>_devNpcEditorReturnAction=action,
  SetHistoryQuery=query=>_devHistorySearchQuery=query,
  HistoryHeroIds=()=>_dialogueHistory?.Keys, PersonaHeroIds=()=>_npcPersonaProfiles?.Keys,
  CountPersonaProfiles=()=>_npcPersonaProfiles?.Count??0, CountDialogueHistory=()=>_dialogueHistory?.Count??0,
  CountEventRecords=()=>_eventRecordEntries!=null?SanitizeEventRecordEntries(_eventRecordEntries).Count:0, CountKingdomStability=()=>_kingdomStabilityValues?.Count??0,
  CountCompressedMemoryOwnersForDev=CountCompressedMemoryOwnersForDev,
  CountNpcActionOwnersForDev=CountNpcActionOwnersForDev,
  CountDebtOwnersForDev=CountDebtOwnersForDev,
  CountUnnamedPersonaForDev=CountUnnamedPersonaForDev,
  CountKnowledgeRulesForDev=CountKnowledgeRulesForDev,
  CountVoiceMappingForDev=CountVoiceMappingForDev,
  OpenDevSingleNpcHeroSelection=OpenDevSingleNpcHeroSelection,
  ClearAllDataForCurrentSave=ClearAllDataForCurrentSave,
  OpenDevCompressedMemoryMenu=OpenDevCompressedMemoryMenu,
  OpenDevPersonaMenuFromTown=OpenDevPersonaMenuFromTown,
  ReturnToDevRootMenu=ReturnToDevRootMenu,
  OpenExportFolderPicker=OpenExportFolderPicker,
  OpenImportFolderPicker=OpenImportFolderPicker,
  GetDevNpcActionEntries=GetDevNpcActionEntries,
 }, NpcActionEditorDisplay, DeveloperImport);
 private static readonly NpcActionEditorDisplayPort NpcActionEditorDisplay = new NpcActionEditorDisplayPort
 {
  ResolveDisplayNameBySettlementEntry = ResolveDisplayNameBySettlementEntry,
  ResolveHeroDisplay = ResolveHeroDisplay,
  ResolveClanDisplay = ResolveClanDisplay,
  ResolveKingdomDisplay = ResolveKingdomDisplay,
  HasStructuredNpcActionMetadata = HasStructuredNpcActionMetadata,
  BuildNpcActionActorNarrativeText = BuildNpcActionActorNarrativeText,
  TranslateNpcActionKindForPrompt = TranslateNpcActionKindForPrompt,
 };
	private string _devHeroSelectionQuery { get=>DeveloperRootEditor.HeroSelectionQuery; set=>DeveloperRootEditor.HeroSelectionQuery=value; }

	private int _devHeroSelectionPage { get=>DeveloperRootEditor.HeroSelectionPage; set=>DeveloperRootEditor.HeroSelectionPage=value; }

	private void OpenDevHeroNpcMenu()
		=> DeveloperRootEditor.OpenDevHeroNpcMenu();

	private void OnDevHeroNpcMenuSelected(List<InquiryElement> selected)
		=> DeveloperRootEditor.OnDevHeroNpcMenuSelected(selected);

	private void OpenDevAllDataMenu()
		=> DeveloperRootEditor.OpenDevAllDataMenu();

	private void OnDevAllDataMenuSelected(List<InquiryElement> selected)
		=> DeveloperRootEditor.OnDevAllDataMenuSelected(selected);

	private string BuildAllDataSummaryText()
		=> DeveloperRootEditor.BuildAllDataSummaryText();

	private void ConfirmClearAllData()
		=> DeveloperRootEditor.ConfirmClearAllData();

	private int CountCompressedMemoryOwnersForDev()
		=> DeveloperEditorDataProjection.CountNonEmptyMemoryOwners(_dailyMemoryDrafts,_compressedMemoryBlocks);

	private int CountNpcActionOwnersForDev()
		=> DeveloperEditorDataProjection.CountNonEmptyOwners(_npcMajorActions,_npcRecentActions);

	private int CountDebtOwnersForDev()
		=> DebtFiles.CountDebtOwnersForDev();

	private int CountUnnamedPersonaForDev()
		=> PersonaProfileFiles.CountUnnamedPersonaForDev();

	private int CountKnowledgeRulesForDev()
		=> KnowledgeFiles.CountKnowledgeRulesForDev();

	private int CountVoiceMappingForDev()
		=> VoiceFiles.CountVoiceMappingForDev();

	private void OpenDevTownEditorHeroSelection()
		=> DeveloperRootEditor.OpenDevTownEditorHeroSelection();

	private void OpenDevTownEditorHeroSelectionPaged(int page, string query)
		=> DeveloperRootEditor.OpenDevTownEditorHeroSelectionPaged(page, query);

	private List<Hero> BuildDevEditableHeroList()
		=> DeveloperRootEditor.BuildDevEditableHeroList();

	private void OnDevHeroSelected(List<InquiryElement> selected)
		=> DeveloperRootEditor.OnDevHeroSelected(selected);

	private void ShowDevEditInquiry(Hero npc)
		=> DeveloperRootEditor.ShowDevEditInquiry(npc);

	private void OpenDevNpcEditorFromExternal(Hero npc, Action onFinished)
		=> DeveloperRootEditor.OpenDevNpcEditorFromExternal(npc, onFinished);

	private void ReturnFromDevNpcEditor(Hero npc)
		=> DeveloperRootEditor.ReturnFromDevNpcEditor(npc);

	private void OpenDevSetDebtGoldSimple(Hero npc)
		=> DeveloperRootEditor.OpenDevSetDebtGoldSimple(npc);

	private void OpenDevDebtMenu(Hero npc)
		=> DeveloperRootEditor.OpenDevDebtMenu(npc);

	private void OnDevDebtMenuSelected(List<InquiryElement> selected)
		=> DeveloperRootEditor.OnDevDebtMenuSelected(selected);

	private void OnDevNpcMainMenuSelected(List<InquiryElement> selected)
		=> DeveloperRootEditor.OnDevNpcMainMenuSelected(selected);

	private void OpenDevNpcActionMenu(Hero npc, bool recentOnly, int page)
		=> DeveloperRootEditor.OpenDevNpcActionMenu(npc, recentOnly, page);

	private List<NpcActionEntry> GetDevNpcActionEntries(Hero npc, bool recentOnly) => _npcActionRecords.ReadEntries(_memoryBusinessState, GetNpcActionHeroKey(npc), recentOnly, GetCurrentGameDayIndexSafe);

	private string BuildDevNpcActionMenuDescription(Hero npc, bool recentOnly, int page, int totalPages, int currentCount, int majorCount)
		=> DeveloperRootEditor.BuildDevNpcActionMenuDescription(npc, recentOnly, page, totalPages, currentCount, majorCount);

	private static string BuildDevNpcActionItemLabel(NpcActionEntry entry)
		=> NpcActionEditorProjection.BuildDevNpcActionItemLabel(NpcActionEditorDisplay, entry);

	private void OpenDevNpcActionDetail(Hero npc, bool recentOnly, int page, NpcActionEntry entry)
		=> DeveloperRootEditor.OpenDevNpcActionDetail(npc, recentOnly, page, entry);

	private static string BuildDevNpcActionDetailSubtitle(NpcActionEntry entry)
		=> NpcActionEditorProjection.BuildDevNpcActionDetailSubtitle(NpcActionEditorDisplay, entry);

	private static string BuildDevNpcActionDetailText(NpcActionEntry entry)
		=> NpcActionEditorProjection.BuildDevNpcActionDetailText(NpcActionEditorDisplay, entry);

	private static string BuildDevNpcActionPreviewText(NpcActionEntry entry)
		=> NpcActionEditorProjection.BuildDevNpcActionPreviewText(NpcActionEditorDisplay, entry);

	private static string BuildDevNpcActionNarrative(NpcActionEntry entry)
		=> NpcActionEditorProjection.BuildDevNpcActionNarrative(NpcActionEditorDisplay, entry);

	private static string GetDevNpcActionKindDisplay(string actionKind)
		=> NpcActionEditorProjection.GetDevNpcActionKindDisplay(NpcActionEditorDisplay, actionKind);

	private static void AppendDevNpcActionField(StringBuilder stringBuilder, string label, string value)
		=> NpcActionEditorProjection.AppendDevNpcActionField(stringBuilder, label, value);

	private static string JoinDevActionIds(List<string> ids)
		=> NpcActionEditorProjection.JoinDevActionIds(ids);

	private static void ShowDevLargeTextOrInquiry(string title, string subtitle, string body, Action onClose, string closeText = "返回")
		=> DeveloperEditorDialogPresenter.ShowDevLargeTextOrInquiry(BuildDevHistoryPreview, title, subtitle, body, onClose, closeText);

	private static void ShowDevLargeSelectionOrInquiry(string title, string subtitle, string body, List<DevLargeSelectionPopup.Option> options, Action<string> onSelect, Action onCancel, string affirmativeText = "进入", string cancelText = "返回")
		=> DeveloperEditorDialogPresenter.ShowDevLargeSelectionOrInquiry(BuildDevHistoryPreview, title, subtitle, body, options, onSelect, onCancel, affirmativeText, cancelText);

	private static void ShowDevLargeConfirmOrInquiry(string title, string subtitle, string body, string confirmText, string cancelText, Action onConfirm, Action onCancel)
		=> DeveloperEditorDialogPresenter.ShowDevLargeConfirmOrInquiry(BuildDevHistoryPreview, title, subtitle, body, confirmText, cancelText, onConfirm, onCancel);

	private static string BuildDevLargeFallbackDescription(string subtitle, string body)
		=> DeveloperEditorDialogPresenter.BuildDevLargeFallbackDescription(subtitle, body);

	private static string BuildDevLargeFallbackOptionText(DevLargeSelectionPopup.Option option)
		=> DeveloperEditorDialogPresenter.BuildDevLargeFallbackOptionText(BuildDevHistoryPreview, option);

}
