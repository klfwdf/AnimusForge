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
 }, DeveloperImport));
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
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			string text2 = Path.Combine(text, "personality_background");
			Directory.CreateDirectory(text2);
			NpcPersonaProfile value = null;
			if (_npcPersonaProfiles != null)
			{
				_npcPersonaProfiles.TryGetValue(heroId, out value);
			}
			if (value == null)
			{
				value = new NpcPersonaProfile();
			}
			if (!TryPrepareNpcPersonaProfileForWrite(heroId, value))
			{
				value = new NpcPersonaProfile();
				StampNpcPersonaProfile(heroId, value);
			}
			string path2 = Path.Combine(text2, NpcDataFileName.Build(heroId, ResolveHeroNameForNpcDataFile(heroId)));
			PlayerExportsStore.WriteJson(path2, value);
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportSingleNpcDialogueHistoryData(string folderName, string heroId)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			string text2 = Path.Combine(text, "compressed_memory");
			Directory.CreateDirectory(text2);
			CompressedMemoryExportBundle value = BuildCompressedMemoryExportBundle(heroId);
			string path2 = Path.Combine(text2, NpcDataFileName.Build(heroId, ResolveHeroNameForNpcDataFile(heroId)));
			PlayerExportsStore.WriteJson(path2, value);
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportSingleNpcDebtData(string folderName, string heroId)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			string text2 = Path.Combine(text, "debt");
			Directory.CreateDirectory(text2);
			RewardSystemBehavior.DebtExportEntry value = null;
			(RewardSystemBehavior.Instance?.ExportDebtEntries())?.TryGetValue(heroId, out value);
			if (value == null)
			{
				value = new RewardSystemBehavior.DebtExportEntry();
			}
			string path2 = Path.Combine(text2, NpcDataFileName.Build(heroId, ResolveHeroNameForNpcDataFile(heroId)));
			PlayerExportsStore.WriteJson(path2, value);
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ImportSingleNpcDebtData(string folderName, string heroId)
	{
		try
		{
			string text = (folderName ?? "").Trim();
			string importDir = null;
			string text2 = null;
			string text3 = null;
			if (!string.IsNullOrEmpty(text) && File.Exists(text))
			{
				text3 = text;
				try
				{
					importDir = Path.GetDirectoryName(Path.GetFullPath(text));
				}
				catch
				{
					importDir = Path.GetDirectoryName(text);
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(text) && Directory.Exists(text))
				{
					importDir = text;
				}
				else
				{
					importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
				}
				if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
					return;
				}
				text2 = Path.Combine(importDir, "debt");
				if (!Directory.Exists(text2))
				{
					try
					{
						string fileName = Path.GetFileName(importDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
						if (string.Equals(fileName, "debt", StringComparison.OrdinalIgnoreCase))
						{
							text2 = importDir;
						}
					}
					catch
					{
					}
				}
				if (!Directory.Exists(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 debt 目录。"));
					return;
				}
			}
			RewardSystemBehavior rs = RewardSystemBehavior.Instance;
			if (rs == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：RewardSystemBehavior 未初始化。"));
				return;
			}
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> all = rs.ExportDebtEntries() ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
			if (string.IsNullOrEmpty(text3))
			{
				text3 = FindNpcJsonByHeroId(text2, heroId);
			}
			if (string.IsNullOrEmpty(text3) || !File.Exists(text3))
			{
				rs.ImportDebtEntries(DebtImportMergePolicy.ApplySingleImportedDebtEntry(all, heroId, null));
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				return;
			}
			RewardSystemBehavior.DebtExportEntry entry = PlayerExportsStore.ReadJson<RewardSystemBehavior.DebtExportEntry>(text3);
			bool flag = entry != null;
			bool flag2 = all.ContainsKey(heroId);
			Action action = delegate
			{
				rs.ImportDebtEntries(DebtImportMergePolicy.ApplySingleImportedDebtEntry(all, heroId, entry));
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				InformationManager.DisplayMessage(new InformationMessage("已跳过重复条目：" + heroId));
			};
			if (flag && flag2)
			{
				ShowDuplicateImportInquiry("检测到重复 - 欠款", "检测到该 NPC 已存在欠款记录：" + heroId + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportSingleNpcPersonaData(string folderName, string heroId)
	{
		long importGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(importGeneration)) return;
		try
		{
			string text = (folderName ?? "").Trim();
			string importDir = null;
			string text2 = null;
			string text3 = null;
			if (!string.IsNullOrEmpty(text) && File.Exists(text))
			{
				text3 = text;
				try
				{
					importDir = Path.GetDirectoryName(Path.GetFullPath(text));
				}
				catch
				{
					importDir = Path.GetDirectoryName(text);
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(text) && Directory.Exists(text))
				{
					importDir = text;
				}
				else
				{
					importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
				}
				if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
					return;
				}
				text2 = Path.Combine(importDir, "personality_background");
				if (!Directory.Exists(text2))
				{
					try
					{
						string fileName = Path.GetFileName(importDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
						if (string.Equals(fileName, "personality_background", StringComparison.OrdinalIgnoreCase))
						{
							text2 = importDir;
						}
					}
					catch
					{
					}
				}
				if (!Directory.Exists(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 personality_background 目录。"));
					return;
				}
				text3 = FindNpcJsonByHeroId(text2, heroId);
			}
			if (string.IsNullOrEmpty(text3) || !File.Exists(text3))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：该NPC没有对应的导出文件。"));
				return;
			}
			if (!TryResolveNpcDataFileHeroIdForImport(text3, out var resolvedHeroId, out var warning) || !string.Equals((resolvedHeroId ?? "").Trim(), (heroId ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：个性/背景文件与当前NPC不匹配。"));
				Logger.Log("NpcPersona", "[WARN] Skipped single persona import file " + Path.GetFileName(text3) + " for hero=" + heroId + ": " + (warning ?? ("resolvedHeroId=" + resolvedHeroId)));
				return;
			}
			NpcPersonaProfile prof = PlayerExportsStore.ReadJson<NpcPersonaProfile>(text3);
			if (_npcPersonaProfiles == null)
			{
				_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
			}
			bool flag = prof != null;
			bool flag2 = false;
			try
			{
				flag2 = _npcPersonaProfiles.TryGetValue(heroId, out var value) && value != null;
			}
			catch
			{
				flag2 = false;
			}
			Action action = delegate
			{
				if (!ApplyImportedSinglePersonaProfile(heroId, prof, importGeneration)) return;
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				InformationManager.DisplayMessage(new InformationMessage("已跳过重复条目：" + heroId));
			};
			if (flag && flag2)
			{
				ShowDuplicateImportInquiry("检测到重复 - 个性/背景", "检测到该 NPC 已存在个性/背景：" + heroId + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportSingleNpcDialogueHistoryData(string folderName, string heroId)
	{
		long importGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(importGeneration)) return;
		try
		{
			string text = (folderName ?? "").Trim();
			string importDir = null;
			string text2 = null;
			string text3 = null;
			if (!string.IsNullOrEmpty(text) && File.Exists(text))
			{
				text3 = text;
				try
				{
					importDir = Path.GetDirectoryName(Path.GetFullPath(text));
				}
				catch
				{
					importDir = Path.GetDirectoryName(text);
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(text) && Directory.Exists(text))
				{
					importDir = text;
				}
				else
				{
					importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
				}
				if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
					return;
				}
				text2 = Path.Combine(importDir, "compressed_memory");
				if (!Directory.Exists(text2))
				{
					try
					{
						string fileName = Path.GetFileName(importDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
						if (string.Equals(fileName, "compressed_memory", StringComparison.OrdinalIgnoreCase))
						{
							text2 = importDir;
						}
					}
					catch
					{
					}
				}
				if (!Directory.Exists(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 compressed_memory 目录。"));
					return;
				}
				text3 = FindNpcJsonByHeroId(text2, heroId);
			}
			if (string.IsNullOrEmpty(text3) || !File.Exists(text3))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：该NPC没有对应的导出文件。"));
				return;
			}
			CompressedMemoryExportBundle bundle = PlayerExportsStore.ReadJson<CompressedMemoryExportBundle>(text3);
			if (bundle == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：压缩记忆文件无效。"));
				return;
			}
			bool flag = true;
			bool flag2 = false;
			try
			{
				string key = NormalizeMemoryHeroId(heroId);
				flag2 = HasCompressedMemoryDataForHero(key);
			}
			catch
			{
				flag2 = false;
			}
			Action action = delegate
			{
				if (!IsMemorySourceEditorCurrent(importGeneration)) return;
				if (ApplyCompressedMemoryExportBundle(heroId, bundle, overwriteExisting: true))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：压缩记忆数据无效。"));
				}
			};
			Action onSkipDuplicates = delegate
			{
				if (!IsMemorySourceEditorCurrent(importGeneration)) return;
				if (ApplyCompressedMemoryExportBundle(heroId, bundle, overwriteExisting: false))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + heroId));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：压缩记忆数据无效。"));
				}
			};
			if (flag && flag2)
			{
				ShowDuplicateImportInquiry("检测到重复 - 压缩记忆", "检测到该 NPC 已存在压缩记忆数据：" + heroId + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			if (!IsMemorySourceEditorCurrent(importGeneration)) return;
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ExportHeroNpcAllData(string folderName)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			string text2 = Path.Combine(text, "personality_background");
			Directory.CreateDirectory(text2);
			PlayerExportsStore.ClearCandidateJsonFiles(text2);
			if (_npcPersonaProfiles != null)
			{
				foreach (KeyValuePair<string, NpcPersonaProfile> npcPersonaProfile in _npcPersonaProfiles)
				{
					if (!string.IsNullOrEmpty(npcPersonaProfile.Key) && TryPrepareNpcPersonaProfileForWrite(npcPersonaProfile.Key, npcPersonaProfile.Value))
					{
						string path2 = Path.Combine(text2, NpcDataFileName.Build(npcPersonaProfile.Key, ResolveHeroNameForNpcDataFile(npcPersonaProfile.Key)));
						PlayerExportsStore.WriteJson(path2, npcPersonaProfile.Value);
					}
				}
			}
			string text3 = Path.Combine(text, "compressed_memory");
			Directory.CreateDirectory(text3);
			PlayerExportsStore.ClearCandidateJsonFiles(text3);
			HashSet<string> memoryHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (_dailyMemoryDrafts != null)
			{
				foreach (string key in _dailyMemoryDrafts.Keys)
				{
					memoryHeroIds.Add(NormalizeMemoryHeroId(key));
				}
			}
			if (_compressedMemoryBlocks != null)
			{
				foreach (string key2 in _compressedMemoryBlocks.Keys)
				{
					memoryHeroIds.Add(NormalizeMemoryHeroId(key2));
				}
			}
			foreach (MemorySummaryJob job in _memorySummaryQueue ?? new List<MemorySummaryJob>())
			{
				if (job != null)
				{
					memoryHeroIds.Add(NormalizeMemoryHeroId(job.HeroId));
				}
			}
			if (_memoryOverviewStates != null)
			{
				foreach (string key3 in _memoryOverviewStates.Keys)
				{
					memoryHeroIds.Add(NormalizeMemoryHeroId(key3));
				}
			}
			foreach (MemoryOverviewJob job2 in _memoryOverviewQueue ?? new List<MemoryOverviewJob>())
			{
				if (job2 != null)
				{
					memoryHeroIds.Add(NormalizeMemoryHeroId(job2.HeroId));
				}
			}
			foreach (string memoryHeroId in memoryHeroIds.Where((string x) => !string.IsNullOrWhiteSpace(x)))
			{
				string path3 = Path.Combine(text3, NpcDataFileName.Build(memoryHeroId, ResolveHeroNameForNpcDataFile(memoryHeroId)));
				PlayerExportsStore.WriteJson(path3, BuildCompressedMemoryExportBundle(memoryHeroId));
			}
			string text4 = Path.Combine(text, "debt");
			Directory.CreateDirectory(text4);
			PlayerExportsStore.ClearCandidateJsonFiles(text4);
			RewardSystemBehavior instance = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = ((instance != null) ? instance.ExportDebtEntries() : new Dictionary<string, RewardSystemBehavior.DebtExportEntry>());
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item2 in dictionary)
				{
					if (!string.IsNullOrEmpty(item2.Key) && item2.Value != null)
					{
						string path4 = Path.Combine(text4, NpcDataFileName.Build(item2.Key, ResolveHeroNameForNpcDataFile(item2.Key)));
						PlayerExportsStore.WriteJson(path4, item2.Value);
					}
				}
			}
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ImportHeroNpcAllData(string folderName)
		=> DeveloperPackageImport.ImportHeroNpcAllData(folderName);

	private void ExportAllData(string folderName)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			string text2 = Path.Combine(text, "personality_background");
			Directory.CreateDirectory(text2);
			PlayerExportsStore.ClearCandidateJsonFiles(text2);
			if (_npcPersonaProfiles != null)
			{
				foreach (KeyValuePair<string, NpcPersonaProfile> npcPersonaProfile in _npcPersonaProfiles)
				{
					if (!string.IsNullOrEmpty(npcPersonaProfile.Key) && TryPrepareNpcPersonaProfileForWrite(npcPersonaProfile.Key, npcPersonaProfile.Value))
					{
						string path2 = Path.Combine(text2, NpcDataFileName.Build(npcPersonaProfile.Key, ResolveHeroNameForNpcDataFile(npcPersonaProfile.Key)));
						PlayerExportsStore.WriteJson(path2, npcPersonaProfile.Value);
					}
				}
			}
			string text3 = Path.Combine(text, "dialogue_history");
			Directory.CreateDirectory(text3);
			PlayerExportsStore.ClearCandidateJsonFiles(text3);
			if (_dialogueHistory != null)
			{
				foreach (KeyValuePair<string, List<DialogueDay>> item in _dialogueHistory)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						string path3 = Path.Combine(text3, NpcDataFileName.Build(item.Key, ResolveHeroNameForNpcDataFile(item.Key)));
						PlayerExportsStore.WriteJson(path3, item.Value);
					}
				}
			}
			string text4 = Path.Combine(text, "debt");
			Directory.CreateDirectory(text4);
			PlayerExportsStore.ClearCandidateJsonFiles(text4);
			RewardSystemBehavior instance = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = ((instance != null) ? instance.ExportDebtEntries() : new Dictionary<string, RewardSystemBehavior.DebtExportEntry>());
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item2 in dictionary)
				{
					if (!string.IsNullOrEmpty(item2.Key) && item2.Value != null)
					{
						string path4 = Path.Combine(text4, NpcDataFileName.Build(item2.Key, ResolveHeroNameForNpcDataFile(item2.Key)));
						PlayerExportsStore.WriteJson(path4, item2.Value);
					}
				}
			}
			if (!TryExportKnowledgeToDir(text, out var exportedKnowledgeCount, out var knowledgeExportError))
			{
				export.RestoreSubdirectory("knowledge");
				InformationManager.DisplayMessage(new InformationMessage("警告：Knowledge 导出失败，已保留旧导出。原因：" + knowledgeExportError));
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("Knowledge 已导出 " + exportedKnowledgeCount + " 条。"));
			}
			ShoutUtils.ExportUnnamedPersonaToDir(text);
			string text5 = Path.Combine(text, "voice_mapping");
			Directory.CreateDirectory(text5);
			PlayerExportsStore.ClearCandidateJsonFiles(text5);
			string path5 = Path.Combine(text5, "VoiceMapping.json");
			string text6 = VoiceMapper.ExportMappingJson();
			if (string.IsNullOrWhiteSpace(text6))
			{
				text6 = "{}";
			}
			File.WriteAllText(path5, text6, Encoding.UTF8);
			ExportEventDataToDir(text);
			if (KingdomStrategicProfileBehavior.Instance != null && !KingdomStrategicProfileBehavior.Instance.ExportAllToDirectory(text, out var kingdomProfileExportMessage))
			{
				export.RestoreSubdirectory("kingdom_profiles");
				InformationManager.DisplayMessage(new InformationMessage("警告：国家战略与性格导出失败，已保留旧导出。原因：" + kingdomProfileExportMessage));
			}
			export.Publish();
			VoiceMapper.SetPreferredExportFolder(export.FinalPath);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportUnnamedPersonaData(string folderName)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			ShoutUtils.ExportUnnamedPersonaToDir(text);
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportPersonaData(string folderName)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			string text2 = Path.Combine(text, "personality_background");
			Directory.CreateDirectory(text2);
			PlayerExportsStore.ClearCandidateJsonFiles(text2);
			if (_npcPersonaProfiles != null)
			{
				foreach (KeyValuePair<string, NpcPersonaProfile> npcPersonaProfile in _npcPersonaProfiles)
				{
					if (!string.IsNullOrEmpty(npcPersonaProfile.Key) && TryPrepareNpcPersonaProfileForWrite(npcPersonaProfile.Key, npcPersonaProfile.Value))
					{
						string path2 = Path.Combine(text2, NpcDataFileName.Build(npcPersonaProfile.Key, ResolveHeroNameForNpcDataFile(npcPersonaProfile.Key)));
						PlayerExportsStore.WriteJson(path2, npcPersonaProfile.Value);
					}
				}
			}
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportDialogueHistoryData(string folderName)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			string text2 = Path.Combine(text, "compressed_memory");
			Directory.CreateDirectory(text2);
			PlayerExportsStore.ClearCandidateJsonFiles(text2);
			HashSet<string> heroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (_dailyMemoryDrafts != null)
			{
				foreach (string key in _dailyMemoryDrafts.Keys)
				{
					heroIds.Add(NormalizeMemoryHeroId(key));
				}
			}
			if (_compressedMemoryBlocks != null)
			{
				foreach (string key2 in _compressedMemoryBlocks.Keys)
				{
					heroIds.Add(NormalizeMemoryHeroId(key2));
				}
			}
			foreach (MemorySummaryJob job in _memorySummaryQueue ?? new List<MemorySummaryJob>())
			{
				if (job != null)
				{
					heroIds.Add(NormalizeMemoryHeroId(job.HeroId));
				}
			}
			if (_memoryOverviewStates != null)
			{
				foreach (string key3 in _memoryOverviewStates.Keys)
				{
					heroIds.Add(NormalizeMemoryHeroId(key3));
				}
			}
			foreach (MemoryOverviewJob job2 in _memoryOverviewQueue ?? new List<MemoryOverviewJob>())
			{
				if (job2 != null)
				{
					heroIds.Add(NormalizeMemoryHeroId(job2.HeroId));
				}
			}
			foreach (string heroId in heroIds.Where((string x) => !string.IsNullOrWhiteSpace(x)))
			{
				string path2 = Path.Combine(text2, NpcDataFileName.Build(heroId, ResolveHeroNameForNpcDataFile(heroId)));
				PlayerExportsStore.WriteJson(path2, BuildCompressedMemoryExportBundle(heroId));
			}
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportDebtData(string folderName)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			string text2 = Path.Combine(text, "debt");
			Directory.CreateDirectory(text2);
			PlayerExportsStore.ClearCandidateJsonFiles(text2);
			RewardSystemBehavior instance = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = ((instance != null) ? instance.ExportDebtEntries() : new Dictionary<string, RewardSystemBehavior.DebtExportEntry>());
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item in dictionary)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						string path2 = Path.Combine(text2, NpcDataFileName.Build(item.Key, ResolveHeroNameForNpcDataFile(item.Key)));
						PlayerExportsStore.WriteJson(path2, item.Value);
					}
				}
			}
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportKnowledgeData(string folderName)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			if (!TryExportKnowledgeToDir(text, out var exportedCount, out var error))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：" + error));
			}
			else
			{
				export.Publish();
				InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath + "（Knowledge " + exportedCount + " 条）"));
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportEventData(string folderName)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			ExportEventDataToDir(text);
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportEventDataToDir(string exportDir)
	{
		string text = Path.Combine(exportDir, "event_data");
		Directory.CreateDirectory(text);
		PlayerExportsStore.ClearCandidateJsonFiles(text);
		PlayerExportsStore.WriteJson(Path.Combine(text, "WorldOpeningSummary.json"), new EventWorldOpeningSummaryJson
		{
			Summary = (_eventWorldOpeningSummary ?? "").Trim()
		});
		PlayerExportsStore.WriteJson(Path.Combine(text, "KingdomOpeningSummaries.json"), BuildEventKingdomSummaryExportMap());
		PlayerExportsStore.WriteJson(Path.Combine(text, "EventRecords.json"), SanitizeEventRecordEntries(_eventRecordEntries));
	}

	private Dictionary<string, string> BuildEventKingdomSummaryExportMap()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (_eventKingdomOpeningSummaries == null)
		{
			return dictionary;
		}
		foreach (KeyValuePair<string, string> item in _eventKingdomOpeningSummaries)
		{
			string text = (item.Key ?? "").Trim();
			string text2 = (item.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2))
			{
				dictionary[text] = text2;
			}
		}
		return dictionary;
	}

	private void ExportKnowledgeToDir(string exportDir)
	{
		TryExportKnowledgeToDir(exportDir, out var _, out var _);
	}

	private bool TryExportKnowledgeToDir(string exportDir, out int exportedCount, out string error)
	{
		exportedCount = 0;
		error = "";
		try
		{
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			if (knowledgeLibraryBehavior == null)
			{
				error = "KnowledgeLibraryBehavior 未初始化。";
				return false;
			}
			if (!knowledgeLibraryBehavior.TryValidateKnowledgeExport(out error))
			{
				return false;
			}
			string value = knowledgeLibraryBehavior.ExportRulesJson();
			KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = (string.IsNullOrWhiteSpace(value) ? null : JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value));
			if (knowledgeFile?.Rules == null || knowledgeFile.Rules.Count <= 0)
			{
				error = "当前没有可导出的知识条目。";
				return false;
			}
			string text = Path.Combine(exportDir, "knowledge", "rules");
			Directory.CreateDirectory(text);
			PlayerExportsStore.ClearCandidateJsonFiles(text);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
			{
				string text2 = (rule?.Id ?? "").Trim();
				if (string.IsNullOrEmpty(text2) || !hashSet.Add(text2))
				{
					continue;
				}
				rule.Id = text2;
				string text3 = "";
				try
				{
					text3 = rule?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
				}
				catch
				{
					text3 = "";
				}
				string text4 = text2;
				char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
				foreach (char oldChar in invalidFileNameChars)
				{
					text4 = text4.Replace(oldChar, '_');
				}
				text4 = (text4 ?? "").Trim();
				if (text4.Length > 80)
				{
					text4 = text4.Substring(0, 80);
				}
				if (string.IsNullOrEmpty(text4))
				{
					text4 = "rule";
				}
				string text5 = text3;
				char[] invalidFileNameChars2 = Path.GetInvalidFileNameChars();
				foreach (char oldChar2 in invalidFileNameChars2)
				{
					text5 = text5.Replace(oldChar2, '_');
				}
				text5 = (text5 ?? "").Trim();
				if (text5.Length > 35)
				{
					text5 = text5.Substring(0, 35);
				}
				string text6 = text4;
				if (!string.IsNullOrEmpty(text5))
				{
					text6 = text6 + "__" + text5;
				}
				string path = Path.Combine(text, text6 + ".json");
				if (File.Exists(path))
				{
					for (int num = 2; num <= 999; num++)
					{
						string text7 = Path.Combine(text, text6 + "__" + num + ".json");
						if (!File.Exists(text7))
						{
							path = text7;
							break;
						}
					}
				}
				string contents = JsonConvert.SerializeObject(rule, Formatting.Indented);
				File.WriteAllText(path, contents, Encoding.UTF8);
				exportedCount++;
			}
			if (exportedCount <= 0)
			{
				error = "没有成功写出任何知识文件。";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private void ExportSingleKnowledgeRuleData(string folderName, string ruleId)
	{
		try
		{
			string text = (ruleId ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：RuleId 为空。"));
				return;
			}
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			if (knowledgeLibraryBehavior == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：KnowledgeLibraryBehavior 未初始化。"));
				return;
			}
			if (!knowledgeLibraryBehavior.TryValidateSingleRuleExport(text, out var error))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：" + error));
				return;
			}
			string text2 = knowledgeLibraryBehavior.ExportSingleRuleJson(text, pretty: true);
			if (string.IsNullOrWhiteSpace(text2))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：找不到该知识条目：" + text));
				return;
			}
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text3 = export.CandidatePath;
			string text4 = Path.Combine(text3, "knowledge", "rules");
			Directory.CreateDirectory(text4);
			KnowledgeLibraryBehavior.LoreRule loreRule = null;
			try
			{
				loreRule = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(text2);
			}
			catch
			{
				loreRule = null;
			}
			string text5 = "";
			try
			{
				text5 = loreRule?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
			}
			catch
			{
				text5 = "";
			}
			string text6 = text;
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text6 = text6.Replace(oldChar, '_');
			}
			text6 = (text6 ?? "").Trim();
			if (text6.Length > 80)
			{
				text6 = text6.Substring(0, 80);
			}
			if (string.IsNullOrEmpty(text6))
			{
				text6 = "rule";
			}
			string text7 = text5;
			char[] invalidFileNameChars2 = Path.GetInvalidFileNameChars();
			foreach (char oldChar2 in invalidFileNameChars2)
			{
				text7 = text7.Replace(oldChar2, '_');
			}
			text7 = (text7 ?? "").Trim();
			if (text7.Length > 35)
			{
				text7 = text7.Substring(0, 35);
			}
			string text8 = text6;
			if (!string.IsNullOrEmpty(text7))
			{
				text8 = text8 + "__" + text7;
			}
			string path2 = Path.Combine(text4, text8 + ".json");
			File.WriteAllText(path2, text2, Encoding.UTF8);
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ImportSingleKnowledgeRuleData(string folderName, string ruleId)
	{
		try
		{
			string id = (ruleId ?? "").Trim();
			if (string.IsNullOrEmpty(id))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：RuleId 为空。"));
				return;
			}
			string importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			KnowledgeLibraryBehavior kb = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			if (kb == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：KnowledgeLibraryBehavior 未初始化。"));
				return;
			}
			string dir = Path.Combine(importDir, "knowledge", "rules");
			string dir2 = Path.Combine(importDir, "knowledge", "single_rules");
			string dir3 = Path.Combine(importDir, "knowledge");
			string text = KnowledgeImportSupport.FindKnowledgeRuleJsonById(dir, id);
			if (string.IsNullOrEmpty(text))
			{
				text = KnowledgeImportSupport.FindKnowledgeRuleJsonById(dir2, id);
			}
			if (string.IsNullOrEmpty(text))
			{
				text = KnowledgeImportSupport.FindKnowledgeRuleJsonById(dir3, id);
			}
			if (string.IsNullOrEmpty(text) || !File.Exists(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到该知识条目的导出文件：" + id));
				return;
			}
			string value = File.ReadAllText(text, Encoding.UTF8);
			if (string.IsNullOrWhiteSpace(value))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：文件为空。"));
				return;
			}
			KnowledgeLibraryBehavior.LoreRule rule = null;
			try
			{
				rule = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(value);
			}
			catch
			{
				rule = null;
			}
			if (rule == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：JSON 解析失败。"));
				return;
			}
			string text2 = (rule.Id ?? "").Trim();
			if (!string.IsNullOrEmpty(text2) && !string.Equals(text2, id, StringComparison.OrdinalIgnoreCase))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：文件中的 Id 不匹配（文件Id=" + text2 + "）。"));
				return;
			}
			if (string.IsNullOrEmpty(text2))
			{
				rule.Id = id;
			}
			bool flag = false;
			try
			{
				flag = !string.IsNullOrWhiteSpace(kb.ExportSingleRuleJson(id));
			}
			catch
			{
				flag = false;
			}
			Action action = delegate
			{
				if (!ValidateKnowledgeKeywordsForSingleRuleImport(kb, rule, overwriteExisting: true, out var error))
				{
					InformationManager.DisplayMessage(new InformationMessage(error));
				}
				else if (!kb.ImportSingleRuleJson(JsonConvert.SerializeObject(rule, Formatting.None)))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：写入规则失败。"));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				}
			};
			Action onSkipDuplicates = delegate
			{
				if (!kb.ImportSingleRuleJson(JsonConvert.SerializeObject(rule, Formatting.None), overwrite: false))
				{
					InformationManager.DisplayMessage(new InformationMessage("已跳过重复条目：" + id));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				}
			};
			if (flag)
			{
				ShowDuplicateImportInquiry("检测到重复 - Knowledge", "检测到相同 RuleId：" + id + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ExportSingleUnnamedPersonaData(string folderName, string key)
	{
		try
		{
			string text = (key ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：Key 为空。"));
				return;
			}
			string personality = "";
			string background = "";
			bool flag = false;
			try
			{
				flag = ShoutUtils.TryGetUnnamedPersonaByKey(text, out personality, out background);
			}
			catch
			{
				flag = false;
			}
			if (!flag)
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：找不到该未命名NPC条目：" + text));
				return;
			}
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text2 = export.CandidatePath;
			string text3 = Path.Combine(text2, "unnamed_persona");
			Directory.CreateDirectory(text3);
			string text4 = text;
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text4 = text4.Replace(oldChar, '_');
			}
			text4 = text4.Replace(':', '_').Replace('/', '_').Replace('\\', '_');
			while (text4.Contains("__"))
			{
				text4 = text4.Replace("__", "_");
			}
			text4 = (text4 ?? "").Trim();
			if (text4.Length > 120)
			{
				text4 = text4.Substring(0, 120);
			}
			if (string.IsNullOrEmpty(text4))
			{
				text4 = "unnamed";
			}
			string path2 = Path.Combine(text3, text4 + ".json");
			PlayerExportsStore.WriteJson(path2, new UnnamedPersonaSingleJson
			{
				Key = text,
				Personality = (personality ?? "").Trim(),
				Background = (background ?? "").Trim()
			});
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ImportSingleUnnamedPersonaData(string folderName, string key)
	{
		try
		{
			string text = (key ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：Key 为空。"));
				return;
			}
			string text2 = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(text2) || !Directory.Exists(text2))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			string dir = Path.Combine(text2, "unnamed_persona");
			string dir2 = text2;
			string text3 = FindUnnamedPersonaJsonByKey(dir, text);
			if (string.IsNullOrEmpty(text3))
			{
				text3 = FindUnnamedPersonaJsonByKey(dir2, text);
			}
			if (string.IsNullOrEmpty(text3) || !File.Exists(text3))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到该未命名NPC条目的导出文件：" + text));
				return;
			}
			UnnamedPersonaSingleJson unnamedPersonaSingleJson = PlayerExportsStore.ReadJson<UnnamedPersonaSingleJson>(text3);
			if (unnamedPersonaSingleJson == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：JSON 解析失败。"));
				return;
			}
			string text4 = (unnamedPersonaSingleJson.Personality ?? "").Trim();
			string text5 = (unnamedPersonaSingleJson.Background ?? "").Trim();
			bool flag = !string.IsNullOrEmpty(text4) || !string.IsNullOrEmpty(text5);
			bool flag2 = false;
			try
			{
				flag2 = ShoutUtils.HasUnnamedPersonaKey(text);
			}
			catch
			{
				flag2 = false;
			}
			if (flag && flag2)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：检测到重复 Key（" + text + "），当前游戏已存在该条目，禁止导入覆盖。"));
				return;
			}
			ShoutUtils.SaveUnnamedPersonaByKey(text, text4, text5);
			InformationManager.DisplayMessage(new InformationMessage("导入完成：" + text2));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private static string FindUnnamedPersonaJsonByKey(string dir, string key)
	{
		try
		{
			string text = (key ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
			{
				return null;
			}
			string text2 = text;
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text2 = text2.Replace(oldChar, '_');
			}
			text2 = text2.Replace(':', '_').Replace('/', '_').Replace('\\', '_');
			while (text2.Contains("__"))
			{
				text2 = text2.Replace("__", "_");
			}
			text2 = (text2 ?? "").Trim();
			if (text2.Length > 120)
			{
				text2 = text2.Substring(0, 120);
			}
			if (!string.IsNullOrEmpty(text2))
			{
				string text3 = Path.Combine(dir, text2 + ".json");
				if (File.Exists(text3))
				{
					return text3;
				}
				try
				{
					string[] files = Directory.GetFiles(dir, text2 + "__*.json");
					if (files != null && files.Length != 0)
					{
						return files[0];
					}
				}
				catch
				{
				}
			}
			string[] files2 = Directory.GetFiles(dir, "*.json");
			string[] array = files2;
			foreach (string text4 in array)
			{
				try
				{
					string value = File.ReadAllText(text4, Encoding.UTF8);
					if (string.IsNullOrWhiteSpace(value))
					{
						continue;
					}
					UnnamedPersonaSingleJson unnamedPersonaSingleJson = JsonConvert.DeserializeObject<UnnamedPersonaSingleJson>(value);
					if (unnamedPersonaSingleJson != null)
					{
						string a = (unnamedPersonaSingleJson.Key ?? "").Trim().ToLower();
						if (string.Equals(a, text, StringComparison.OrdinalIgnoreCase))
						{
							return text4;
						}
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private List<string> GetKnowledgeRuleIdsFromImportFolderForDev(string folderName, int maxCount = 200)
	{
		List<string> list = new List<string>();
		try
		{
			if (maxCount <= 0)
			{
				maxCount = 200;
			}
			string text = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(text) || !Directory.Exists(text))
			{
				return list;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<string> source = new List<string>
			{
				Path.Combine(text, "knowledge", "rules"),
				Path.Combine(text, "knowledge", "single_rules"),
				Path.Combine(text, "knowledge")
			};
			foreach (string item in source.Where((string d) => !string.IsNullOrEmpty(d)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				string[] array = null;
				try
				{
					array = Directory.GetFiles(item, "*.json");
				}
				catch
				{
					array = null;
				}
				if (array == null)
				{
					continue;
				}
				string[] array2 = array;
				foreach (string path in array2)
				{
					if (list.Count >= maxCount)
					{
						break;
					}
					try
					{
						string value = File.ReadAllText(path, Encoding.UTF8);
						if (!string.IsNullOrWhiteSpace(value))
						{
							string text2 = (JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(value)?.Id ?? "").Trim();
							if (!string.IsNullOrEmpty(text2) && hashSet.Add(text2))
							{
								list.Add(text2);
							}
						}
					}
					catch
					{
					}
				}
				if (list.Count < maxCount)
				{
					continue;
				}
				break;
			}
		}
		catch
		{
		}
		try
		{
			list.Sort(StringComparer.OrdinalIgnoreCase);
		}
		catch
		{
		}
		if (list.Count > maxCount)
		{
			list.RemoveRange(maxCount, list.Count - maxCount);
		}
		return list;
	}

	private List<string> GetUnnamedPersonaKeysFromImportFolderForDev(string folderName, int maxCount = 200)
	{
		List<string> list = new List<string>();
		try
		{
			if (maxCount <= 0)
			{
				maxCount = 200;
			}
			string text = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(text) || !Directory.Exists(text))
			{
				return list;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<string> source = new List<string>
			{
				Path.Combine(text, "unnamed_persona"),
				text
			};
			foreach (string item in source.Where((string d) => !string.IsNullOrEmpty(d)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				string[] array = null;
				try
				{
					array = Directory.GetFiles(item, "*.json");
				}
				catch
				{
					array = null;
				}
				if (array == null)
				{
					continue;
				}
				string[] array2 = array;
				foreach (string path in array2)
				{
					if (list.Count >= maxCount)
					{
						break;
					}
					try
					{
						string value = File.ReadAllText(path, Encoding.UTF8);
						if (!string.IsNullOrWhiteSpace(value))
						{
							string text2 = (JsonConvert.DeserializeObject<UnnamedPersonaSingleJson>(value)?.Key ?? "").Trim().ToLower();
							if (!string.IsNullOrEmpty(text2) && hashSet.Add(text2))
							{
								list.Add(text2);
							}
						}
					}
					catch
					{
					}
				}
				if (list.Count < maxCount)
				{
					continue;
				}
				break;
			}
		}
		catch
		{
		}
		try
		{
			list.Sort(StringComparer.OrdinalIgnoreCase);
		}
		catch
		{
		}
		if (list.Count > maxCount)
		{
			list.RemoveRange(maxCount, list.Count - maxCount);
		}
		return list;
	}

	private void ImportPersonaData(string folderName)
	{
		long importGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(importGeneration)) return;
		try
		{
			string importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			string path = Path.Combine(importDir, "personality_background");
			if (!Directory.Exists(path))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 personality_background 目录。"));
				return;
			}
			string[] files = Directory.GetFiles(path, "*.json");
			Dictionary<string, NpcPersonaProfile> dict = new Dictionary<string, NpcPersonaProfile>();
			string[] array = files;
			foreach (string text in array)
			{
				if (TryResolveNpcDataFileHeroIdForImport(text, out var text2, out var warning))
				{
					NpcPersonaProfile npcPersonaProfile = PlayerExportsStore.ReadJson<NpcPersonaProfile>(text);
					if (npcPersonaProfile != null)
					{
						StampNpcPersonaProfile(text2, npcPersonaProfile);
						dict[text2] = npcPersonaProfile;
					}
				}
				else
				{
					Logger.Log("NpcPersona", "[WARN] Skipped persona import file " + Path.GetFileName(text) + ": " + warning);
				}
			}
			int num = 0;
			int count = dict.Count;
			try
			{
				if (_npcPersonaProfiles != null)
				{
					foreach (string key in dict.Keys)
					{
						if (!string.IsNullOrEmpty(key) && _npcPersonaProfiles.ContainsKey(key))
						{
							num++;
						}
					}
				}
			}
			catch
			{
				num = 0;
			}
			Action action = delegate
			{
				if (!ApplyImportedPersonaProfiles(dict, true, importGeneration)) return;
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				if (!ApplyImportedPersonaProfiles(dict, false, importGeneration)) return;
				InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
			};
			if (num > 0)
			{
				ShowDuplicateImportInquiry("检测到重复 - 个性/背景", "导入数据与当前游戏存在重复 HeroId。\n重复：" + num + " / 总计：" + count + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportDialogueHistoryData(string folderName)
	{
		long importGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(importGeneration)) return;
		try
		{
			string importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			string path = Path.Combine(importDir, "compressed_memory");
			if (!Directory.Exists(path))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 compressed_memory 目录。"));
				return;
			}
			string[] files = Directory.GetFiles(path, "*.json");
			int invalidMemoryFiles = 0;
			Dictionary<string, CompressedMemoryExportBundle> dict = new Dictionary<string, CompressedMemoryExportBundle>(StringComparer.OrdinalIgnoreCase);
			string[] array = files;
			foreach (string text in array)
			{
				string text2 = NpcDataFileName.TryParseHeroId(text);
				if (!string.IsNullOrEmpty(text2))
				{
					CompressedMemoryExportBundle bundle = PlayerExportsStore.ReadJson<CompressedMemoryExportBundle>(text);
					if (bundle != null)
					{
						dict[NormalizeMemoryHeroId(text2)] = bundle;
					}
					else
					{
						invalidMemoryFiles++;
					}
				}
				else
				{
					invalidMemoryFiles++;
				}
			}
			if (files.Length > 0 && dict.Count == 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：压缩记忆文件全部无效（跳过 " + invalidMemoryFiles + " 个）。"));
				return;
			}
			int num = 0;
			int count = dict.Count;
			try
			{
				foreach (string key in dict.Keys)
				{
					string text3 = NormalizeMemoryHeroId(key);
					if (!string.IsNullOrWhiteSpace(text3) && HasCompressedMemoryDataForHero(text3))
					{
						num++;
					}
				}
			}
			catch
			{
				num = 0;
			}
			Action action = delegate
			{
				if (!IsMemorySourceEditorCurrent(importGeneration)) return;
				foreach (KeyValuePair<string, CompressedMemoryExportBundle> item in dict)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						ApplyCompressedMemoryExportBundle(item.Key, item.Value, overwriteExisting: true);
					}
				}
				InformationManager.DisplayMessage(new InformationMessage(invalidMemoryFiles > 0 ? "部分导入完成（跳过 " + invalidMemoryFiles + " 个无效压缩记忆文件）：" + importDir : "导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				if (!IsMemorySourceEditorCurrent(importGeneration)) return;
				foreach (KeyValuePair<string, CompressedMemoryExportBundle> item2 in dict)
				{
					if (!string.IsNullOrEmpty(item2.Key) && item2.Value != null)
					{
						ApplyCompressedMemoryExportBundle(item2.Key, item2.Value, overwriteExisting: false);
					}
				}
				InformationManager.DisplayMessage(new InformationMessage(invalidMemoryFiles > 0 ? "部分导入完成（跳过 " + invalidMemoryFiles + " 个无效压缩记忆文件，已跳过重复）：" + importDir : "导入完成（已跳过重复）：" + importDir));
			};
			if (num > 0)
			{
				ShowDuplicateImportInquiry("检测到重复 - 压缩记忆", "导入数据与当前游戏存在重复 HeroId。\n重复：" + num + " / 总计：" + count + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			if (!IsMemorySourceEditorCurrent(importGeneration)) return;
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportDebtData(string folderName)
	{
		try
		{
			string importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			string path = Path.Combine(importDir, "debt");
			if (!Directory.Exists(path))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 debt 目录。"));
				return;
			}
			RewardSystemBehavior rs = RewardSystemBehavior.Instance;
			if (rs == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：RewardSystemBehavior 未初始化。"));
				return;
			}
			string[] files = Directory.GetFiles(path, "*.json");
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> dict = new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
			string[] array = files;
			foreach (string text in array)
			{
				string text2 = NpcDataFileName.TryParseHeroId(text);
				if (!string.IsNullOrEmpty(text2))
				{
					RewardSystemBehavior.DebtExportEntry debtExportEntry = PlayerExportsStore.ReadJson<RewardSystemBehavior.DebtExportEntry>(text);
					if (debtExportEntry != null)
					{
						dict[text2] = debtExportEntry;
					}
				}
			}
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> exist = rs.ExportDebtEntries() ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
			int num = 0;
			int count = dict.Count;
			try
			{
				foreach (string key in dict.Keys)
				{
					if (!string.IsNullOrEmpty(key) && exist.ContainsKey(key))
					{
						num++;
					}
				}
			}
			catch
			{
				num = 0;
			}
			Action action = delegate
			{
				rs.ImportDebtEntries(DebtImportMergePolicy.ApplyImportedDebtEntries(exist, dict, true));
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				rs.ImportDebtEntries(DebtImportMergePolicy.ApplyImportedDebtEntries(exist, dict, false));
				InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
			};
			if (num > 0)
			{
				ShowDuplicateImportInquiry("检测到重复 - 欠款", "导入数据与当前游戏存在重复 HeroId。\n重复：" + num + " / 总计：" + count + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportEventData(string folderName)
	{
		try
		{
			string importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			if (!TryLoadEventDataFromImportDir(importDir, out var payload, out var error))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：" + error));
				return;
			}
			int num = 0;
			int num2 = 0;
			if (payload.HasWorldSummaryFile)
			{
				num2 = 1;
				if (!string.IsNullOrWhiteSpace(_eventWorldOpeningSummary))
				{
					num = 1;
				}
			}
			int num3 = 0;
			int num4 = payload.HasKingdomSummariesFile ? payload.KingdomSummaries.Count : 0;
			if (payload.HasKingdomSummariesFile && _eventKingdomOpeningSummaries != null)
			{
				foreach (string key in payload.KingdomSummaries.Keys)
				{
					if (!string.IsNullOrWhiteSpace(key) && _eventKingdomOpeningSummaries.ContainsKey(key))
					{
						num3++;
					}
				}
			}
			int num5 = 0;
			int num6 = payload.HasEventRecordsFile ? payload.EventRecords.Count : 0;
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (_eventRecordEntries != null)
			{
				foreach (EventRecordEntry eventRecordEntry in _eventRecordEntries)
				{
					string text = (eventRecordEntry?.EventId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text))
					{
						hashSet.Add(text);
					}
				}
			}
			if (payload.HasEventRecordsFile)
			{
				foreach (EventRecordEntry eventRecordEntry2 in payload.EventRecords)
				{
					string text2 = (eventRecordEntry2?.EventId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2) && hashSet.Contains(text2))
					{
						num5++;
					}
				}
			}
			bool flag = num + num3 + num5 > 0;
			Action action = delegate
			{
				ApplyImportedEventData(payload, overwriteExisting: true);
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				ApplyImportedEventData(payload, overwriteExisting: false);
				InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
			};
			if (flag)
			{
				string text3 = "导入数据与当前游戏存在重复。\n世界开局概要：" + num + "/" + num2 + "\n王国开局概要：" + num3 + "/" + num4 + "\n事件记录：" + num5 + "/" + num6 + "\n请选择处理方式：";
				ShowDuplicateImportInquiry("检测到重复 - 事件编辑", text3, action, onSkipDuplicates, delegate
				{
					OpenDevEventEditorMenu();
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportUnnamedPersonaData(string folderName)
	{
		try
		{
			string text = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(text) || !Directory.Exists(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			if (!ValidateUnnamedPersonaKeysForImport(text, out var error))
			{
				InformationManager.DisplayMessage(new InformationMessage(error));
				return;
			}
			ShoutUtils.ImportUnnamedPersonaFromDir(text);
			InformationManager.DisplayMessage(new InformationMessage("导入完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportKnowledgeData(string folderName)
	{
		try
		{
			string importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			int num = 0;
			int num2 = 0;
			try
			{
				KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
				if (knowledgeLibraryBehavior != null)
				{
					HashSet<string> hashSet = new HashSet<string>(knowledgeLibraryBehavior.GetRuleIdsForDev(100000) ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
					HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = KnowledgeImportSupport.TryLoadKnowledgeRulesFromRuleFiles(importDir);
					if (knowledgeFile?.Rules != null)
					{
						foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
						{
							string text = (rule?.Id ?? "").Trim();
							if (!string.IsNullOrEmpty(text))
							{
								hashSet2.Add(text);
							}
						}
					}
					string path = Path.Combine(importDir, "knowledge", "KnowledgeRules.json");
					if (hashSet2.Count <= 0 && File.Exists(path))
					{
						string value = File.ReadAllText(path, Encoding.UTF8);
						KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value);
						if (knowledgeFile2?.Rules != null)
						{
							foreach (KnowledgeLibraryBehavior.LoreRule rule2 in knowledgeFile2.Rules)
							{
								string text2 = (rule2?.Id ?? "").Trim();
								if (!string.IsNullOrEmpty(text2))
								{
									hashSet2.Add(text2);
								}
							}
						}
					}
					num2 = hashSet2.Count;
					foreach (string item in hashSet2)
					{
						if (hashSet.Contains(item))
						{
							num++;
						}
					}
				}
			}
			catch
			{
				num = 0;
				num2 = 0;
			}
			Action action = delegate
			{
				if (!ValidateKnowledgeKeywordsForImport(importDir, overwriteExisting: true, out var error))
				{
					InformationManager.DisplayMessage(new InformationMessage(error));
				}
				else if (!ImportKnowledgeFromDir(importDir, overwriteExisting: true, out var detailMessage))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：" + (string.IsNullOrWhiteSpace(detailMessage) ? "找不到 knowledge\\AIConfig.json 或 knowledge\\rules(\\*.json) 或 knowledge\\KnowledgeRules.json" : detailMessage)));
				}
				else
				{
					if (!string.IsNullOrWhiteSpace(detailMessage))
					{
						InformationManager.DisplayMessage(new InformationMessage(detailMessage));
					}
					InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				}
			};
			Action onSkipDuplicates = delegate
			{
				if (!ValidateKnowledgeKeywordsForImport(importDir, overwriteExisting: false, out var error))
				{
					InformationManager.DisplayMessage(new InformationMessage(error));
				}
				else if (!ImportKnowledgeFromDir(importDir, overwriteExisting: false, out var detailMessage))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：" + (string.IsNullOrWhiteSpace(detailMessage) ? "找不到 knowledge\\AIConfig.json 或 knowledge\\rules(\\*.json) 或 knowledge\\KnowledgeRules.json" : detailMessage)));
				}
				else
				{
					if (!string.IsNullOrWhiteSpace(detailMessage))
					{
						InformationManager.DisplayMessage(new InformationMessage(detailMessage));
					}
					InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
				}
			};
			if (num > 0)
			{
				ShowDuplicateImportInquiry("检测到重复 - Knowledge", "导入的 Knowledge 规则与当前游戏存在重复 RuleId。\n重复：" + num + " / 总计：" + num2 + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ExportVoiceMappingData(string folderName)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			string text2 = Path.Combine(text, "voice_mapping");
			Directory.CreateDirectory(text2);
			PlayerExportsStore.ClearCandidateJsonFiles(text2);
			string path2 = Path.Combine(text2, "VoiceMapping.json");
			string text3 = VoiceMapper.ExportMappingJson();
			if (string.IsNullOrWhiteSpace(text3))
			{
				text3 = "{}";
			}
			File.WriteAllText(path2, text3, Encoding.UTF8);
			export.Publish();
			VoiceMapper.SetPreferredExportFolder(export.FinalPath);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ImportVoiceMappingData(string folderName)
	{
		try
		{
			string importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			string text = null;
			string text2 = Path.Combine(importDir, "voice_mapping", "VoiceMapping.json");
			string text3 = Path.Combine(importDir, "VoiceMapping.json");
			if (File.Exists(text2))
			{
				text = text2;
			}
			else if (File.Exists(text3))
			{
				text = text3;
			}
			if (string.IsNullOrEmpty(text) || !File.Exists(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 voice_mapping/VoiceMapping.json。"));
				return;
			}
			string json = File.ReadAllText(text, Encoding.UTF8);
			if (string.IsNullOrWhiteSpace(json))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：VoiceMapping.json 为空。"));
				return;
			}
			bool flag = false;
			try
			{
				flag = VoiceMapper.GetTotalVoiceCount() > 0 || !string.IsNullOrWhiteSpace(VoiceMapper.GetFallbackVoice());
			}
			catch
			{
				flag = false;
			}
			Action action = delegate
			{
				bool flag2 = VoiceMapper.ImportMappingFromFile(text);
				if (flag2)
				{
					_voiceMappingJsonStorage = VoiceMapper.ExportMappingJson(pretty: false) ?? "";
				}
				InformationManager.DisplayMessage(new InformationMessage(flag2 ? ("导入完成：" + importDir) : "导入失败：VoiceMapping JSON 无效。"));
			};
			Action onSkipDuplicates = delegate
			{
				bool flag2 = VoiceMapper.ImportMappingFromFile(text, overwriteExisting: false);
				if (flag2)
				{
					_voiceMappingJsonStorage = VoiceMapper.ExportMappingJson(pretty: false) ?? "";
				}
				InformationManager.DisplayMessage(new InformationMessage(flag2 ? ("导入完成（已合并）：" + importDir) : "导入失败：VoiceMapping JSON 无效。"));
			};
			if (flag)
			{
				ShowDuplicateImportInquiry("检测到重复 - VoiceMapping", "当前已存在声音映射。\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportAllData(string folderName)
		=> DeveloperPackageImport.ImportAllData(folderName);

	private bool ImportKnowledgeFromDir(string importDir)
	{
		return ImportKnowledgeFromDir(importDir, overwriteExisting: true, out var _);
	}

	private bool ImportKnowledgeFromDir(string importDir, bool overwriteExisting)
	{
		return ImportKnowledgeFromDir(importDir, overwriteExisting, out var _);
	}

	private bool ImportKnowledgeFromDir(string importDir, bool overwriteExisting, out string detailMessage)
	{
		detailMessage = "";
		try
		{
			bool result = false;
			string text = Path.Combine(importDir, "knowledge", "AIConfig.json");
			if (File.Exists(text))
			{
				string moduleRootPath = PlayerExportsStore.GetModuleRootPath();
				string text2 = Path.Combine(moduleRootPath, "ModuleData", "AIConfig.json");
				string text3 = Path.Combine(moduleRootPath, "AIConfig.json");
				try
				{
					Directory.CreateDirectory(Path.GetDirectoryName(text2));
					if (overwriteExisting || !File.Exists(text2))
					{
						File.Copy(text, text2, overwrite: true);
					}
				}
				catch
				{
				}
				try
				{
					if (overwriteExisting && File.Exists(text3))
					{
						File.Copy(text, text3, overwrite: true);
					}
				}
				catch
				{
				}
				try
				{
					AIConfigHandler.ReloadConfig();
				}
				catch
				{
				}
				result = true;
			}
			try
			{
				KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
				if (knowledgeLibraryBehavior != null)
				{
					KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = KnowledgeImportSupport.TryLoadKnowledgeRulesFromRuleFiles(importDir);
					if (knowledgeFile != null)
					{
						if (TryImportKnowledgeFileWithFallback(knowledgeLibraryBehavior, knowledgeFile, overwriteExisting, out var importedCount, out var failedCount, out var firstFailedRuleId, out var firstFailedReason))
						{
							result = true;
							if (failedCount > 0)
							{
								detailMessage = "Knowledge 全量导入已改为逐条回退导入；成功 " + importedCount + " 条，失败 " + failedCount + " 条。首个失败 RuleId=" + firstFailedRuleId + (string.IsNullOrWhiteSpace(firstFailedReason) ? "" : "；原因：" + firstFailedReason);
							}
						}
						else
						{
							detailMessage = (failedCount > 0) ? ("知识规则全部导入失败。失败 " + failedCount + " 条。首个失败 RuleId=" + firstFailedRuleId + (string.IsNullOrWhiteSpace(firstFailedReason) ? "" : "；原因：" + firstFailedReason)) : "知识规则写入失败。";
						}
					}
					else
					{
						string path = Path.Combine(importDir, "knowledge", "KnowledgeRules.json");
						if (File.Exists(path))
						{
							string json2 = File.ReadAllText(path);
							KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(json2);
							if (TryImportKnowledgeFileWithFallback(knowledgeLibraryBehavior, knowledgeFile2, overwriteExisting, out var importedCount2, out var failedCount2, out var firstFailedRuleId2, out var firstFailedReason2))
							{
								result = true;
								if (failedCount2 > 0)
								{
									detailMessage = "Knowledge 全量导入已改为逐条回退导入；成功 " + importedCount2 + " 条，失败 " + failedCount2 + " 条。首个失败 RuleId=" + firstFailedRuleId2 + (string.IsNullOrWhiteSpace(firstFailedReason2) ? "" : "；原因：" + firstFailedReason2);
								}
							}
							else
							{
								detailMessage = (failedCount2 > 0) ? ("知识规则全部导入失败。失败 " + failedCount2 + " 条。首个失败 RuleId=" + firstFailedRuleId2 + (string.IsNullOrWhiteSpace(firstFailedReason2) ? "" : "；原因：" + firstFailedReason2)) : "知识规则写入失败。";
							}
						}
						else if (!File.Exists(text))
						{
							detailMessage = "找不到 knowledge\\AIConfig.json、knowledge\\rules\\*.json 或 knowledge\\KnowledgeRules.json。";
						}
					}
				}
				else
				{
					detailMessage = "KnowledgeLibraryBehavior 未初始化。";
				}
			}
			catch
			{
				detailMessage = "知识导入过程发生异常。";
			}
			return result;
		}
		catch (Exception ex)
		{
			detailMessage = ex.Message;
			return false;
		}
	}

	private static bool TryImportKnowledgeFileWithFallback(KnowledgeLibraryBehavior kb, KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile, bool overwriteExisting, out int importedCount, out int failedCount, out string firstFailedRuleId, out string firstFailedReason)
		=> KnowledgeRuleImportOwner.TryImportKnowledgeFileWithFallback(kb, knowledgeFile, overwriteExisting, out importedCount, out failedCount, out firstFailedRuleId, out firstFailedReason);

	private static string BuildKnowledgeRuleImportFailureMessage(KnowledgeLibraryBehavior kb, KnowledgeLibraryBehavior.LoreRule rule, bool overwriteExisting)
		=> KnowledgeRuleImportOwner.BuildKnowledgeRuleImportFailureMessage(kb, rule, overwriteExisting);

	private static string FindNpcJsonByHeroId(string dir, string heroId)
	{
		try
		{
			if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
			{
				return null;
			}
			string text = (heroId ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			string result = null;
			string nameMatchedResult = null;
			DateTime dateTime = DateTime.MinValue;
			DateTime nameMatchedDateTime = DateTime.MinValue;
			Hero targetHero = ResolveHeroByIdForNpcData(text);
			string[] files = Directory.GetFiles(dir, "*.json");
			string[] array = files;
			foreach (string text2 in array)
			{
				if (!NpcDataFileName.TryParseParts(text2, out var text3, out var fileDisplayName))
				{
					continue;
				}
				if (!(text3 != text))
				{
					if (targetHero != null && !NpcDataFileName.IsDisplayNameCompatible(fileDisplayName, targetHero?.Name?.ToString(), targetHero != null, NpcDataFileName.IsAutoGeneratedHeroId(text)))
					{
						continue;
					}
					DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(text2);
					if (lastWriteTimeUtc > dateTime)
					{
						result = text2;
						dateTime = lastWriteTimeUtc;
					}
				}
				else if (targetHero != null && NpcDataFileName.IsDisplayNameSpecified(fileDisplayName) && NpcDataFileName.IsDisplayNameCompatible(fileDisplayName, targetHero?.Name?.ToString(), targetHero != null, strictWhenNameMissing: true))
				{
					DateTime lastWriteTimeUtc2 = File.GetLastWriteTimeUtc(text2);
					if (lastWriteTimeUtc2 > nameMatchedDateTime)
					{
						nameMatchedResult = text2;
						nameMatchedDateTime = lastWriteTimeUtc2;
					}
				}
			}
			return result ?? nameMatchedResult;
		}
		catch
		{
			return null;
		}
	}

	private static string ResolveHeroNameForNpcDataFile(string heroId)
	{
		string id = (heroId ?? "").Trim();
		if (string.IsNullOrEmpty(id))
		{
			return "";
		}
		try
		{
			return Hero.FindFirst((Hero x) => x != null && x.StringId == id)?.Name?.ToString() ?? "";
		}
		catch
		{
			return "";
		}
	}

	private static Hero ResolveHeroByIdForNpcData(string heroId)
	{
		string text = (heroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Hero.Find(text) ?? Hero.FindFirst((Hero x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static Hero ResolveUniqueHeroByNpcFileDisplayName(string fileDisplayName)
	{
		if (!NpcDataFileName.IsDisplayNameSpecified(fileDisplayName))
		{
			return null;
		}
		string text = NpcDataFileName.NormalizeDisplayName(fileDisplayName);
		try
		{
			List<Hero> list = ((IEnumerable<Hero>)Hero.AllAliveHeroes ?? Enumerable.Empty<Hero>())
				.Where((Hero x) => x != null && string.Equals(NpcDataFileName.NormalizeDisplayName(x.Name?.ToString() ?? ""), text, StringComparison.OrdinalIgnoreCase))
				.Take(2)
				.ToList();
			return list.Count == 1 ? list[0] : null;
		}
		catch
		{
			return null;
		}
	}

	private static bool TryResolveNpcDataFileHeroIdForImport(string filePath, out string resolvedHeroId, out string warning)
	{
		resolvedHeroId = "";
		warning = "";
		if (!NpcDataFileName.TryParseParts(filePath, out var parsedHeroId, out var fileDisplayName))
		{
			warning = "文件名缺少 heroId__名字 格式。";
			return false;
		}
		Hero currentHeroById = ResolveHeroByIdForNpcData(parsedHeroId);
		bool autoGeneratedId = NpcDataFileName.IsAutoGeneratedHeroId(parsedHeroId);
		if (currentHeroById != null && NpcDataFileName.IsDisplayNameCompatible(fileDisplayName, currentHeroById?.Name?.ToString(), currentHeroById != null, autoGeneratedId))
		{
			resolvedHeroId = (currentHeroById.StringId ?? parsedHeroId).Trim();
			return !string.IsNullOrWhiteSpace(resolvedHeroId);
		}
		Hero currentHeroByName = ResolveUniqueHeroByNpcFileDisplayName(fileDisplayName);
		if (currentHeroByName != null)
		{
			resolvedHeroId = (currentHeroByName.StringId ?? "").Trim();
			if (!string.Equals(resolvedHeroId, parsedHeroId, StringComparison.OrdinalIgnoreCase))
			{
				Logger.Log("NpcPersona", "[INFO] Remapped NPC data file by unique name: file=" + Path.GetFileName(filePath) + " oldId=" + parsedHeroId + " newId=" + resolvedHeroId + " name=" + (currentHeroByName.Name?.ToString() ?? ""));
			}
			return !string.IsNullOrWhiteSpace(resolvedHeroId);
		}
		if (currentHeroById != null)
		{
			warning = "文件名人物名“" + NpcDataFileName.NormalizeDisplayName(fileDisplayName) + "”与当前同 ID 人物“" + (currentHeroById.Name?.ToString() ?? "") + "”不一致。";
			return false;
		}
		if (autoGeneratedId)
		{
			warning = "自动编号 " + parsedHeroId + " 在当前存档中不可可靠匹配，且文件名人物名不能唯一匹配。";
			return false;
		}
		resolvedHeroId = parsedHeroId;
		return true;
	}

	private static void StampNpcPersonaProfile(string heroId, NpcPersonaProfile profile)
		=> PersonaImportOwner.StampProfileMetadata(heroId, profile, id => ResolveHeroByIdForNpcData(id)?.Name?.ToString());

	private static bool TryPrepareNpcPersonaProfileForWrite(string heroId, NpcPersonaProfile profile)
	{
		if (string.IsNullOrWhiteSpace(heroId) || profile == null)
		{
			return false;
		}
		StampNpcPersonaProfile(heroId, profile);
		return true;
	}

}
