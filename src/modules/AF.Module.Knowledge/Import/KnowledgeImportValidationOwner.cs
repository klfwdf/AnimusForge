using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class KnowledgeImportValidationOwner
{
	internal static string NormalizeKeywordForCompare(string keyword)
	{
		try
		{
			string text = (keyword ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder(text.Length);
			bool flag = false;
			foreach (char c in text)
			{
				if (char.IsWhiteSpace(c))
				{
					if (!flag)
					{
						stringBuilder.Append(' ');
					}
					flag = true;
				}
				else
				{
					stringBuilder.Append(c);
					flag = false;
				}
			}
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	internal static bool ValidateKnowledgeKeywordsForSingleRuleImport(KnowledgeLibraryBehavior kb, KnowledgeLibraryBehavior.LoreRule rule, bool overwriteExisting, out string error)
	{
		error = "";
		try
		{
			if (kb == null)
			{
				error = "导入失败：KnowledgeLibraryBehavior 未初始化。";
				return false;
			}
			if (rule == null)
			{
				error = "导入失败：规则为空。";
				return false;
			}
			string text = (rule.Id ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				error = "导入失败：RuleId 为空。";
				return false;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (rule.Keywords != null)
			{
				foreach (string keyword in rule.Keywords)
				{
					string text2 = NormalizeKeywordForCompare(keyword);
					if (!string.IsNullOrEmpty(text2))
					{
						if (dictionary.ContainsKey(text2))
						{
							error = "导入失败：导入规则中存在重复关键词（" + text2 + "）。";
							return false;
						}
						dictionary[text2] = text;
					}
				}
			}
			KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = null;
			try
			{
				string value = kb.ExportRulesJson();
				if (!string.IsNullOrWhiteSpace(value))
				{
					knowledgeFile = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value);
				}
			}
			catch
			{
				knowledgeFile = null;
			}
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (knowledgeFile?.Rules != null)
			{
				foreach (KnowledgeLibraryBehavior.LoreRule rule2 in knowledgeFile.Rules)
				{
					string text3 = (rule2?.Id ?? "").Trim();
					if (string.IsNullOrEmpty(text3) || (overwriteExisting && string.Equals(text3, text, StringComparison.OrdinalIgnoreCase)))
					{
						continue;
					}
					foreach (string item in KnowledgeImportSupport.GetKnowledgeKeywordsForCompare(rule2))
					{
						if (dictionary2.TryGetValue(item, out var value2) && !string.Equals(value2, text3, StringComparison.OrdinalIgnoreCase))
						{
							error = "导入失败：当前存档中已存在重复关键词（" + item + "），请先在游戏内修复后再导入。";
							return false;
						}
						dictionary2[item] = text3;
					}
				}
			}
			foreach (KeyValuePair<string, string> item2 in dictionary)
			{
				if (dictionary2.TryGetValue(item2.Key, out var value3) && !string.Equals(value3, text, StringComparison.OrdinalIgnoreCase))
				{
					error = "导入失败：关键词冲突（" + item2.Key + "）。当前存档中该关键词属于规则 " + value3 + "，导入规则为 " + text + "。";
					return false;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "导入失败：关键词校验异常：" + ex.Message;
			return false;
		}
	}

	internal static bool ValidateKnowledgeKeywordsForImport(KnowledgeLibraryBehavior knowledgeLibraryBehavior, string importDir, bool overwriteExisting, out string error)
	{
		error = "";
		try
		{
			if (knowledgeLibraryBehavior == null)
			{
				error = "导入失败：KnowledgeLibraryBehavior 未初始化。";
				return false;
			}
			List<KnowledgeLibraryBehavior.LoreRule> list = KnowledgeImportSupport.LoadKnowledgeRulesFromImportDir(importDir);
			if (list == null || list.Count <= 0)
			{
				error = "导入失败：导入目录中未找到可导入的知识规则文件。";
				return false;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (KnowledgeLibraryBehavior.LoreRule item in list)
			{
				string text = (item?.Id ?? "").Trim();
				if (string.IsNullOrEmpty(text))
				{
					continue;
				}
				foreach (string item2 in KnowledgeImportSupport.GetKnowledgeKeywordsForCompare(item))
				{
					if (dictionary.TryGetValue(item2, out var value) && !string.Equals(value, text, StringComparison.OrdinalIgnoreCase))
					{
						error = "导入失败：导入文件夹中存在重复关键词（" + item2 + "），分别属于规则 " + value + " 与 " + text + "。";
						return false;
					}
					dictionary[item2] = text;
				}
			}
			HashSet<string> existingIds = new HashSet<string>(knowledgeLibraryBehavior.GetRuleIdsForDev(100000) ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
			List<KnowledgeLibraryBehavior.LoreRule> list2 = list;
			if (!overwriteExisting)
			{
				list2 = list.Where((KnowledgeLibraryBehavior.LoreRule r) => r != null && !string.IsNullOrWhiteSpace(r.Id) && !existingIds.Contains((r.Id ?? "").Trim())).ToList();
			}
			HashSet<string> hashSet = (overwriteExisting ? new HashSet<string>(from r in list
				where r != null
				select (r.Id ?? "").Trim() into x
				where !string.IsNullOrEmpty(x)
				select x, StringComparer.OrdinalIgnoreCase) : new HashSet<string>(StringComparer.OrdinalIgnoreCase));
			KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = null;
			try
			{
				string value2 = knowledgeLibraryBehavior.ExportRulesJson();
				if (!string.IsNullOrWhiteSpace(value2))
				{
					knowledgeFile = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value2);
				}
			}
			catch
			{
				knowledgeFile = null;
			}
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (knowledgeFile?.Rules != null)
			{
				foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
				{
					string text2 = (rule?.Id ?? "").Trim();
					if (string.IsNullOrEmpty(text2) || hashSet.Contains(text2))
					{
						continue;
					}
					foreach (string item3 in KnowledgeImportSupport.GetKnowledgeKeywordsForCompare(rule))
					{
						if (dictionary2.TryGetValue(item3, out var value3) && !string.Equals(value3, text2, StringComparison.OrdinalIgnoreCase))
						{
							error = "导入失败：当前存档中已存在重复关键词（" + item3 + "），请先在游戏内修复后再导入。";
							return false;
						}
						dictionary2[item3] = text2;
					}
				}
			}
			Dictionary<string, string> dictionary3 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (KnowledgeLibraryBehavior.LoreRule item4 in list2)
			{
				string value4 = (item4?.Id ?? "").Trim();
				if (string.IsNullOrEmpty(value4))
				{
					continue;
				}
				foreach (string item5 in KnowledgeImportSupport.GetKnowledgeKeywordsForCompare(item4))
				{
					dictionary3[item5] = value4;
				}
			}
			foreach (KeyValuePair<string, string> item6 in dictionary3)
			{
				if (dictionary2.TryGetValue(item6.Key, out var value5) && !string.Equals(value5, item6.Value, StringComparison.OrdinalIgnoreCase))
				{
					error = "导入失败：关键词冲突（" + item6.Key + "）。当前存档中该关键词属于规则 " + value5 + "，导入规则为 " + item6.Value + "。";
					return false;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "导入失败：关键词校验异常：" + ex.Message;
			return false;
		}
	}

	internal static bool TryAddDatabaseReloadKnowledgeRule(KnowledgeLibraryBehavior.LoreRule rule, string sourceLabel, HashSet<string> knownRuleIds, Dictionary<string, string> keywordOwnerOriginalRuleIds, List<KnowledgeLibraryBehavior.LoreRule> rules, ref int disambiguatedRuleIdCount, ref int deduplicatedKeywordCount, out string error)
	{
		error = "";
		if (rule == null)
		{
			error = "资料包包含空知识条目。";
			return false;
		}
		string originalRuleId = (rule.Id ?? "").Trim();
		if (string.IsNullOrWhiteSpace(originalRuleId))
		{
			error = "知识条目 “" + (sourceLabel ?? "") + "” 的 RuleId 为空。";
			return false;
		}
		// Never turn a second package player-profile entry into ordinary lore by renaming it.
		if (KnowledgeLibraryBehavior.IsPlayerPersonaRuleId(originalRuleId))
		{
			return true;
		}
		string effectiveRuleId = originalRuleId;
		bool disambiguatedRuleId = false;
		if (!knownRuleIds.Add(effectiveRuleId))
		{
			effectiveRuleId = BuildDatabaseReloadDisambiguatedRuleId(originalRuleId, sourceLabel, knownRuleIds);
			if (string.IsNullOrWhiteSpace(effectiveRuleId) || !knownRuleIds.Add(effectiveRuleId))
			{
				error = "无法为重复知识 RuleId 生成稳定 ID：" + originalRuleId;
				return false;
			}
			rule.Id = effectiveRuleId;
			disambiguatedRuleId = true;
			disambiguatedRuleIdCount++;
			Logger.Log("DatabaseReload", "[WARN] Disambiguated duplicate knowledge RuleId source=" + (sourceLabel ?? "") + " old=" + originalRuleId + " new=" + effectiveRuleId);
		}
		else
		{
			rule.Id = effectiveRuleId;
		}
		if (disambiguatedRuleId)
		{
			// Exact-keyword lookup returns one rule only, so a split duplicate keeps each shared trigger on the original rule and retains its unique triggers here.
			deduplicatedKeywordCount += RemoveDuplicateKeywordsFromDisambiguatedDatabaseRule(rule, sourceLabel, originalRuleId, keywordOwnerOriginalRuleIds);
		}
		foreach (string keyword in rule.Keywords ?? new List<string>())
		{
			string normalizedKeyword = KnowledgeImportValidationOwner.NormalizeKeywordForCompare(keyword);
			if (!string.IsNullOrWhiteSpace(normalizedKeyword) && !keywordOwnerOriginalRuleIds.ContainsKey(normalizedKeyword))
			{
				keywordOwnerOriginalRuleIds[normalizedKeyword] = originalRuleId;
			}
		}
		rules.Add(rule);
		return true;
	}
	internal static string BuildDatabaseReloadDisambiguatedRuleId(string originalRuleId, string sourceLabel, HashSet<string> knownRuleIds)
	{
		string fileStem = Path.GetFileNameWithoutExtension(sourceLabel ?? "") ?? "";
		int separatorIndex = fileStem.IndexOf("__", StringComparison.Ordinal);
		string suffix = (separatorIndex >= 0 && separatorIndex + 2 < fileStem.Length) ? fileStem.Substring(separatorIndex + 2).Trim() : fileStem.Trim();
		if (string.IsNullOrWhiteSpace(suffix))
		{
			suffix = "duplicate";
		}
		string baseId = (originalRuleId ?? "").Trim() + "__" + suffix;
		string candidate = baseId;
		int suffixIndex = 2;
		while (knownRuleIds != null && knownRuleIds.Contains(candidate))
		{
			candidate = baseId + "_" + suffixIndex++;
		}
		return candidate;
	}

	internal static int RemoveDuplicateKeywordsFromDisambiguatedDatabaseRule(KnowledgeLibraryBehavior.LoreRule rule, string sourceLabel, string originalRuleId, Dictionary<string, string> keywordOwnerOriginalRuleIds)
	{
		if (rule?.Keywords == null || rule.Keywords.Count <= 0 || keywordOwnerOriginalRuleIds == null)
		{
			return 0;
		}
		int removedCount = 0;
		List<string> retainedKeywords = new List<string>(rule.Keywords.Count);
		foreach (string keyword in rule.Keywords)
		{
			string normalizedKeyword = NormalizeKeywordForCompare(keyword);
			if (!string.IsNullOrWhiteSpace(normalizedKeyword)
				&& keywordOwnerOriginalRuleIds.TryGetValue(normalizedKeyword, out string ownerOriginalRuleId)
				&& string.Equals(ownerOriginalRuleId, originalRuleId, StringComparison.OrdinalIgnoreCase))
			{
				removedCount++;
				Logger.Log("DatabaseReload", "[WARN] Kept duplicate source keyword on original rule source=" + (sourceLabel ?? "") + " ruleId=" + originalRuleId + " keyword=" + normalizedKeyword);
				continue;
			}
			retainedKeywords.Add(keyword);
		}
		if (removedCount > 0)
		{
			rule.Keywords = retainedKeywords;
		}
		return removedCount;
	}

}
