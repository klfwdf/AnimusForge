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

internal static class DatabaseReloadUiText
{
	internal static string T(string id, string englishFallback) => DeveloperUiText.T(id, englishFallback);
	internal static string F(string id, string englishFallback, params string[] variables) => DeveloperUiText.F(id, englishFallback, variables);
}

// One short-lived reload transaction coordinates real domain capabilities. It never retains snapshots or live save tables.
internal sealed class DatabaseReloadController
{
 private readonly DatabaseReloadPort _port;
 internal DatabaseReloadController(DatabaseReloadPort port) { _port = port; }
	private static string T(string id, string englishFallback) => DatabaseReloadUiText.T(id, englishFallback);
	private static string F(string id, string englishFallback, params string[] variables) => DatabaseReloadUiText.F(id, englishFallback, variables);
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
				detail = T("AF_UI_Developer_ReloadPlanInvalid", "The reload plan is invalid.");
				return false;
			}
			knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			kingdomStrategicProfileBehavior = KingdomStrategicProfileBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KingdomStrategicProfileBehavior>();
			string error = "";
			if (knowledgeLibraryBehavior == null || kingdomStrategicProfileBehavior == null || !TryCaptureDatabaseReloadRollbackSnapshot(knowledgeLibraryBehavior, kingdomStrategicProfileBehavior, out databaseReloadRollbackSnapshot, out error))
			{
				detail = string.IsNullOrWhiteSpace(error) ? T("AF_UI_Developer_ReloadSnapshotUnavailable", "The knowledge library or kingdom profile service is unavailable, or a reload rollback snapshot could not be created.") : error;
				return false;
			}
			if (!knowledgeLibraryBehavior.ReplaceDatabaseRulesPreservingPlayer(plan.KnowledgeRules, out var removedCount, out var importedCount, out error))
			{
				detail = string.IsNullOrWhiteSpace(error) ? T("AF_UI_Developer_ReloadKnowledgeValidationFailed", "Knowledge rule validation failed.") : error;
				return false;
			}
			if (!VoiceMapper.ImportMappingJson(plan.VoiceMappingJson, overwriteExisting: true, saveToFile: false))
			{
				return RestoreDatabaseReloadAfterFailure(knowledgeLibraryBehavior, kingdomStrategicProfileBehavior, databaseReloadRollbackSnapshot, T("AF_UI_Developer_ReloadVoiceMappingImportFailed", "Voice mapping import failed."), out detail);
			}
			_port.SetVoiceMappingStorage(VoiceMapper.ExportMappingJson(pretty: false) ?? "");
			int num = _port.ReplaceNpcVoiceAssignments(plan.NpcVoiceIds, out var appliedVoiceIdCount);
			_port.ReplaceOpeningKnowledge(plan.OpeningKnowledge);
			if (!kingdomStrategicProfileBehavior.TryApplyDatabaseReloadPlan(plan.KingdomProfilePlan, out var detailMessage))
			{
				return RestoreDatabaseReloadAfterFailure(knowledgeLibraryBehavior, kingdomStrategicProfileBehavior, databaseReloadRollbackSnapshot, T("AF_UI_Developer_ReloadKingdomProfileImportFailed", "Kingdom personality and strategy import failed: ") + detailMessage, out detail);
			}
			detail = F("AF_UI_Developer_ReloadCompletedDetail", "Knowledge removed {REMOVED}, imported {IMPORTED}{DISAMBIGUATION}{DEDUPLICATION}; NPC voice IDs cleared {CLEARED}, imported {VOICE_IMPORTED}; {KINGDOM}", "REMOVED", removedCount.ToString(), "IMPORTED", importedCount.ToString(), "DISAMBIGUATION", plan.KnowledgeRuleIdDisambiguationCount > 0 ? F("AF_UI_Developer_ReloadStableRenames", " (stable ID renames: {COUNT})", "COUNT", plan.KnowledgeRuleIdDisambiguationCount.ToString()) : "", "DEDUPLICATION", plan.KnowledgeKeywordDeduplicationCount > 0 ? F("AF_UI_Developer_ReloadKeywordDeduplication", " (duplicate trigger keywords removed: {COUNT})", "COUNT", plan.KnowledgeKeywordDeduplicationCount.ToString()) : "", "CLEARED", num.ToString(), "VOICE_IMPORTED", appliedVoiceIdCount.ToString(), "KINGDOM", detailMessage);
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
				error = T("AF_UI_Developer_ReloadServicesUnavailable", "The knowledge library or kingdom profile service is unavailable.");
				return false;
			}
			string knowledgeJson = knowledgeLibraryBehavior.ExportRulesJson(pretty: false);
			if (string.IsNullOrWhiteSpace(knowledgeJson))
			{
				error = T("AF_UI_Developer_ReloadKnowledgeSnapshotExportFailed", "Could not export the current knowledge library snapshot.");
				return false;
			}
			if (!kingdomStrategicProfileBehavior.TryCaptureDatabaseReloadRollbackJson(out string kingdomProfilesJson, out string kingdomError))
			{
				error = T("AF_UI_Developer_ReloadKingdomSnapshotExportFailed", "Could not export the current kingdom profile snapshot: ") + kingdomError;
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
			error = T("AF_UI_Developer_ReloadSnapshotCreateFailed", "Failed to create a rollback snapshot: ") + ex.Message;
			return false;
		}
	}

	internal bool RestoreDatabaseReloadAfterFailure(KnowledgeLibraryBehavior knowledgeLibraryBehavior, KingdomStrategicProfileBehavior kingdomStrategicProfileBehavior, DatabaseReloadRollbackSnapshot snapshot, string failure, out string detail)
	{
		List<string> restoreErrors = new List<string>();
		if (knowledgeLibraryBehavior == null || snapshot == null)
		{
			detail = (failure ?? T("AF_UI_Developer_ReloadFailed", "Reload failed")) + T("AF_UI_Developer_ReloadNoSnapshot", "; no rollback snapshot is available.");
			return false;
		}
		try
		{
			if (!knowledgeLibraryBehavior.ImportRulesJson(snapshot.KnowledgeJson, overwrite: true))
			{
				restoreErrors.Add(T("AF_UI_Developer_ReloadKnowledgeLibrary", "knowledge library"));
			}
		}
		catch (Exception ex)
		{
			restoreErrors.Add(T("AF_UI_Developer_ReloadKnowledgeLibrary", "knowledge library") + "(" + ex.Message + ")");
		}
		try
		{
			if (!VoiceMapper.ImportMappingJson(snapshot.VoiceMappingJson, overwriteExisting: true, saveToFile: false))
			{
				restoreErrors.Add(T("AF_UI_Developer_ReloadVoiceMapping", "voice mapping"));
			}
			else
			{
				_port.SetVoiceMappingStorage(VoiceMapper.ExportMappingJson(pretty: false) ?? "");
			}
		}
		catch (Exception ex2)
		{
			restoreErrors.Add(T("AF_UI_Developer_ReloadVoiceMapping", "voice mapping") + "(" + ex2.Message + ")");
		}
		try
		{
			Dictionary<string, NpcPersonaProfile> restoredProfiles = JsonConvert.DeserializeObject<Dictionary<string, NpcPersonaProfile>>(snapshot.NpcPersonaProfilesJson);
			if (restoredProfiles == null)
			{
				restoreErrors.Add(T("AF_UI_Developer_ReloadNpcVoiceIds", "NPC voice IDs"));
			}
			else
			{
				_port.RestorePersonaProfiles(restoredProfiles);
			}
		}
		catch (Exception ex3)
		{
			restoreErrors.Add(T("AF_UI_Developer_ReloadNpcVoiceIds", "NPC voice IDs") + "(" + ex3.Message + ")");
		}
		try
		{
			string previousWorldWeeklyProductsFingerprint = _port.CaptureWorldWeeklyProductsFingerprint();
			List<EventRecordEntry> restoredRecords = JsonConvert.DeserializeObject<List<EventRecordEntry>>(snapshot.EventRecordsJson);
			if (restoredRecords == null)
			{
				restoreErrors.Add(T("AF_UI_Developer_ReloadOpeningEvents", "opening knowledge and event records"));
			}
			else
			{
				_port.RestoreWeeklyData(snapshot.EventWorldOpeningSummary, snapshot.EventKingdomOpeningSummaries, restoredRecords, previousWorldWeeklyProductsFingerprint);
			}
		}
		catch (Exception ex4)
		{
			restoreErrors.Add(T("AF_UI_Developer_ReloadOpeningEvents", "opening knowledge and event records") + "(" + ex4.Message + ")");
		}
		try
		{
			string kingdomError = "";
			if (kingdomStrategicProfileBehavior == null || !kingdomStrategicProfileBehavior.TryRestoreDatabaseReloadRollbackJson(snapshot.KingdomProfilesJson, out kingdomError))
			{
				restoreErrors.Add(T("AF_UI_Developer_ReloadKingdomProfiles", "kingdom personality and strategy") + (string.IsNullOrWhiteSpace(kingdomError) ? "" : "(" + kingdomError + ")"));
			}
		}
		catch (Exception ex5)
		{
			restoreErrors.Add(T("AF_UI_Developer_ReloadKingdomProfiles", "kingdom personality and strategy") + "(" + ex5.Message + ")");
		}
		if (restoreErrors.Count <= 0)
		{
			detail = (failure ?? T("AF_UI_Developer_ReloadFailed", "Reload failed")) + T("AF_UI_Developer_ReloadRollbackSucceeded", "; the reload was rolled back automatically.");
		}
		else
		{
			detail = (failure ?? T("AF_UI_Developer_ReloadFailed", "Reload failed")) + T("AF_UI_Developer_ReloadRollbackIncomplete", "; automatic rollback was incomplete: ") + string.Join(", ", restoreErrors);
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
	private static string T(string id, string englishFallback) => DatabaseReloadUiText.T(id, englishFallback);
	internal bool TryBuildDatabaseReloadPlan(string folderName, out DatabaseReloadPlan plan, out string error)
	{
		plan = null;
		error = "";
		try
		{
			string text = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrWhiteSpace(text) || !Directory.Exists(text))
			{
				error = T("AF_UI_Developer_ReloadPackageFolderMissing", "The data package folder was not found.");
				return false;
			}
			if (!TryLoadDatabaseReloadKnowledgeRules(text, out List<KnowledgeLibraryBehavior.LoreRule> list, out int knowledgeRuleIdDisambiguationCount, out int knowledgeKeywordDeduplicationCount, out error))
			{
				return false;
			}
			list = list.Where((KnowledgeLibraryBehavior.LoreRule x) => x != null && !KnowledgeLibraryBehavior.IsPlayerPersonaRuleId(x.Id)).ToList();
			if (list.Count <= 0)
			{
				error = T("AF_UI_Developer_ReloadNoKnowledgeRules", "The package contains no non-player knowledge rules eligible for reload.");
				return false;
			}
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			if (knowledgeLibraryBehavior == null || !knowledgeLibraryBehavior.TryValidateDatabaseRulesPreservingPlayer(list, out var _, out var _, out error))
			{
				error = string.IsNullOrWhiteSpace(error) ? T("AF_UI_Developer_ReloadKnowledgeValidationFailed", "Knowledge library is unavailable or knowledge rule validation failed.") : error;
				return false;
			}
			string text2 = Path.Combine(text, "voice_mapping", "VoiceMapping.json");
			if (!File.Exists(text2))
			{
				text2 = Path.Combine(text, "VoiceMapping.json");
			}
			if (!File.Exists(text2) || !TryReadDatabaseReloadUtf8Text(text2, out string text3, out error))
			{
				error = string.IsNullOrWhiteSpace(error) ? T("AF_UI_Developer_ReloadVoiceMappingMissing", "The package does not contain voice_mapping\\VoiceMapping.json.") : error;
				return false;
			}
			if (!TryValidateDatabaseReloadVoiceMapping(text3, out error))
			{
				error = T("AF_UI_Developer_ReloadVoiceMappingInvalid", "VoiceMapping.json cannot be used for reload: ") + error;
				return false;
			}
			if (!TryLoadDatabaseReloadOpeningKnowledge(text, out EventImportPayload eventImportPayload, out error))
			{
				return false;
			}
			KingdomStrategicProfileBehavior kingdomStrategicProfileBehavior = KingdomStrategicProfileBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KingdomStrategicProfileBehavior>();
			if (kingdomStrategicProfileBehavior == null || !kingdomStrategicProfileBehavior.TryBuildDatabaseReloadPlan(text, out KingdomDatabaseReloadPlan kingdomDatabaseReloadPlan, out error))
			{
				error = string.IsNullOrWhiteSpace(error) ? T("AF_UI_Developer_ReloadKingdomServiceUnavailable", "The kingdom personality and strategy service is unavailable.") : error;
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
						error = T("AF_UI_Developer_ReloadVoiceFileMatchFailed", "Could not safely match voice file \u201c") + Path.GetFileName(item) + "\u201d: " + warning;
						return false;
					}
					if (!TryReadDatabaseReloadJson(item, out NpcPersonaProfile npcPersonaProfile, out error))
					{
						error = T("AF_UI_Developer_ReloadVoiceFileReadFailed", "Could not read voice file \u201c") + Path.GetFileName(item) + "\u201d: " + error;
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
						error = T("AF_UI_Developer_ReloadDuplicateNpcVoice", "The package maps multiple voice files to the same NPC: ") + text7;
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
				error = T("AF_UI_Developer_ReloadFileMissing", "The file does not exist.");
				return false;
			}
			text = File.ReadAllText(filePath, DatabaseReloadStrictUtf8);
			return true;
		}
		catch (DecoderFallbackException)
		{
			error = T("AF_UI_Developer_ReloadInvalidUtf8", "The file is not valid UTF-8.");
			return false;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	internal static bool TryReadDatabaseReloadJson<TValue>(string filePath, out TValue value, out string error) where TValue : class
	{
		value = null;
		error = "";
		if (!TryReadDatabaseReloadUtf8Text(filePath, out string text, out error))
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			error = T("AF_UI_Developer_ReloadJsonEmpty", "The JSON file is empty.");
			return false;
		}
		try
		{
			value = JsonConvert.DeserializeObject<TValue>(text);
			if (value == null)
			{
				error = T("AF_UI_Developer_ReloadJsonInvalid", "The JSON content is empty or has an invalid structure.");
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = T("AF_UI_Developer_ReloadJsonParseFailed", "JSON parsing failed: ") + ex.Message;
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
				error = T("AF_UI_Developer_ReloadFileEmpty", "The file is empty.");
				return false;
			}
			JObject root = JObject.Parse(json);
			foreach (string groupKey in VoiceMapper.AllGroupKeys)
			{
				JToken groupToken = root[groupKey];
				if (groupToken == null)
				{
				error = T("AF_UI_Developer_ReloadVoiceGroupMissing", "Voice group is missing: ") + groupKey;
					return false;
				}
				if (groupToken.Type != JTokenType.Array)
				{
				error = T("AF_UI_Developer_ReloadVoiceGroupNotArray", "Voice group \u201c") + groupKey + "\u201d must be an array.";
					return false;
				}
				HashSet<string> voiceIds = new HashSet<string>(StringComparer.Ordinal);
				foreach (JToken voiceToken in groupToken.Children())
				{
					if (voiceToken.Type != JTokenType.String)
					{
						error = T("AF_UI_Developer_ReloadVoiceGroupNonString", "Voice group \u201c") + groupKey + "\u201d contains a non-string voice ID.";
						return false;
					}
					string voiceId = (voiceToken.ToString() ?? "").Trim();
					if (string.IsNullOrWhiteSpace(voiceId) || !voiceIds.Add(voiceId))
					{
						error = T("AF_UI_Developer_ReloadVoiceGroupDuplicate", "Voice group \u201c") + groupKey + "\u201d contains an empty or duplicate voice ID.";
						return false;
					}
				}
			}
			JToken fallbackToken = root["fallback"];
			if (fallbackToken == null || fallbackToken.Type != JTokenType.String)
			{
				error = T("AF_UI_Developer_ReloadVoiceFallbackInvalid", "fallback must exist and be a string.");
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = T("AF_UI_Developer_ReloadJsonParseFailed", "JSON parsing failed: ") + ex.Message;
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
			error = T("AF_UI_Developer_ReloadKnowledgePackageMissing", "The knowledge package directory does not exist.");
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
					error = T("AF_UI_Developer_ReloadKnowledgeFileReadFailed", "Could not read knowledge file \u201c") + fileName + "\u201d: " + readError;
					return false;
				}
				if (!KnowledgeImportValidationOwner.TryAddImportedKnowledgeRule(rule, fileName, ruleIds, keywordOwnerOriginalRuleIds, rules, ref disambiguatedRuleIdCount, ref deduplicatedKeywordCount, out error))
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
				error = T("AF_UI_Developer_ReloadKnowledgeAggregateReadFailed", "Could not read the knowledge aggregate file: ") + aggregateError;
				return false;
			}
			if (aggregate.Rules == null)
			{
				error = T("AF_UI_Developer_ReloadKnowledgeAggregateRulesMissing", "The knowledge aggregate file has no Rules list.");
				return false;
			}
			foreach (KnowledgeLibraryBehavior.LoreRule rule2 in aggregate.Rules)
			{
				if (rule2 == null)
				{
					error = T("AF_UI_Developer_ReloadKnowledgeAggregateEmptyRule", "The knowledge aggregate file contains an empty rule entry.");
					return false;
				}
				if (!KnowledgeImportValidationOwner.TryAddImportedKnowledgeRule(rule2, "KnowledgeRules_" + (rules.Count + 1), ruleIds, keywordOwnerOriginalRuleIds, rules, ref disambiguatedRuleIdCount, ref deduplicatedKeywordCount, out error))
				{
					return false;
				}
			}
		}
		if (sourceFileCount <= 0 || rules.Count <= 0)
		{
			error = T("AF_UI_Developer_ReloadKnowledgePackageRulesMissing", "The package contains neither knowledge\\rules files nor knowledge\\KnowledgeRules.json.");
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
			error = T("AF_UI_Developer_ReloadOpeningFilesMissing", "The package must contain both event_data\\WorldOpeningSummary.json and event_data\\KingdomOpeningSummaries.json.");
			return false;
		}
		if (!TryReadDatabaseReloadJson(worldPath, out OpeningSummarySourceJson worldSummary, out string worldError))
		{
			error = T("AF_UI_Developer_ReloadWorldOpeningReadFailed", "Could not read world opening knowledge: ") + worldError;
			return false;
		}
		if (!TryReadDatabaseReloadJson(kingdomsPath, out Dictionary<string, string> kingdomSummaries, out string kingdomsError))
		{
			error = T("AF_UI_Developer_ReloadKingdomOpeningReadFailed", "Could not read kingdom opening knowledge: ") + kingdomsError;
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
				error = T("AF_UI_Developer_ReloadEmptyKingdomId", "Kingdom opening knowledge contains an empty kingdom ID.");
				return false;
			}
			if (payload.KingdomSummaries.ContainsKey(kingdomId))
			{
				error = T("AF_UI_Developer_ReloadDuplicateKingdomId", "Kingdom opening knowledge contains a duplicate kingdom ID: ") + kingdomId;
				return false;
			}
			payload.KingdomSummaries[kingdomId] = (kingdomSummary.Value ?? "").Trim();
		}
		return true;
	}

 internal sealed class OpeningSummarySourceJson { public string Summary; }
}
