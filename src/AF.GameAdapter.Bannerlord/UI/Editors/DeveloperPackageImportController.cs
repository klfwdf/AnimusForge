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
 internal DeveloperPackageImportController(DeveloperPackageImportPort port, DeveloperImportController execution) { _port=port; _execution=execution; }
	internal void ImportHeroNpcAllData(string folderName)
	{
		long importGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(importGeneration)) return;
		try
		{
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
			string path = Path.Combine(importDir, "personality_background");
			if (Directory.Exists(path))
			{
				string[] files = Directory.GetFiles(path, "*.json");
				pbNew = new Dictionary<string, NpcPersonaProfile>();
				string[] array = files;
				foreach (string text in array)
				{
					if (_port.TryResolveNpcDataFileHeroIdForImport(text, out var text2, out var warning))
					{
						NpcPersonaProfile npcPersonaProfile = PlayerExportsStore.ReadJson<NpcPersonaProfile>(text);
						if (npcPersonaProfile != null)
						{
							_port.StampNpcPersonaProfile(text2, npcPersonaProfile);
							pbNew[text2] = npcPersonaProfile;
						}
					}
					else
					{
						Logger.Log("NpcPersona", "[WARN] Skipped persona import file " + Path.GetFileName(text) + ": " + warning);
					}
				}
				num2 = pbNew.Count;
				if (_port.HasPersonaAuthority())
				{
					foreach (string key in pbNew.Keys)
					{
						if (!string.IsNullOrEmpty(key) && _port.ContainsPersona(key))
						{
							num++;
						}
					}
				}
			}
			string path2 = Path.Combine(importDir, "compressed_memory");
			if (Directory.Exists(path2))
			{
				string[] files2 = Directory.GetFiles(path2, "*.json");
				dhNew = new Dictionary<string, CompressedMemoryExportBundle>(StringComparer.OrdinalIgnoreCase);
				string[] array2 = files2;
				foreach (string text3 in array2)
				{
					string text4 = NpcDataFileName.TryParseHeroId(text3);
					if (!string.IsNullOrEmpty(text4))
					{
						if (CompressedMemoryExportBundleReader.TryRead(text3, out var bundle, out var memoryImportError))
						{
							dhNew[_port.NormalizeMemoryHeroId(text4)] = bundle;
						}
						else
						{
							invalidMemoryFiles++;
							Logger.Log("MemoryImport", "[WARN] Skipped " + Path.GetFileName(text3) + ": " + memoryImportError);
						}
					}
					else
					{
						invalidMemoryFiles++;
					}
				}
				num4 = dhNew.Count;
				foreach (string key2 in dhNew.Keys)
				{
					string memoryKey = _port.NormalizeMemoryHeroId(key2);
					if (!string.IsNullOrEmpty(memoryKey) && _port.HasCompressedMemoryDataForHero(memoryKey))
					{
						num3++;
					}
				}
			}
			string path3 = Path.Combine(importDir, "debt");
			RewardSystemBehavior rs = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> existDebt = ((rs != null) ? (rs.ExportDebtEntries() ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>()) : null);
			if (Directory.Exists(path3))
			{
				string[] files3 = Directory.GetFiles(path3, "*.json");
				debtNew = new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
				string[] array3 = files3;
				foreach (string text5 in array3)
				{
					string text6 = NpcDataFileName.TryParseHeroId(text5);
					if (!string.IsNullOrEmpty(text6))
					{
						RewardSystemBehavior.DebtExportEntry debtExportEntry = PlayerExportsStore.ReadJson<RewardSystemBehavior.DebtExportEntry>(text5);
						if (debtExportEntry != null)
						{
							debtNew[text6] = debtExportEntry;
						}
					}
				}
				num6 = debtNew.Count;
				if (existDebt != null)
				{
					foreach (string key3 in debtNew.Keys)
					{
						if (!string.IsNullOrEmpty(key3) && existDebt.ContainsKey(key3))
						{
							num5++;
						}
					}
				}
			}
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
						foreach (KeyValuePair<string, CompressedMemoryExportBundle> item2 in dhNew)
						{
							if (!string.IsNullOrEmpty(item2.Key) && item2.Value != null)
							{
								_port.ApplyCompressedMemoryExportBundle(item2.Key, item2.Value, overwriteExisting: true);
							}
						}
					}
					if (debtNew != null && rs != null)
					{
						rs.ImportDebtEntries(DebtImportMergePolicy.ApplyImportedDebtEntries(existDebt, debtNew, true));
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
						foreach (KeyValuePair<string, CompressedMemoryExportBundle> item5 in dhNew)
						{
							if (!string.IsNullOrEmpty(item5.Key) && item5.Value != null)
							{
								_port.ApplyCompressedMemoryExportBundle(item5.Key, item5.Value, overwriteExisting: false);
							}
						}
					}
					if (debtNew != null && rs != null)
					{
						rs.ImportDebtEntries(DebtImportMergePolicy.ApplyImportedDebtEntries(existDebt, debtNew, false));
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
			string path = Path.Combine(importDir, "personality_background");
			if (Directory.Exists(path))
			{
				string[] files = Directory.GetFiles(path, "*.json");
				pbNew = new Dictionary<string, NpcPersonaProfile>();
				string[] array = files;
				foreach (string text in array)
				{
					if (_port.TryResolveNpcDataFileHeroIdForImport(text, out var text2, out var warning))
					{
						NpcPersonaProfile npcPersonaProfile = PlayerExportsStore.ReadJson<NpcPersonaProfile>(text);
						if (npcPersonaProfile != null)
						{
							_port.StampNpcPersonaProfile(text2, npcPersonaProfile);
							pbNew[text2] = npcPersonaProfile;
						}
					}
					else
					{
						Logger.Log("NpcPersona", "[WARN] Skipped persona import file " + Path.GetFileName(text) + ": " + warning);
					}
				}
				num2 = pbNew.Count;
				if (_port.HasPersonaAuthority())
				{
					foreach (string key in pbNew.Keys)
					{
						if (!string.IsNullOrEmpty(key) && _port.ContainsPersona(key))
						{
							num++;
						}
					}
				}
			}
			string path2 = Path.Combine(importDir, "dialogue_history");
			if (Directory.Exists(path2))
			{
				string[] files2 = Directory.GetFiles(path2, "*.json");
				dhNew = new Dictionary<string, List<DialogueDay>>();
				string[] array2 = files2;
				foreach (string text3 in array2)
				{
					string text4 = NpcDataFileName.TryParseHeroId(text3);
					if (!string.IsNullOrEmpty(text4))
					{
						List<DialogueDay> list = PlayerExportsStore.ReadJson<List<DialogueDay>>(text3);
						if (list != null)
						{
							dhNew[text4] = list;
						}
					}
				}
				num4 = dhNew.Count;
				if (_port.HasHistoryAuthority())
				{
					foreach (string key2 in dhNew.Keys)
					{
						if (!string.IsNullOrEmpty(key2) && _port.ContainsHistory(key2))
						{
							num3++;
						}
					}
				}
			}
			RewardSystemBehavior rs = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> existDebt = ((rs != null) ? (rs.ExportDebtEntries() ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>()) : null);
			string path3 = Path.Combine(importDir, "debt");
			if (Directory.Exists(path3))
			{
				string[] files3 = Directory.GetFiles(path3, "*.json");
				debtNew = new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
				string[] array3 = files3;
				foreach (string text5 in array3)
				{
					string text6 = NpcDataFileName.TryParseHeroId(text5);
					if (!string.IsNullOrEmpty(text6))
					{
						RewardSystemBehavior.DebtExportEntry debtExportEntry = PlayerExportsStore.ReadJson<RewardSystemBehavior.DebtExportEntry>(text5);
						if (debtExportEntry != null)
						{
							debtNew[text6] = debtExportEntry;
						}
					}
				}
				num6 = debtNew.Count;
				if (existDebt != null)
				{
					foreach (string key3 in debtNew.Keys)
					{
						if (!string.IsNullOrEmpty(key3) && existDebt.ContainsKey(key3))
						{
							num5++;
						}
					}
				}
			}
			try
			{
				List<string> source = new List<string>
				{
					Path.Combine(importDir, "unnamed_persona"),
					importDir
				};
				HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (string item in source.Distinct(StringComparer.OrdinalIgnoreCase))
				{
					if (!Directory.Exists(item))
					{
						continue;
					}
					string[] array4 = null;
					try
					{
						array4 = Directory.GetFiles(item, "*.json");
					}
					catch
					{
						array4 = null;
					}
					if (array4 == null)
					{
						continue;
					}
					string[] array5 = array4;
					foreach (string path4 in array5)
					{
						string text7 = null;
						try
						{
							text7 = (_port.ReadUnnamedPersonaKey(path4) ?? "").Trim().ToLower();
						}
						catch
						{
							text7 = null;
						}
						if (string.IsNullOrWhiteSpace(text7))
						{
							string text8 = Path.GetFileNameWithoutExtension(path4) ?? "";
							int num13 = text8.LastIndexOf("__", StringComparison.Ordinal);
							if (num13 > 0)
							{
								text8 = text8.Substring(0, num13);
							}
							text7 = (text8 ?? "").Trim().ToLower();
						}
						if (!string.IsNullOrEmpty(text7) && hashSet.Add(text7))
						{
							num8++;
							if (ShoutUtils.HasUnnamedPersonaKey(text7))
							{
								num7++;
							}
						}
					}
				}
			}
			catch
			{
				num7 = 0;
				num8 = 0;
			}
			try
			{
				KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
				if (knowledgeLibraryBehavior != null)
				{
					HashSet<string> hashSet2 = new HashSet<string>(knowledgeLibraryBehavior.GetRuleIdsForDev(100000) ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
					HashSet<string> hashSet3 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = KnowledgeImportSupport.TryLoadKnowledgeRulesFromRuleFiles(importDir);
					if (knowledgeFile?.Rules != null)
					{
						foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
						{
							string text9 = (rule?.Id ?? "").Trim();
							if (!string.IsNullOrEmpty(text9))
							{
								hashSet3.Add(text9);
							}
						}
					}
					string path5 = Path.Combine(importDir, "knowledge", "KnowledgeRules.json");
					if (hashSet3.Count <= 0 && File.Exists(path5))
					{
						string value = File.ReadAllText(path5, Encoding.UTF8);
						KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value);
						if (knowledgeFile2?.Rules != null)
						{
							foreach (KnowledgeLibraryBehavior.LoreRule rule2 in knowledgeFile2.Rules)
							{
								string text10 = (rule2?.Id ?? "").Trim();
								if (!string.IsNullOrEmpty(text10))
								{
									hashSet3.Add(text10);
								}
							}
						}
					}
					num10 = hashSet3.Count;
					foreach (string item2 in hashSet3)
					{
						if (hashSet2.Contains(item2))
						{
							num9++;
						}
					}
				}
			}
			catch
			{
				num9 = 0;
				num10 = 0;
			}
			try
			{
				string path6 = Path.Combine(importDir, "voice_mapping", "VoiceMapping.json");
				if (!File.Exists(path6))
				{
					path6 = Path.Combine(importDir, "VoiceMapping.json");
				}
				if (File.Exists(path6))
				{
					string text11 = File.ReadAllText(path6, Encoding.UTF8);
					if (!string.IsNullOrWhiteSpace(text11))
					{
						vmPath = path6;
						vmJson = text11;
						num12 = 1;
						bool flag = false;
						try
						{
							flag = VoiceMapper.GetTotalVoiceCount() > 0 || !string.IsNullOrWhiteSpace(VoiceMapper.GetFallbackVoice());
						}
						catch
						{
							flag = false;
						}
						if (flag)
						{
							num11 = 1;
						}
					}
				}
			}
			catch
			{
				num11 = 0;
				num12 = 0;
				vmJson = null;
			}
			try
			{
				if (_port.TryLoadEventDataFromImportDir(importDir, out eventPayload, out var _))
				{
					if (eventPayload.HasWorldSummaryFile)
					{
						eventWorldTotalCount = 1;
						if (!string.IsNullOrWhiteSpace(_port.WorldOpeningSummary()))
						{
							eventWorldDupCount = 1;
						}
					}
					if (eventPayload.HasKingdomSummariesFile)
					{
						eventKingdomTotalCount = eventPayload.KingdomSummaries.Count;
						if (_port.HasKingdomSummaryAuthority())
						{
							foreach (string key4 in eventPayload.KingdomSummaries.Keys)
							{
								if (!string.IsNullOrWhiteSpace(key4) && _port.ContainsKingdomSummary(key4))
								{
									eventKingdomDupCount++;
								}
							}
						}
					}
					if (eventPayload.HasEventRecordsFile)
					{
						eventRecordTotalCount = eventPayload.EventRecords.Count;
						HashSet<string> hashSet4 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
						if (_port.EventRecords() != null)
						{
							foreach (EventRecordEntry eventRecordEntry in _port.EventRecords())
							{
								string text12 = (eventRecordEntry?.EventId ?? "").Trim();
								if (!string.IsNullOrWhiteSpace(text12))
								{
									hashSet4.Add(text12);
								}
							}
						}
						foreach (EventRecordEntry eventRecordEntry2 in eventPayload.EventRecords)
						{
							string text13 = (eventRecordEntry2?.EventId ?? "").Trim();
							if (!string.IsNullOrWhiteSpace(text13) && hashSet4.Contains(text13))
							{
								eventRecordDupCount++;
							}
						}
					}
				}
			}
			catch
			{
				eventWorldDupCount = 0;
				eventWorldTotalCount = 0;
				eventKingdomDupCount = 0;
				eventKingdomTotalCount = 0;
				eventRecordDupCount = 0;
				eventRecordTotalCount = 0;
				eventPayload = null;
			}
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
if (debtNew != null && rs != null) rs.ImportDebtEntries(DebtImportMergePolicy.ApplyImportedDebtEntries(existDebt, debtNew, overwrite));
				},
				Voice = overwrite =>
				{
bool flag3 = true;
				if (!string.IsNullOrWhiteSpace(vmJson))
				{
					flag3 = ((!string.IsNullOrWhiteSpace(vmPath) && File.Exists(vmPath)) ? VoiceMapper.ImportMappingFromFile(vmPath, overwriteExisting: overwrite) : VoiceMapper.ImportMappingJson(vmJson, overwriteExisting: overwrite));
					if (flag3)
					{
						_port.RefreshVoiceStorage();
					}
				}
				if (!flag3)
				{
					InformationManager.DisplayMessage(new InformationMessage("警告：VoiceMapping 导入失败，已跳过。"));
				}
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
ShoutUtils.ImportUnnamedPersonaFromDir(importDir, overwriteExisting: overwrite);
				_port.RefreshUnnamedStorage();
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
