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
 internal Func<bool> CaptureOnboardingImportGuard()
 {
  long generation = SaveRuntimeGuard.CaptureGeneration();
  return () => IsMemorySourceEditorCurrent(generation);
 }
 internal Action<OnboardingImportStep>[] CaptureOnboardingImports(string directory) => new Action<OnboardingImportStep>[]
 {
  step => PersonaProfileFiles.ImportPersonaDataScoped(directory, step),
  step => PersonaProfileFiles.ImportUnnamedPersonaDataScoped(directory, step),
  step => KnowledgeFiles.ImportKnowledgeDataScoped(directory, step),
  step => VoiceFiles.ImportVoiceMappingDataScoped(directory, step),
  step => WeeklyEventFiles.ImportEventDataScoped(directory, step)
 };
 private PersonaProfileImportExportAdapter _personaProfileFiles;
 private PersonaProfileImportExportAdapter PersonaProfileFiles => _personaProfileFiles ?? (_personaProfileFiles = new PersonaProfileImportExportAdapter(
  _personaProfiles, SaveRuntimeGuard.CaptureGeneration, IsMemorySourceEditorCurrent, ShowDuplicateImportInquiry));
 private MemoryHistoryImportExportAdapter _memoryHistoryFiles;
 private MemoryHistoryImportExportAdapter MemoryHistoryFiles => _memoryHistoryFiles ?? (_memoryHistoryFiles = new MemoryHistoryImportExportAdapter(
  _memoryBusinessState, SaveRuntimeGuard.CaptureGeneration, IsMemorySourceEditorCurrent,
  id => MemoryQueueState.MarkMemoryOverviewDirty(id), ShowDuplicateImportInquiry));
 private DebtImportExportAdapter _debtFiles;
 private DebtImportExportAdapter DebtFiles => _debtFiles ?? (_debtFiles = new DebtImportExportAdapter(ShowDuplicateImportInquiry));
 private VoicePersonaImportExportAdapter _voiceFiles;
 private VoicePersonaImportExportAdapter VoiceFiles => _voiceFiles ?? (_voiceFiles = new VoicePersonaImportExportAdapter(
  ShowDuplicateImportInquiry, json => _voiceMappingJsonStorage = json));
 private KnowledgeImportExportAdapter _knowledgeFiles;
 private KnowledgeImportExportAdapter KnowledgeFiles => _knowledgeFiles ?? (_knowledgeFiles = new KnowledgeImportExportAdapter(ShowDuplicateImportInquiry));
 private WeeklyEventImportExportAdapter _weeklyEventFiles;
 private WeeklyEventImportExportAdapter WeeklyEventFiles => _weeklyEventFiles ?? (_weeklyEventFiles = new WeeklyEventImportExportAdapter(
  _weeklyEventRecords, SanitizeEventRecordEntries, _weeklyReportMaterialRevisions.MarkOpening,
  BuildPublishedWorldWeeklyProductsFingerprint, NotifyWorldMessageWeeklyTimelineChanged,
  OpenDevEventEditorMenu, ShowDuplicateImportInquiry));
 private DeveloperPackageExportController _developerPackageExport;
 private DeveloperPackageExportController DeveloperPackageExport => _developerPackageExport ?? (_developerPackageExport = new DeveloperPackageExportController(
  PersonaProfileFiles, MemoryHistoryFiles, DebtFiles, VoiceFiles, KnowledgeFiles, WeeklyEventFiles));
 private DeveloperPackageImportController _developerPackageImport;
 private DeveloperPackageImportController DeveloperPackageImport => _developerPackageImport ?? (_developerPackageImport = new DeveloperPackageImportController(new DeveloperPackageImportPort
 {
 CaptureGeneration = SaveRuntimeGuard.CaptureGeneration, IsCurrent = IsMemorySourceEditorCurrent,
 HasPersonaAuthority = () => _npcPersonaProfiles != null, ContainsPersona = key => _npcPersonaProfiles.ContainsKey(key),
 HasHistoryAuthority = () => _dialogueHistory != null, ContainsHistory = key => _dialogueHistory.ContainsKey(key),
 HasKingdomSummaryAuthority = () => _eventKingdomOpeningSummaries != null, ContainsKingdomSummary = key => _eventKingdomOpeningSummaries.ContainsKey(key),
 WorldOpeningSummary = () => _eventWorldOpeningSummary, EventRecords = () => _eventRecordEntries,
 ReadUnnamedPersonaKey = ReadUnnamedPersonaImportKey,
 RefreshVoiceStorage = () => _voiceMappingJsonStorage = VoiceMapper.ExportMappingJson(pretty: false) ?? "",
 RefreshUnnamedStorage = () => _unnamedPersonaJsonStorage = ShoutUtils.ExportUnnamedPersonaStateJson(pretty: false) ?? "",
 ValidateUnnamedPersonaKeysForImport = ValidateUnnamedPersonaKeysForImport,
 ValidateKnowledgeKeywordsForImport = ValidateKnowledgeKeywordsForImport,
 TryResolveNpcDataFileHeroIdForImport = TryResolveNpcDataFileHeroIdForImport,
 StampNpcPersonaProfile = StampNpcPersonaProfile,
 NormalizeMemoryHeroId = NormalizeMemoryHeroId,
 HasCompressedMemoryDataForHero = HasCompressedMemoryDataForHero,
 ApplyImportedPersonaProfiles = ApplyImportedPersonaProfiles,
 ApplyImportedDialogueHistory = ApplyImportedDialogueHistory,
 ApplyCompressedMemoryExportBundle = ApplyCompressedMemoryExportBundle,
 TryLoadEventDataFromImportDir = TryLoadEventDataFromImportDir,
 ApplyImportedEventData = ApplyImportedEventData,
 ImportKnowledgeFromDir = ImportKnowledgeFromDir,
 ShowDuplicateImportInquiry = ShowDuplicateImportInquiry,
 }, DeveloperImport, PersonaProfileFiles, MemoryHistoryFiles, DebtFiles, KnowledgeFiles, VoiceFiles, WeeklyEventFiles));
 private DeveloperImportUiController _developerImportUi;
 private DeveloperImportUiController DeveloperImportUi => _developerImportUi ?? (_developerImportUi = new DeveloperImportUiController(new DeveloperImportUiPort
 {
  TryBuildDatabaseReloadPlan = TryBuildDatabaseReloadPlan, ApplyDatabaseReloadPlan = ApplyDatabaseReloadPlan,
  CaptureGeneration = SaveRuntimeGuard.CaptureGeneration, IsCurrent = IsMemorySourceEditorCurrent,
  ReturnToDevRootMenu = ReturnToDevRootMenu,
  ExportAllData = ExportAllData,
  ExportDebtData = ExportDebtData,
  ExportDialogueHistoryData = ExportDialogueHistoryData,
  ExportEventData = ExportEventData,
  ExportHeroNpcAllData = ExportHeroNpcAllData,
  ExportKnowledgeData = ExportKnowledgeData,
  ExportPersonaData = ExportPersonaData,
  ExportSingleNpcDebtData = ExportSingleNpcDebtData,
  ExportSingleNpcDialogueHistoryData = ExportSingleNpcDialogueHistoryData,
  ExportSingleNpcPersonaData = ExportSingleNpcPersonaData,
  ExportUnnamedPersonaData = ExportUnnamedPersonaData,
  ExportVoiceMappingData = ExportVoiceMappingData,
  ImportAllData = ImportAllData,
  ImportDebtData = ImportDebtData,
  ImportDialogueHistoryData = ImportDialogueHistoryData,
  ImportEventData = ImportEventData,
  ImportHeroNpcAllData = ImportHeroNpcAllData,
  ImportKnowledgeData = ImportKnowledgeData,
  ImportPersonaData = ImportPersonaData,
  ImportSingleNpcDebtData = ImportSingleNpcDebtData,
  ImportSingleNpcDialogueHistoryData = ImportSingleNpcDialogueHistoryData,
  ImportSingleNpcPersonaData = ImportSingleNpcPersonaData,
  ImportUnnamedPersonaData = ImportUnnamedPersonaData,
  ImportVoiceMappingData = ImportVoiceMappingData,
 }, DeveloperImport));
 private DeveloperImportController _developerImport;
 private DeveloperImportController DeveloperImport => _developerImport ?? (_developerImport = new DeveloperImportController(SaveRuntimeGuard.CaptureGeneration, IsMemorySourceEditorCurrent));

	private void OpenExportFolderPicker(string title, ExportImportScope scope)
		=> DeveloperImportUi.OpenExportFolderPicker(title, scope);

	private void OpenExportFolderPicker(string title, ExportImportScope scope, Action onReturn)
		=> DeveloperImportUi.OpenExportFolderPicker(title, scope, onReturn);

	private void OpenImportFolderPicker(string title, ExportImportScope scope)
		=> DeveloperImportUi.OpenImportFolderPicker(title, scope);

	private void OpenImportFolderPicker(string title, ExportImportScope scope, Action onReturn)
		=> DeveloperImportUi.OpenImportFolderPicker(title, scope, onReturn);

	private void OpenFolderPicker(string title, bool isExport, ExportImportScope scope)
		=> DeveloperImportUi.OpenFolderPicker(title, isExport, scope);

	private void OpenFolderPickerWithCallback(string title, bool isExport, Action<string> onSelectedFolder, Action onReturn)
		=> DeveloperImportUi.OpenFolderPickerWithCallback(title, isExport, onSelectedFolder, onReturn);

	private void OpenDatabaseReloadFolderPicker(Action onReturn)
		=> DeveloperImportUi.OpenDatabaseReloadFolderPicker(onReturn);

	private void BeginDatabaseReloadPreflight(string folderName, Action onReturn)
		=> DeveloperImportUi.BeginDatabaseReloadPreflight(folderName, onReturn);

	private void ShowDuplicateImportInquiry(string title, string text, Action onOverwrite, Action onSkipDuplicates, Action onCancel)
		=> DeveloperImportUi.ShowDuplicateImportInquiry(title, text, onOverwrite, onSkipDuplicates, onCancel);

	private static bool IsDirectoryNonEmpty(string dir)
		=> DeveloperImportUiController.IsDirectoryNonEmpty(dir);

	private void ShowOverwriteExportInquiry(string title, string text, Action onOverwrite, Action onNewFolder, Action onCancel)
		=> DeveloperImportUi.ShowOverwriteExportInquiry(title, text, onOverwrite, onNewFolder, onCancel);

	private void OpenFolderPicker(string title, bool isExport, ExportImportScope scope, Action onReturn, string heroId)
		=> DeveloperImportUi.OpenFolderPicker(title, isExport, scope, onReturn, heroId);

	private void ResolveAndRunExportImport(bool isExport, ExportImportScope scope, string folderName)
		=> DeveloperImportUi.ResolveAndRunExportImport(isExport, scope, folderName);

	private void ResolveAndRunExportImportForHero(bool isExport, ExportImportScope scope, string folderName, string heroId)
		=> DeveloperImportUi.ResolveAndRunExportImportForHero(isExport, scope, folderName, heroId);

	private void ExportSingleNpcPersonaData(string folderName, string heroId)
		=> PersonaProfileFiles.ExportSingleNpcPersonaData(folderName, heroId);

	private void ExportSingleNpcDialogueHistoryData(string folderName, string heroId)
		=> MemoryHistoryFiles.ExportSingleNpcDialogueHistoryData(folderName, heroId);

	private void ExportSingleNpcDebtData(string folderName, string heroId)
		=> DebtFiles.ExportSingleNpcDebtData(folderName, heroId);

	private void ImportSingleNpcDebtData(string folderName, string heroId)
		=> DebtFiles.ImportSingleNpcDebtData(folderName, heroId);

	private void ImportSingleNpcPersonaData(string folderName, string heroId)
		=> PersonaProfileFiles.ImportSingleNpcPersonaData(folderName, heroId);

	private void ImportSingleNpcDialogueHistoryData(string folderName, string heroId)
		=> MemoryHistoryFiles.ImportSingleNpcDialogueHistoryData(folderName, heroId);

	private void ExportHeroNpcAllData(string folderName)
		=> DeveloperPackageExport.ExportHeroNpcAllData(folderName);

	private void ImportHeroNpcAllData(string folderName)
		=> DeveloperPackageImport.ImportHeroNpcAllData(folderName);

	private void ExportAllData(string folderName)
		=> DeveloperPackageExport.ExportAllData(folderName);

	private void ExportUnnamedPersonaData(string folderName)
		=> PersonaProfileFiles.ExportUnnamedPersonaData(folderName);

	private void ExportPersonaData(string folderName)
		=> PersonaProfileFiles.ExportPersonaData(folderName);

	private void ExportDialogueHistoryData(string folderName)
		=> MemoryHistoryFiles.ExportDialogueHistoryData(folderName);

	private void ExportDebtData(string folderName)
		=> DebtFiles.ExportDebtData(folderName);

	private void ExportKnowledgeData(string folderName)
		=> KnowledgeFiles.ExportKnowledgeData(folderName);

	private void ExportEventData(string folderName)
		=> WeeklyEventFiles.ExportEventData(folderName);

	private void ExportEventDataToDir(string exportDir)
		=> WeeklyEventFiles.ExportEventDataToDir(exportDir);

	private Dictionary<string, string> BuildEventKingdomSummaryExportMap()
		=> WeeklyEventFiles.BuildEventKingdomSummaryExportMap();

	private void ExportKnowledgeToDir(string exportDir)
		=> KnowledgeFiles.ExportKnowledgeToDir(exportDir);

	private bool TryExportKnowledgeToDir(string exportDir, out int exportedCount, out string error)
		=> KnowledgeFiles.TryExportKnowledgeToDir(exportDir, out exportedCount, out error);

	private void ExportSingleKnowledgeRuleData(string folderName, string ruleId)
		=> KnowledgeFiles.ExportSingleKnowledgeRuleData(folderName, ruleId);

	private void ImportSingleKnowledgeRuleData(string folderName, string ruleId)
		=> KnowledgeFiles.ImportSingleKnowledgeRuleData(folderName, ruleId);

	private void ExportSingleUnnamedPersonaData(string folderName, string key)
		=> PersonaProfileFiles.ExportSingleUnnamedPersonaData(folderName, key);

	private void ImportSingleUnnamedPersonaData(string folderName, string key)
		=> PersonaProfileFiles.ImportSingleUnnamedPersonaData(folderName, key);

	private static string FindUnnamedPersonaJsonByKey(string dir, string key)
		=> PersonaProfileImportExportAdapter.FindUnnamedPersonaJsonByKey(dir, key);

	private List<string> GetKnowledgeRuleIdsFromImportFolderForDev(string folderName, int maxCount = 200)
		=> KnowledgeFiles.GetKnowledgeRuleIdsFromImportFolderForDev(folderName, maxCount);

	private List<string> GetUnnamedPersonaKeysFromImportFolderForDev(string folderName, int maxCount = 200)
		=> PersonaProfileFiles.GetUnnamedPersonaKeysFromImportFolderForDev(folderName, maxCount);

	private void ImportPersonaData(string folderName)
		=> PersonaProfileFiles.ImportPersonaData(folderName);

	private void ImportDialogueHistoryData(string folderName)
		=> MemoryHistoryFiles.ImportDialogueHistoryData(folderName);

	private void ImportDebtData(string folderName)
		=> DebtFiles.ImportDebtData(folderName);

	private void ImportEventData(string folderName)
		=> WeeklyEventFiles.ImportEventData(folderName);

	private void ImportUnnamedPersonaData(string folderName)
		=> PersonaProfileFiles.ImportUnnamedPersonaData(folderName);

	private void ImportKnowledgeData(string folderName)
		=> KnowledgeFiles.ImportKnowledgeData(folderName);

	private void ExportVoiceMappingData(string folderName)
		=> VoiceFiles.ExportVoiceMappingData(folderName);

	private void ImportVoiceMappingData(string folderName)
		=> VoiceFiles.ImportVoiceMappingData(folderName);

	private void ImportAllData(string folderName)
		=> DeveloperPackageImport.ImportAllData(folderName);

	private bool ImportKnowledgeFromDir(string importDir)
		=> KnowledgeFiles.ImportKnowledgeFromDir(importDir);

	private bool ImportKnowledgeFromDir(string importDir, bool overwriteExisting)
		=> KnowledgeFiles.ImportKnowledgeFromDir(importDir, overwriteExisting);

	private bool ImportKnowledgeFromDir(string importDir, bool overwriteExisting, out string detailMessage)
		=> KnowledgeFiles.ImportKnowledgeFromDir(importDir, overwriteExisting, out detailMessage);

	private static bool TryImportKnowledgeFileWithFallback(KnowledgeLibraryBehavior kb, KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile, bool overwriteExisting, out int importedCount, out int failedCount, out string firstFailedRuleId, out string firstFailedReason)
		=> KnowledgeRuleImportOwner.TryImportKnowledgeFileWithFallback(kb, knowledgeFile, overwriteExisting, out importedCount, out failedCount, out firstFailedRuleId, out firstFailedReason);

	private static string BuildKnowledgeRuleImportFailureMessage(KnowledgeLibraryBehavior kb, KnowledgeLibraryBehavior.LoreRule rule, bool overwriteExisting)
		=> KnowledgeRuleImportOwner.BuildKnowledgeRuleImportFailureMessage(kb, rule, overwriteExisting);

	private static string FindNpcJsonByHeroId(string dir, string heroId)
		=> NpcDataIdentityFileAdapter.FindNpcJsonByHeroId(dir, heroId);

	private static string ResolveHeroNameForNpcDataFile(string heroId)
		=> NpcDataIdentityFileAdapter.ResolveHeroNameForNpcDataFile(heroId);

	private static Hero ResolveHeroByIdForNpcData(string heroId) => MemoryEntityIdentityBannerlordAdapter.ResolveHeroByIdForNpcData(heroId);

	private static Hero ResolveUniqueHeroByNpcFileDisplayName(string fileDisplayName) => MemoryEntityIdentityBannerlordAdapter.ResolveUniqueHeroByNpcFileDisplayName(fileDisplayName);

	private static bool TryResolveNpcDataFileHeroIdForImport(string filePath, out string resolvedHeroId, out string warning)
		=> NpcDataIdentityFileAdapter.TryResolveNpcDataFileHeroIdForImport(filePath, out resolvedHeroId, out warning);

	private static void StampNpcPersonaProfile(string heroId, NpcPersonaProfile profile)
		=> PersonaProfileImportExportAdapter.StampNpcPersonaProfile(heroId, profile);

	private static bool TryPrepareNpcPersonaProfileForWrite(string heroId, NpcPersonaProfile profile)
		=> PersonaProfileImportExportAdapter.TryPrepareNpcPersonaProfileForWrite(heroId, profile);

}
