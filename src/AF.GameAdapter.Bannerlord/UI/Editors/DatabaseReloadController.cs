using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using static AnimusForge.MyBehavior;

namespace AnimusForge;

internal sealed class DatabaseReloadPort
{
 internal Func<string> ExportPersonaProfilesJson, GetWorldOpeningSummary, ExportEventRecordsJson, CaptureWorldWeeklyProductsFingerprint;
 internal Func<Dictionary<string,string>> GetKingdomOpeningSummaries;
 internal Action<Dictionary<string,NpcPersonaProfile>> RestorePersonaProfiles;
 internal Action<string, Dictionary<string,string>, List<EventRecordEntry>, string> RestoreWeeklyData;
 internal Action<EventImportPayload> ReplaceOpeningKnowledge;
 internal Action<string> SetVoiceMappingStorage;
 internal delegate int ReplaceNpcVoiceCapability(Dictionary<string,string> source, out int appliedCount);
 internal ReplaceNpcVoiceCapability ReplaceNpcVoiceAssignments;
}

// One short-lived reload transaction coordinates real domain capabilities. It never retains snapshots or live save tables.
internal sealed class DatabaseReloadController
{
 private readonly DatabaseReloadPort _port;
 internal DatabaseReloadController(DatabaseReloadPort port) { _port = port; }
	internal bool ApplyDatabaseReloadPlan(DatabaseReloadPlan plan, out string detail)
	{
		detail = "";
		KnowledgeLibraryBehavior knowledgeLibraryBehavior = null;
		KingdomStrategicProfileBehavior kingdomStrategicProfileBehavior = null;
		DatabaseReloadRollbackSnapshot databaseReloadRollbackSnapshot = null;
		try
		{
			if (plan == null || string.IsNullOrWhiteSpace(plan.ImportDirectory))
			{
				detail = "重载计划无效。";
				return false;
			}
			knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			kingdomStrategicProfileBehavior = KingdomStrategicProfileBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KingdomStrategicProfileBehavior>();
			string error = "";
			if (knowledgeLibraryBehavior == null || kingdomStrategicProfileBehavior == null || !TryCaptureDatabaseReloadRollbackSnapshot(knowledgeLibraryBehavior, kingdomStrategicProfileBehavior, out databaseReloadRollbackSnapshot, out error))
			{
				detail = string.IsNullOrWhiteSpace(error) ? "知识库、王国资料行为未初始化或无法创建重载回滚快照。" : error;
				return false;
			}
			if (!knowledgeLibraryBehavior.ReplaceDatabaseRulesPreservingPlayer(plan.KnowledgeRules, out var removedCount, out var importedCount, out error))
			{
				detail = string.IsNullOrWhiteSpace(error) ? "知识规则校验失败。" : error;
				return false;
			}
			if (!VoiceMapper.ImportMappingJson(plan.VoiceMappingJson, overwriteExisting: true, saveToFile: false))
			{
				return RestoreDatabaseReloadAfterFailure(knowledgeLibraryBehavior, kingdomStrategicProfileBehavior, databaseReloadRollbackSnapshot, "声音映射导入失败。", out detail);
			}
			_port.SetVoiceMappingStorage(VoiceMapper.ExportMappingJson(pretty: false) ?? "");
			int num = _port.ReplaceNpcVoiceAssignments(plan.NpcVoiceIds, out var appliedVoiceIdCount);
			_port.ReplaceOpeningKnowledge(plan.OpeningKnowledge);
			if (!kingdomStrategicProfileBehavior.TryApplyDatabaseReloadPlan(plan.KingdomProfilePlan, out var detailMessage))
			{
				return RestoreDatabaseReloadAfterFailure(knowledgeLibraryBehavior, kingdomStrategicProfileBehavior, databaseReloadRollbackSnapshot, "王国性格与战略导入失败：" + detailMessage, out detail);
			}
			detail = "知识删除 " + removedCount + " 条、导入 " + importedCount + " 条" + ((plan.KnowledgeRuleIdDisambiguationCount > 0) ? ("（同 ID 稳定重命名 " + plan.KnowledgeRuleIdDisambiguationCount + " 条）") : "") + ((plan.KnowledgeKeywordDeduplicationCount > 0) ? ("（重复触发词去重 " + plan.KnowledgeKeywordDeduplicationCount + " 个）") : "") + "；NPC 声音 ID 清除 " + num + " 条、导入 " + appliedVoiceIdCount + " 条；" + detailMessage;
			Logger.Log("DatabaseReload", "completed importDir=" + plan.ImportDirectory + " knowledgeRemoved=" + removedCount + " knowledgeImported=" + importedCount + " knowledgeRuleIdsDisambiguated=" + plan.KnowledgeRuleIdDisambiguationCount + " knowledgeKeywordsDeduplicated=" + plan.KnowledgeKeywordDeduplicationCount + " npcVoiceCleared=" + num + " npcVoiceImported=" + appliedVoiceIdCount + " kingdomProfiles=" + (plan.KingdomProfilePlan?.SourceProfilesByTargetId?.Count ?? 0));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("DatabaseReload", "[ERROR] Apply failed: " + ex);
			if (knowledgeLibraryBehavior != null && databaseReloadRollbackSnapshot != null)
			{
				return RestoreDatabaseReloadAfterFailure(knowledgeLibraryBehavior, kingdomStrategicProfileBehavior, databaseReloadRollbackSnapshot, ex.Message, out detail);
			}
			detail = ex.Message;
			return false;
		}
	}

