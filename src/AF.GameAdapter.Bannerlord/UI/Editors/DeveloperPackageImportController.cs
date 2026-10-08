using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using static AnimusForge.MyBehavior;
namespace AnimusForge;
internal sealed class DeveloperPackageImportPort
{
 internal Func<long> CaptureGeneration; internal Func<long,bool> IsCurrent;
 internal Func<bool> HasPersonaAuthority, HasHistoryAuthority, HasKingdomSummaryAuthority;
 internal Func<string,bool> ContainsPersona, ContainsHistory, ContainsKingdomSummary;
 internal Func<string> WorldOpeningSummary; internal Func<List<EventRecordEntry>> EventRecords;
 internal Func<string,string> ReadUnnamedPersonaKey; internal Action RefreshVoiceStorage, RefreshUnnamedStorage;
 internal delegate bool ValidateUnnamedPersonaKeysForImportCapability(string importDir, out string error);
 internal ValidateUnnamedPersonaKeysForImportCapability ValidateUnnamedPersonaKeysForImport;
 internal delegate bool ValidateKnowledgeKeywordsForImportCapability(string importDir, bool overwriteExisting, out string error);
 internal ValidateKnowledgeKeywordsForImportCapability ValidateKnowledgeKeywordsForImport;
 internal delegate bool TryResolveNpcDataFileHeroIdForImportCapability(string filePath, out string resolvedHeroId, out string warning);
 internal TryResolveNpcDataFileHeroIdForImportCapability TryResolveNpcDataFileHeroIdForImport;
 internal delegate void StampNpcPersonaProfileCapability(string heroId, NpcPersonaProfile profile);
 internal StampNpcPersonaProfileCapability StampNpcPersonaProfile;
 internal delegate string NormalizeMemoryHeroIdCapability(string heroId);
 internal NormalizeMemoryHeroIdCapability NormalizeMemoryHeroId;
 internal delegate bool HasCompressedMemoryDataForHeroCapability(string heroId);
 internal HasCompressedMemoryDataForHeroCapability HasCompressedMemoryDataForHero;
 internal delegate bool ApplyImportedPersonaProfilesCapability(Dictionary<string,NpcPersonaProfile> imported, bool overwriteExisting, long generation);
 internal ApplyImportedPersonaProfilesCapability ApplyImportedPersonaProfiles;
 internal delegate bool ApplyImportedDialogueHistoryCapability(Dictionary<string,List<DialogueDay>> imported, bool overwriteExisting, long generation);
 internal ApplyImportedDialogueHistoryCapability ApplyImportedDialogueHistory;
 internal delegate bool ApplyCompressedMemoryExportBundleCapability(string heroId, CompressedMemoryExportBundle bundle, bool overwriteExisting);
 internal ApplyCompressedMemoryExportBundleCapability ApplyCompressedMemoryExportBundle;
 internal delegate bool TryLoadEventDataFromImportDirCapability(string importDir, out EventImportPayload payload, out string error);
 internal TryLoadEventDataFromImportDirCapability TryLoadEventDataFromImportDir;
 internal delegate void ApplyImportedEventDataCapability(EventImportPayload payload, bool overwriteExisting);
 internal ApplyImportedEventDataCapability ApplyImportedEventData;
 internal delegate bool ImportKnowledgeFromDirCapability(string importDir, bool overwriteExisting, out string detailMessage);
 internal ImportKnowledgeFromDirCapability ImportKnowledgeFromDir;
 internal delegate void ShowDuplicateImportInquiryCapability(string title, string text, Action onOverwrite, Action onSkipDuplicates, Action onCancel);
 internal ShowDuplicateImportInquiryCapability ShowDuplicateImportInquiry;
}

