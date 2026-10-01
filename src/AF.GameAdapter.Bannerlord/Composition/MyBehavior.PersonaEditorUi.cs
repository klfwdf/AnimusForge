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
 private DeveloperEditorSession _developerEditorSession;
 private DeveloperEditorSession EditorSession
 {
  get
  {
   var owner = _developerEditorSession ?? (_developerEditorSession = new DeveloperEditorSession());
   owner.SynchronizeGeneration(SaveRuntimeGuard.CaptureGeneration());
   return owner;
  }
 }

 private PersonaEditorController _personaEditor;
 private PersonaEditorController CreatePersonaEditor() => new PersonaEditorController(new PersonaEditorPort
 {
  GetEditableHeroes = () => _devEditableHeroes, SetEditableHeroes = heroes => _devEditableHeroes = heroes,
  VoiceMappingStorage = () => _voiceMappingJsonStorage,
  OpenExportFolderPicker = (title, scope, onReturn) => OpenExportFolderPicker(title, scope, onReturn),
  OpenImportFolderPicker = (title, scope, onReturn) => OpenImportFolderPicker(title, scope, onReturn),
  OpenFolderPicker = (title, isExport, scope, onReturn, heroId) => OpenFolderPicker(title, isExport, scope, onReturn, heroId),
  OpenFolderPickerWithCallback = OpenFolderPickerWithCallback,
  BuildDevEditableHeroList = BuildDevEditableHeroList,
  GetKnowledgeRuleIdsFromImportFolderForDev = GetKnowledgeRuleIdsFromImportFolderForDev,
  GetUnnamedPersonaKeysFromImportFolderForDev = GetUnnamedPersonaKeysFromImportFolderForDev,
  ExportSingleKnowledgeRuleData = ExportSingleKnowledgeRuleData,
  ImportSingleKnowledgeRuleData = ImportSingleKnowledgeRuleData,
  ExportSingleUnnamedPersonaData = ExportSingleUnnamedPersonaData,
  ImportSingleUnnamedPersonaData = ImportSingleUnnamedPersonaData,
  OpenDevHeroNpcMenu = OpenDevHeroNpcMenu,
  CaptureGeneration = SaveRuntimeGuard.CaptureGeneration, IsCurrent = IsMemorySourceEditorCurrent,
  GetSelectedHero = () => _devEditingHero, SetSelectedHero = hero => _devEditingHero = hero,
  GetNpcPersonaStrings = GetNpcPersonaStrings, GetNpcVoiceId = GetNpcVoiceId,
  ShowDevEditInquiry = ShowDevEditInquiry,
  ClearPersona = hero => SaveNpcPersonaProfile(hero, new NpcPersonaProfile()),
  SavePersonaText = SaveDevPersonaEditorText, SaveVoice = SaveDevPersonaEditorVoice,
  GeneratePersona = hero => GenerateNpcPersonaAsync(hero, ignoreRetryCooldown: true, overwriteExisting: true),
  CompleteOnMainThread = RunMemorySummaryCompletionAsync
 });
 private PersonaEditorController PersonaEditor
 {
  get
  {
   var owner = _personaEditor ?? (_personaEditor = CreatePersonaEditor());
   owner.SynchronizeGeneration(SaveRuntimeGuard.CaptureGeneration());
   return owner;
  }
 }
 private void SaveDevPersonaEditorText(Hero hero, string personality, string background)
 {
  NpcPersonaProfile profile = GetNpcPersonaProfile(hero, createIfMissing: true) ?? new NpcPersonaProfile();
  profile.Personality = personality; profile.Background = background;
  SaveNpcPersonaProfile(hero, profile);
 }
 private void SaveDevPersonaEditorVoice(Hero hero, string voice)
 {
  NpcPersonaProfile profile = GetNpcPersonaProfile(hero, createIfMissing: true) ?? new NpcPersonaProfile();
  profile.VoiceId = voice;
  SaveNpcPersonaProfile(hero, profile);
 }

	private Action _devPersonaReturnAction { get => PersonaEditor.PersonaReturnAction; set => PersonaEditor.PersonaReturnAction = value; }

	private Action _devNpcEditorReturnAction { get => PersonaEditor.NpcEditorReturnAction; set => PersonaEditor.NpcEditorReturnAction = value; }

	private string _devOpsHeroId { get => PersonaEditor.OpsHeroId; set => PersonaEditor.OpsHeroId = value; }

	private string _devOpsHeroName { get => PersonaEditor.OpsHeroName; set => PersonaEditor.OpsHeroName = value; }

	private bool _devPendingKnowledgeSingleImportPicked { get => PersonaEditor.KnowledgeImportPicked; set => PersonaEditor.KnowledgeImportPicked = value; }

	private string _devPendingKnowledgeSingleImportFolderName { get => PersonaEditor.KnowledgeImportFolder; set => PersonaEditor.KnowledgeImportFolder = value; }

	private bool _devPendingUnnamedSingleImportPicked { get => PersonaEditor.UnnamedImportPicked; set => PersonaEditor.UnnamedImportPicked = value; }

	private string _devPendingUnnamedSingleImportFolderName { get => PersonaEditor.UnnamedImportFolder; set => PersonaEditor.UnnamedImportFolder = value; }

	private string _devSingleNpcSelectionQuery { get => PersonaEditor.SingleNpcQuery; set => PersonaEditor.SingleNpcQuery = value; }

	private int _devSingleNpcSelectionPage { get => PersonaEditor.SingleNpcPage; set => PersonaEditor.SingleNpcPage = value; }

	private void OpenDevPersonaMenu(Hero npc)
		=> PersonaEditor.OpenDevPersonaMenu(npc);

	private void OpenDevPersonaMenuFromTown(Hero npc)
		=> PersonaEditor.OpenDevPersonaMenuFromTown(npc);

	private void OpenDevPersonaMenuFromExternal(Hero npc)
		=> PersonaEditor.OpenDevPersonaMenuFromExternal(npc);

	private void OpenDevPersonaMenuFromExternal(Hero npc, Action onFinished)
		=> PersonaEditor.OpenDevPersonaMenuFromExternal(npc, onFinished);

	private void OpenDevPersonaMenuInternal(Hero npc)
		=> PersonaEditor.OpenDevPersonaMenuInternal(npc);

	private void ReturnFromDevPersonaMenu(Hero npc)
		=> PersonaEditor.ReturnFromDevPersonaMenu(npc);

	private void OnDevPersonaMenuSelected(List<InquiryElement> selected)
		=> PersonaEditor.OnDevPersonaMenuSelected(selected);

	private void OpenDevRerollPersonaConfirmation(Hero npc)
		=> PersonaEditor.OpenDevRerollPersonaConfirmation(npc);

	private void OpenHeroPersonaRerollConfirmation(Hero npc, Action onClosed)
		=> PersonaEditor.OpenHeroPersonaRerollConfirmation(npc, onClosed);

	private Task RunHeroPersonaRerollAsync(Hero npc, Action onClosed)
		=> PersonaEditor.RunHeroPersonaRerollAsync(npc, onClosed);

	private static void InvokePersonaRerollClosed(Action onClosed)
		=> PersonaEditorController.InvokePersonaRerollClosed(onClosed);

	private void OpenDevSetPersonality(Hero npc)
		=> PersonaEditor.OpenDevSetPersonality(npc);

	private void OpenDevSetVoiceId(Hero npc)
		=> PersonaEditor.OpenDevSetVoiceId(npc);

	private void OpenDevSetBackground(Hero npc)
		=> PersonaEditor.OpenDevSetBackground(npc);

	private void OpenDevKnowledgeMenu()
		=> PersonaEditor.OpenDevKnowledgeMenu();

	private void OnDevKnowledgeMenuSelected(List<InquiryElement> selected)
		=> PersonaEditor.OnDevKnowledgeMenuSelected(selected);

	private void OpenDevKnowledgeSingleExportSelection()
		=> PersonaEditor.OpenDevKnowledgeSingleExportSelection();

	private void OpenDevKnowledgeSingleImportSelection(string folderName)
		=> PersonaEditor.OpenDevKnowledgeSingleImportSelection(folderName);

	private void OpenDevUnnamedPersonaMenu()
		=> PersonaEditor.OpenDevUnnamedPersonaMenu();

	private void OnDevUnnamedPersonaMenuSelected(List<InquiryElement> selected)
		=> PersonaEditor.OnDevUnnamedPersonaMenuSelected(selected);

	private void OpenDevUnnamedPersonaSingleExportSelection()
		=> PersonaEditor.OpenDevUnnamedPersonaSingleExportSelection();

	private void OpenDevUnnamedPersonaSingleImportSelection(string folderName)
		=> PersonaEditor.OpenDevUnnamedPersonaSingleImportSelection(folderName);

	private void OpenDevUnnamedPersonaIndexSelection()
		=> PersonaEditor.OpenDevUnnamedPersonaIndexSelection();

	private void OnDevUnnamedPersonaIndexSelected(List<InquiryElement> selected)
		=> PersonaEditor.OnDevUnnamedPersonaIndexSelected(selected);

	private void OpenDevUnnamedPersonaEdit(string key)
		=> PersonaEditor.OpenDevUnnamedPersonaEdit(key);

	private void OpenDevSingleNpcHeroSelection()
		=> PersonaEditor.OpenDevSingleNpcHeroSelection();

	private void OpenDevSingleNpcHeroSelectionPaged(int page, string query)
		=> PersonaEditor.OpenDevSingleNpcHeroSelectionPaged(page, query);

	private void OpenDevSingleNpcHeroSelectionFromExportFolder(string folderName)
		=> PersonaEditor.OpenDevSingleNpcHeroSelectionFromExportFolder(folderName);

	private void OnDevSingleNpcHeroSelected(List<InquiryElement> selected)
		=> PersonaEditor.OnDevSingleNpcHeroSelected(selected);

	private void OpenDevSingleNpcOpsMenu()
		=> PersonaEditor.OpenDevSingleNpcOpsMenu();

	private void OnDevSingleNpcOpsSelected(List<InquiryElement> selected)
		=> PersonaEditor.OnDevSingleNpcOpsSelected(selected);

	private static string GetVoiceGroupShortName(string key)
		=> PersonaEditorController.GetVoiceGroupShortName(key);

	private void OpenDevVoiceMappingMenu()
		=> PersonaEditor.OpenDevVoiceMappingMenu();

	private void OnDevVoiceMappingMenuSelected(List<InquiryElement> selected)
		=> PersonaEditor.OnDevVoiceMappingMenuSelected(selected);

	private void OpenDevVoiceMappingSelectGroup(bool isAdd)
		=> PersonaEditor.OpenDevVoiceMappingSelectGroup(isAdd);

	private void OpenDevVoiceMappingAddVoice(string groupKey)
		=> PersonaEditor.OpenDevVoiceMappingAddVoice(groupKey);

	private void OpenDevVoiceMappingRemoveVoice(string groupKey)
		=> PersonaEditor.OpenDevVoiceMappingRemoveVoice(groupKey);

	private void OpenDevVoiceMappingSetFallback()
		=> PersonaEditor.OpenDevVoiceMappingSetFallback();

}