	internal bool TryCaptureDatabaseReloadRollbackSnapshot(KnowledgeLibraryBehavior knowledgeLibraryBehavior, KingdomStrategicProfileBehavior kingdomStrategicProfileBehavior, out DatabaseReloadRollbackSnapshot snapshot, out string error)
	{
		snapshot = null;
		error = "";
		try
		{
			if (knowledgeLibraryBehavior == null || kingdomStrategicProfileBehavior == null)
			{
				error = "知识库或王国资料行为未初始化。";
				return false;
			}
			string knowledgeJson = knowledgeLibraryBehavior.ExportRulesJson(pretty: false);
			if (string.IsNullOrWhiteSpace(knowledgeJson))
			{
				error = "无法导出当前知识库快照。";
				return false;
			}
			if (!kingdomStrategicProfileBehavior.TryCaptureDatabaseReloadRollbackJson(out string kingdomProfilesJson, out string kingdomError))
			{
				error = "无法导出当前王国资料快照：" + kingdomError;
				return false;
			}
			snapshot = new DatabaseReloadRollbackSnapshot
			{
				KnowledgeJson = knowledgeJson,
				VoiceMappingJson = VoiceMapper.ExportMappingJson(pretty: false) ?? "",
				NpcPersonaProfilesJson = _port.ExportPersonaProfilesJson(),
				EventWorldOpeningSummary = _port.GetWorldOpeningSummary() ?? "",
				EventRecordsJson = _port.ExportEventRecordsJson(),
				KingdomProfilesJson = kingdomProfilesJson
			};
			foreach (KeyValuePair<string, string> summary in _port.GetKingdomOpeningSummaries() ?? new Dictionary<string, string>())
			{
				snapshot.EventKingdomOpeningSummaries[summary.Key ?? ""] = summary.Value ?? "";
			}
			return true;
		}
		catch (Exception ex)
		{
			snapshot = null;
			error = "创建回滚快照失败：" + ex.Message;
			return false;
		}
	}