// Owns complete cross-domain package preparation, confirmation and original partial-success order.
// Prepared dictionaries are detached locals retained only by the current confirmation, never domain authority.
internal sealed class DeveloperPackageImportController
{
 private readonly DeveloperPackageImportPort _port; private readonly DeveloperImportController _execution;
 private readonly PersonaProfileImportExportAdapter _personaFiles;
 private readonly MemoryHistoryImportExportAdapter _memoryFiles;
 private readonly DebtImportExportAdapter _debtFiles;
 private readonly KnowledgeImportExportAdapter _knowledgeFiles;
 private readonly VoicePersonaImportExportAdapter _voiceFiles;
 private readonly WeeklyEventImportExportAdapter _weeklyFiles;
 internal DeveloperPackageImportController(DeveloperPackageImportPort port, DeveloperImportController execution,
  PersonaProfileImportExportAdapter personaFiles, MemoryHistoryImportExportAdapter memoryFiles, DebtImportExportAdapter debtFiles,
  KnowledgeImportExportAdapter knowledgeFiles, VoicePersonaImportExportAdapter voiceFiles, WeeklyEventImportExportAdapter weeklyFiles)
 { _port=port; _execution=execution; _personaFiles=personaFiles; _memoryFiles=memoryFiles; _debtFiles=debtFiles; _knowledgeFiles=knowledgeFiles; _voiceFiles=voiceFiles; _weeklyFiles=weeklyFiles; }
	internal void ImportHeroNpcAllData(string folderName)
	{
		long importGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(importGeneration)) return;
		try
		{
			var lookup = new NpcDataIdentityFileAdapter.LookupScope();
			string importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			if (!_port.ValidateUnnamedPersonaKeysForImport(importDir, out var error))
			{
				InformationManager.DisplayMessage(new InformationMessage(error));
				return;
			}
			Dictionary<string, NpcPersonaProfile> pbNew = null;
			Dictionary<string, CompressedMemoryExportBundle> dhNew = null;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> debtNew = null;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			int invalidMemoryFiles = 0;
			pbNew = _personaFiles.PreparePersonaDirectory(importDir,lookup,out num,out num2);
			dhNew = _memoryFiles.PrepareCompressedDirectory(importDir,out num3,out num4,out invalidMemoryFiles);
			string path3 = Path.Combine(importDir, "debt");
			RewardSystemBehavior rs = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> existDebt = ((rs != null) ? (rs.ExportDebtEntries() ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>()) : null);
			debtNew = _debtFiles.PrepareDebtDirectory(path3,existDebt,out num5,out num6);
			bool flag = num + num3 + num5 > 0;
			Action action = delegate
			{
				if (!_port.IsCurrent(importGeneration)) return;
				if (!_port.ValidateKnowledgeKeywordsForImport(importDir, overwriteExisting: true, out var error2))
				{
					InformationManager.DisplayMessage(new InformationMessage(error2));
				}
				else
				{
					if (pbNew != null)
					{
						if (!_port.ApplyImportedPersonaProfiles(pbNew, true, importGeneration)) return;
					}
					if (dhNew != null)
					{
						_memoryFiles.ApplyCompressedDirectory(dhNew,true);
					}
					if (debtNew != null && rs != null)
					{
						_debtFiles.ApplyPreparedDebt(rs,existDebt,debtNew,true);
					}
					InformationManager.DisplayMessage(new InformationMessage(invalidMemoryFiles > 0 ? "部分导入完成（跳过 " + invalidMemoryFiles + " 个无效压缩记忆文件）：" + importDir : "导入完成：" + importDir));
				}
			};
			Action onSkipDuplicates = delegate
			{
				if (!_port.IsCurrent(importGeneration)) return;
				if (!_port.ValidateKnowledgeKeywordsForImport(importDir, overwriteExisting: false, out var error2))
				{
					InformationManager.DisplayMessage(new InformationMessage(error2));
				}
				else
				{
					if (pbNew != null)
					{
						if (!_port.ApplyImportedPersonaProfiles(pbNew, false, importGeneration)) return;
					}
					if (dhNew != null)
					{
						_memoryFiles.ApplyCompressedDirectory(dhNew,false);
					}
					if (debtNew != null && rs != null)
					{
						_debtFiles.ApplyPreparedDebt(rs,existDebt,debtNew,false);
					}
					InformationManager.DisplayMessage(new InformationMessage(invalidMemoryFiles > 0 ? "部分导入完成（跳过 " + invalidMemoryFiles + " 个无效压缩记忆文件，已跳过重复）：" + importDir : "导入完成（已跳过重复）：" + importDir));
				}
			};
			if (flag)
			{
				string text7 = "导入数据与当前游戏存在重复。\n个性/背景：" + num + "/" + num2 + "\n压缩记忆：" + num3 + "/" + num4 + "\n欠款：" + num5 + "/" + num6 + "\n请选择处理方式：";
				_port.ShowDuplicateImportInquiry("检测到重复 - HeroNPC 全量导入", text7, action, onSkipDuplicates, delegate
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
			if (!_port.IsCurrent(importGeneration)) return;
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	internal void ImportAllData(string folderName)
	{
		long importGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(importGeneration)) return;
		try
		{
			var lookup = new NpcDataIdentityFileAdapter.LookupScope();
			string importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			Dictionary<string, NpcPersonaProfile> pbNew = null;
			Dictionary<string, List<DialogueDay>> dhNew = null;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> debtNew = null;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			int num7 = 0;
			int num8 = 0;
			int num9 = 0;
			int num10 = 0;
			int num11 = 0;
			int num12 = 0;
			int eventWorldDupCount = 0;
			int eventWorldTotalCount = 0;
			int eventKingdomDupCount = 0;
			int eventKingdomTotalCount = 0;
			int eventRecordDupCount = 0;
			int eventRecordTotalCount = 0;
			int kingdomProfileDupCount = 0;
			int kingdomProfileTotalCount = 0;
			int kingdomProfileSkippedCount = 0;
			string kingdomProfileImportError = "";
			KingdomStrategicProfileBehavior kingdomProfileBehavior = KingdomStrategicProfileBehavior.Instance;
			string vmJson = null;
			string vmPath = null;
			EventImportPayload eventPayload = null;
			pbNew = _personaFiles.PreparePersonaDirectory(importDir,lookup,out num,out num2);
			dhNew = _memoryFiles.PrepareRawHistoryDirectory(importDir,out num3,out num4);
			RewardSystemBehavior rs = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> existDebt = ((rs != null) ? (rs.ExportDebtEntries() ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>()) : null);
			string path3 = Path.Combine(importDir, "debt");
			debtNew = _debtFiles.PrepareDebtDirectory(path3,existDebt,out num5,out num6);
			_personaFiles.PrepareUnnamedPersonaCounts(importDir,out num7,out num8);
			_knowledgeFiles.PrepareKnowledgeCounts(importDir,out num9,out num10);
			_voiceFiles.PrepareVoiceMapping(importDir,out vmJson,out vmPath,out num11,out num12);
			_weeklyFiles.PrepareEventPayload(importDir,out eventPayload,out eventWorldDupCount,out eventWorldTotalCount,out eventKingdomDupCount,out eventKingdomTotalCount,out eventRecordDupCount,out eventRecordTotalCount);
			if (kingdomProfileBehavior != null && !kingdomProfileBehavior.InspectImportDirectory(importDir, out kingdomProfileTotalCount, out kingdomProfileDupCount, out kingdomProfileSkippedCount, out kingdomProfileImportError))
			{
				kingdomProfileDupCount = 0;
				kingdomProfileTotalCount = 0;
				kingdomProfileSkippedCount = 0;
			}
			bool flag2 = num + num3 + num5 + num7 + num9 + num11 + eventWorldDupCount + eventKingdomDupCount + eventRecordDupCount + kingdomProfileDupCount > 0;
			DeveloperImportPlan plan = new DeveloperImportPlan
			{
				Persona = overwrite =>
				{
return _port.ApplyImportedPersonaProfiles(pbNew, overwrite, importGeneration);
				},
				Memory = overwrite =>
				{
return _port.ApplyImportedDialogueHistory(dhNew, overwrite, importGeneration);
				},
				Debt = overwrite =>
				{
if (debtNew != null && rs != null) _debtFiles.ApplyPreparedDebt(rs,existDebt,debtNew,overwrite);
				},
				Voice = overwrite =>
				{
_voiceFiles.ApplyPreparedVoice(vmJson,vmPath,overwrite,_port.RefreshVoiceStorage);
				},
				Weekly = overwrite =>
				{
if (eventPayload != null)
				{
					_port.ApplyImportedEventData(eventPayload, overwriteExisting: overwrite);
				}
				},
				Kingdom = overwrite =>
				{
if (kingdomProfileBehavior != null && kingdomProfileTotalCount > 0 && !kingdomProfileBehavior.ImportAllFromDirectory(importDir, overwriteExisting: overwrite, out var kingdomProfileMessage))
				{
					InformationManager.DisplayMessage(new InformationMessage("警告：国家战略与性格导入失败，已跳过。原因：" + kingdomProfileMessage));
				}
				else if (!string.IsNullOrWhiteSpace(kingdomProfileImportError))
				{
					InformationManager.DisplayMessage(new InformationMessage("警告：国家战略与性格导入文件无效，已跳过。原因：" + kingdomProfileImportError));
				}
				},
				UnnamedPersona = overwrite =>
				{
_personaFiles.ApplyUnnamedPersonaDirectory(importDir,overwrite,_port.RefreshUnnamedStorage);
				},
				Knowledge = overwrite =>
				{
if (!_port.ImportKnowledgeFromDir(importDir, overwriteExisting: overwrite, out var knowledgeImportMessage))
				{
					InformationManager.DisplayMessage(new InformationMessage("警告：Knowledge 导入失败，已跳过。原因：" + (string.IsNullOrWhiteSpace(knowledgeImportMessage) ? "未知错误。" : knowledgeImportMessage)));
				}
				else if (!string.IsNullOrWhiteSpace(knowledgeImportMessage))
				{
					InformationManager.DisplayMessage(new InformationMessage(knowledgeImportMessage));
				}
				},
				Completed = overwrite =>
				{
InformationManager.DisplayMessage(new InformationMessage((overwrite ? "导入完成：" : "导入完成（已跳过重复）：") + importDir));
				}
			};
			Action action = () => _execution.Execute(plan, true, importGeneration);
			Action onSkipDuplicates = () => _execution.Execute(plan, false, importGeneration);

			if (flag2)
			{
				string text14 = "导入数据与当前游戏存在重复。\n个性/背景：" + num + "/" + num2 + "\n对话历史：" + num3 + "/" + num4 + "\n欠款：" + num5 + "/" + num6 + "\n未命名NPC：" + num7 + "/" + num8 + "\nKnowledge：" + num9 + "/" + num10 + "\n声音映射：" + num11 + "/" + num12 + "\n世界开局概要：" + eventWorldDupCount + "/" + eventWorldTotalCount + "\n王国开局概要：" + eventKingdomDupCount + "/" + eventKingdomTotalCount + "\n事件记录：" + eventRecordDupCount + "/" + eventRecordTotalCount + "\n国家战略/性格：" + kingdomProfileDupCount + "/" + kingdomProfileTotalCount + "（无法匹配 " + kingdomProfileSkippedCount + "）\n请选择处理方式：";
				_port.ShowDuplicateImportInquiry("检测到重复 - 全部导入", text14, action, onSkipDuplicates, delegate
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
			if (!_port.IsCurrent(importGeneration)) return;
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

}