	internal bool RestoreDatabaseReloadAfterFailure(KnowledgeLibraryBehavior knowledgeLibraryBehavior, KingdomStrategicProfileBehavior kingdomStrategicProfileBehavior, DatabaseReloadRollbackSnapshot snapshot, string failure, out string detail)
	{
		List<string> restoreErrors = new List<string>();
		if (knowledgeLibraryBehavior == null || snapshot == null)
		{
			detail = (failure ?? "重载失败") + "；缺少回滚快照。";
			return false;
		}
		try
		{
			if (!knowledgeLibraryBehavior.ImportRulesJson(snapshot.KnowledgeJson, overwrite: true))
			{
				restoreErrors.Add("知识库");
			}
		}
		catch (Exception ex)
		{
			restoreErrors.Add("知识库(" + ex.Message + ")");
		}
		try
		{
			if (!VoiceMapper.ImportMappingJson(snapshot.VoiceMappingJson, overwriteExisting: true, saveToFile: false))
			{
				restoreErrors.Add("声音映射");
			}
			else
			{
				_port.SetVoiceMappingStorage(VoiceMapper.ExportMappingJson(pretty: false) ?? "");
			}
		}
		catch (Exception ex2)
		{
			restoreErrors.Add("声音映射(" + ex2.Message + ")");
		}
		try
		{
			Dictionary<string, NpcPersonaProfile> restoredProfiles = JsonConvert.DeserializeObject<Dictionary<string, NpcPersonaProfile>>(snapshot.NpcPersonaProfilesJson);
			if (restoredProfiles == null)
			{
				restoreErrors.Add("NPC 声音 ID");
			}
			else
			{
				_port.RestorePersonaProfiles(restoredProfiles);
			}
		}
		catch (Exception ex3)
		{
			restoreErrors.Add("NPC 声音 ID(" + ex3.Message + ")");
		}
		try
		{
			string previousWorldWeeklyProductsFingerprint = _port.CaptureWorldWeeklyProductsFingerprint();
			List<EventRecordEntry> restoredRecords = JsonConvert.DeserializeObject<List<EventRecordEntry>>(snapshot.EventRecordsJson);
			if (restoredRecords == null)
			{
				restoreErrors.Add("开局知识和事件记录");
			}
			else
			{
				_port.RestoreWeeklyData(snapshot.EventWorldOpeningSummary, snapshot.EventKingdomOpeningSummaries, restoredRecords, previousWorldWeeklyProductsFingerprint);
			}
		}
		catch (Exception ex4)
		{
			restoreErrors.Add("开局知识和事件记录(" + ex4.Message + ")");
		}
		try
		{
			string kingdomError = "";
			if (kingdomStrategicProfileBehavior == null || !kingdomStrategicProfileBehavior.TryRestoreDatabaseReloadRollbackJson(snapshot.KingdomProfilesJson, out kingdomError))
			{
				restoreErrors.Add("王国性格与战略" + (string.IsNullOrWhiteSpace(kingdomError) ? "" : "(" + kingdomError + ")"));
			}
		}
		catch (Exception ex5)
		{
			restoreErrors.Add("王国性格与战略(" + ex5.Message + ")");
		}
		if (restoreErrors.Count <= 0)
		{
			detail = (failure ?? "重载失败") + "；已自动回滚本次重载。";
		}
		else
		{
			detail = (failure ?? "重载失败") + "；自动回滚不完整：" + string.Join("、", restoreErrors);
		}
		Logger.Log("DatabaseReload", "[WARN] Reload failed and rollback was attempted: " + detail);
		return false;
	}

}

internal sealed class DatabaseReloadPreflight
{
 internal delegate bool ResolveNpcFileCapability(string file, out string heroId, out string warning);
 private readonly ResolveNpcFileCapability _resolveNpcFile;
 internal DatabaseReloadPreflight(ResolveNpcFileCapability resolveNpcFile) { _resolveNpcFile = resolveNpcFile; }
 private static readonly UTF8Encoding DatabaseReloadStrictUtf8 = new UTF8Encoding(false, true);
	internal bool TryBuildDatabaseReloadPlan(string folderName, out DatabaseReloadPlan plan, out string error)
	{
		plan = null;
		error = "";
		try
		{
			string text = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrWhiteSpace(text) || !Directory.Exists(text))
			{
				error = "找不到资料包文件夹。";
				return false;
			}
			if (!TryLoadDatabaseReloadKnowledgeRules(text, out List<KnowledgeLibraryBehavior.LoreRule> list, out int knowledgeRuleIdDisambiguationCount, out int knowledgeKeywordDeduplicationCount, out error))
			{
				return false;
			}
			list = list.Where((KnowledgeLibraryBehavior.LoreRule x) => x != null && !KnowledgeLibraryBehavior.IsPlayerPersonaRuleId(x.Id)).ToList();
			if (list.Count <= 0)
			{
				error = "资料包中未找到可重载的非主角知识规则。";
				return false;
			}
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			if (knowledgeLibraryBehavior == null || !knowledgeLibraryBehavior.TryValidateDatabaseRulesPreservingPlayer(list, out var _, out var _, out error))
			{
				error = string.IsNullOrWhiteSpace(error) ? "知识库未初始化或知识规则校验失败。" : error;
				return false;
			}
			string text2 = Path.Combine(text, "voice_mapping", "VoiceMapping.json");
			if (!File.Exists(text2))
			{
				text2 = Path.Combine(text, "VoiceMapping.json");
			}
			if (!File.Exists(text2) || !TryReadDatabaseReloadUtf8Text(text2, out string text3, out error))
			{
				error = string.IsNullOrWhiteSpace(error) ? "资料包中缺少 voice_mapping\\VoiceMapping.json。" : error;
				return false;
			}
			if (!TryValidateDatabaseReloadVoiceMapping(text3, out error))
			{
				error = "VoiceMapping.json 无法用于重载：" + error;
				return false;
			}
			if (!TryLoadDatabaseReloadOpeningKnowledge(text, out EventImportPayload eventImportPayload, out error))
			{
				return false;
			}
			KingdomStrategicProfileBehavior kingdomStrategicProfileBehavior = KingdomStrategicProfileBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KingdomStrategicProfileBehavior>();
			if (kingdomStrategicProfileBehavior == null || !kingdomStrategicProfileBehavior.TryBuildDatabaseReloadPlan(text, out KingdomDatabaseReloadPlan kingdomDatabaseReloadPlan, out error))
			{
				error = string.IsNullOrWhiteSpace(error) ? "王国性格与战略数据行为尚未初始化。" : error;
				return false;
			}
			DatabaseReloadPlan databaseReloadPlan = new DatabaseReloadPlan
			{
				ImportDirectory = text,
				KnowledgeRules = list,
				KnowledgeRuleIdDisambiguationCount = knowledgeRuleIdDisambiguationCount,
				KnowledgeKeywordDeduplicationCount = knowledgeKeywordDeduplicationCount,
				VoiceMappingJson = text3,
				OpeningKnowledge = eventImportPayload,
				KingdomProfilePlan = kingdomDatabaseReloadPlan,
				CurrentKingdomProfilesResetToDefaultCount = kingdomDatabaseReloadPlan.CurrentProfilesResetToDefaultCount
			};
			string text4 = Path.Combine(text, "personality_background");
			string text5 = (Hero.MainHero?.StringId ?? "").Trim();
			if (Directory.Exists(text4))
			{
				// Persona files contribute VoiceId only; strict parsing guarantees a damaged file cannot silently erase a current voice assignment.
				foreach (string item in Directory.GetFiles(text4, "*.json", SearchOption.TopDirectoryOnly).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase))
				{
					if (!_resolveNpcFile(item, out string resolvedHeroId, out string warning))
					{
						error = "无法安全匹配声音文件 “" + Path.GetFileName(item) + "”：" + warning;
						return false;
					}
					if (!TryReadDatabaseReloadJson(item, out NpcPersonaProfile npcPersonaProfile, out error))
					{
						error = "无法读取声音文件 “" + Path.GetFileName(item) + "”：" + error;
						return false;
					}
					string text6 = (npcPersonaProfile.VoiceId ?? "").Trim();
					string text7 = (resolvedHeroId ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text6) || string.IsNullOrWhiteSpace(text7) || string.Equals(text7, text5, StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					if (databaseReloadPlan.NpcVoiceIds.ContainsKey(text7))
					{
						error = "资料包中有多个声音文件映射到同一 NPC：" + text7;
						return false;
					}
					databaseReloadPlan.NpcVoiceIds[text7] = text6;
				}
			}
			plan = databaseReloadPlan;
			return true;
		}
		catch (Exception ex2)
		{
			error = ex2.Message;
			Logger.Log("DatabaseReload", "[WARN] Source preflight failed: " + ex2);
			return false;
		}
	}

	internal static bool TryReadDatabaseReloadUtf8Text(string filePath, out string text, out string error)
	{
		text = "";
		error = "";
		try
		{
			if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
			{
				error = "文件不存在。";
				return false;
			}
			text = File.ReadAllText(filePath, DatabaseReloadStrictUtf8);
			return true;
		}
		catch (DecoderFallbackException)
		{
			error = "文件不是有效的 UTF-8 编码。";
			return false;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	internal static bool TryReadDatabaseReloadJson<T>(string filePath, out T value, out string error) where T : class
	{
		value = null;
		error = "";
		if (!TryReadDatabaseReloadUtf8Text(filePath, out string text, out error))
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			error = "JSON 文件为空。";
			return false;
		}
		try
		{
			value = JsonConvert.DeserializeObject<T>(text);
			if (value == null)
			{
				error = "JSON 内容为空或结构无效。";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "JSON 解析失败：" + ex.Message;
			return false;
		}
	}

	internal static bool TryValidateDatabaseReloadVoiceMapping(string json, out string error)
	{
		error = "";
		try
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				error = "文件为空。";
				return false;
			}
			JObject root = JObject.Parse(json);
			foreach (string groupKey in VoiceMapper.AllGroupKeys)
			{
				JToken groupToken = root[groupKey];
				if (groupToken == null)
				{
					error = "缺少声音分组：" + groupKey;
					return false;
				}
				if (groupToken.Type != JTokenType.Array)
				{
					error = "声音分组 “" + groupKey + "” 必须是数组。";
					return false;
				}
				HashSet<string> voiceIds = new HashSet<string>(StringComparer.Ordinal);
				foreach (JToken voiceToken in groupToken.Children())
				{
					if (voiceToken.Type != JTokenType.String)
					{
						error = "声音分组 “" + groupKey + "” 包含非字符串声音 ID。";
						return false;
					}
					string voiceId = (voiceToken.ToString() ?? "").Trim();
					if (string.IsNullOrWhiteSpace(voiceId) || !voiceIds.Add(voiceId))
					{
						error = "声音分组 “" + groupKey + "” 包含空或重复声音 ID。";
						return false;
					}
				}
			}
			JToken fallbackToken = root["fallback"];
			if (fallbackToken == null || fallbackToken.Type != JTokenType.String)
			{
				error = "fallback 必须存在且为字符串。";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "JSON 解析失败：" + ex.Message;
			return false;
		}
	}

	internal static bool TryLoadDatabaseReloadKnowledgeRules(string importDir, out List<KnowledgeLibraryBehavior.LoreRule> rules, out int disambiguatedRuleIdCount, out int deduplicatedKeywordCount, out string error)
	{
		rules = new List<KnowledgeLibraryBehavior.LoreRule>();
		disambiguatedRuleIdCount = 0;
		deduplicatedKeywordCount = 0;
		error = "";
		if (string.IsNullOrWhiteSpace(importDir) || !Directory.Exists(importDir))
		{
			error = "知识资料包目录不存在。";
			return false;
		}
		HashSet<string> ruleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> keywordOwnerOriginalRuleIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		List<string> ruleDirectories = new List<string>
		{
			Path.Combine(importDir, "knowledge", "rules"),
			Path.Combine(importDir, "knowledge", "single_rules"),
			Path.Combine(importDir, "knowledge")
		};
		int sourceFileCount = 0;
		foreach (string ruleDirectory in ruleDirectories.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
		{
			if (!Directory.Exists(ruleDirectory))
			{
				continue;
			}
			foreach (string filePath in Directory.GetFiles(ruleDirectory, "*.json", SearchOption.TopDirectoryOnly).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase))
			{
				string fileName = Path.GetFileName(filePath) ?? "";
				// AIConfig is deliberately excluded: database reload must never change API configuration or any prompt-related configuration.
				if (string.Equals(fileName, "AIConfig.json", StringComparison.OrdinalIgnoreCase) || string.Equals(fileName, "KnowledgeRules.json", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				sourceFileCount++;
				if (!TryReadDatabaseReloadJson(filePath, out KnowledgeLibraryBehavior.LoreRule rule, out string readError))
				{
					error = "知识文件 “" + fileName + "” 无法读取：" + readError;
					return false;
				}
				if (!KnowledgeImportValidationOwner.TryAddDatabaseReloadKnowledgeRule(rule, fileName, ruleIds, keywordOwnerOriginalRuleIds, rules, ref disambiguatedRuleIdCount, ref deduplicatedKeywordCount, out error))
				{
					return false;
				}
			}
		}
		string aggregatePath = Path.Combine(importDir, "knowledge", "KnowledgeRules.json");
		if (File.Exists(aggregatePath))
		{
			sourceFileCount++;
			if (!TryReadDatabaseReloadJson(aggregatePath, out KnowledgeLibraryBehavior.KnowledgeFile aggregate, out string aggregateError))
			{
				error = "知识汇总文件无法读取：" + aggregateError;
				return false;
			}
			if (aggregate.Rules == null)
			{
				error = "知识汇总文件缺少 Rules 列表。";
				return false;
			}
			foreach (KnowledgeLibraryBehavior.LoreRule rule2 in aggregate.Rules)
			{
				if (rule2 == null)
				{
					error = "知识汇总文件包含空知识条目。";
					return false;
				}
				if (!KnowledgeImportValidationOwner.TryAddDatabaseReloadKnowledgeRule(rule2, "KnowledgeRules_" + (rules.Count + 1), ruleIds, keywordOwnerOriginalRuleIds, rules, ref disambiguatedRuleIdCount, ref deduplicatedKeywordCount, out error))
				{
					return false;
				}
			}
		}
		if (sourceFileCount <= 0 || rules.Count <= 0)
		{
			error = "资料包中未找到 knowledge\\rules 下的知识文件或 knowledge\\KnowledgeRules.json。";
			return false;
		}
		return true;
	}



	internal static bool TryLoadDatabaseReloadOpeningKnowledge(string importDir, out EventImportPayload payload, out string error)
	{
		payload = new EventImportPayload();
		error = "";
		string eventDirectory = Path.Combine(importDir ?? "", "event_data");
		if (!Directory.Exists(eventDirectory))
		{
			eventDirectory = importDir ?? "";
		}
		string worldPath = Path.Combine(eventDirectory, "WorldOpeningSummary.json");
		string kingdomsPath = Path.Combine(eventDirectory, "KingdomOpeningSummaries.json");
		if (!File.Exists(worldPath) || !File.Exists(kingdomsPath))
		{
			error = "资料包必须同时包含 event_data\\WorldOpeningSummary.json 与 event_data\\KingdomOpeningSummaries.json。";
			return false;
		}
		if (!TryReadDatabaseReloadJson(worldPath, out OpeningSummarySourceJson worldSummary, out string worldError))
		{
			error = "世界开局知识无法读取：" + worldError;
			return false;
		}
		if (!TryReadDatabaseReloadJson(kingdomsPath, out Dictionary<string, string> kingdomSummaries, out string kingdomsError))
		{
			error = "王国开局知识无法读取：" + kingdomsError;
			return false;
		}
		payload.HasWorldSummaryFile = true;
		payload.WorldSummary = (worldSummary.Summary ?? "").Trim();
		payload.HasKingdomSummariesFile = true;
		foreach (KeyValuePair<string, string> kingdomSummary in kingdomSummaries)
		{
			string kingdomId = (kingdomSummary.Key ?? "").Trim();
			if (string.IsNullOrWhiteSpace(kingdomId))
			{
				error = "王国开局知识包含空王国 ID。";
				return false;
			}
			if (payload.KingdomSummaries.ContainsKey(kingdomId))
			{
				error = "王国开局知识包含重复王国 ID：" + kingdomId;
				return false;
			}
			payload.KingdomSummaries[kingdomId] = (kingdomSummary.Value ?? "").Trim();
		}
		return true;
	}

 internal sealed class OpeningSummarySourceJson { public string Summary; }
}
