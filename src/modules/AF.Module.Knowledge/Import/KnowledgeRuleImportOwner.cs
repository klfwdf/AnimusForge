using System;
using System.Linq;
using Newtonsoft.Json;
namespace AnimusForge;
// Stateless import commit/diagnostics; KnowledgeLibraryBehavior remains the authoritative rule store.
internal static class KnowledgeRuleImportOwner
{
	internal static bool TryImportKnowledgeFileWithFallback(KnowledgeLibraryBehavior kb, KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile, bool overwriteExisting, out int importedCount, out int failedCount, out string firstFailedRuleId, out string firstFailedReason)
	{
		importedCount = 0;
		failedCount = 0;
		firstFailedRuleId = "";
		firstFailedReason = "";
		try
		{
			if (kb == null || knowledgeFile?.Rules == null)
			{
				return false;
			}
			string json = JsonConvert.SerializeObject(knowledgeFile, Formatting.None);
			if (kb.ImportRulesJson(json, overwriteExisting))
			{
				importedCount = knowledgeFile.Rules.Count((KnowledgeLibraryBehavior.LoreRule r) => r != null && !string.IsNullOrWhiteSpace(r.Id));
				return importedCount > 0;
			}
			foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
			{
				if (rule == null || string.IsNullOrWhiteSpace(rule.Id))
				{
					continue;
				}
				string json2 = JsonConvert.SerializeObject(rule, Formatting.None);
				if (kb.ImportSingleRuleJson(json2, overwriteExisting))
				{
					importedCount++;
				}
				else
				{
					failedCount++;
					if (string.IsNullOrWhiteSpace(firstFailedRuleId))
					{
						firstFailedRuleId = (rule.Id ?? "").Trim();
						firstFailedReason = BuildKnowledgeRuleImportFailureMessage(kb, rule, overwriteExisting);
					}
				}
			}
			return importedCount > 0;
		}
		catch
		{
			importedCount = 0;
			failedCount = 0;
			firstFailedRuleId = "";
			firstFailedReason = "";
			return false;
		}
	}

	internal static string BuildKnowledgeRuleImportFailureMessage(KnowledgeLibraryBehavior kb, KnowledgeLibraryBehavior.LoreRule rule, bool overwriteExisting)
	{
		try
		{
			string text = (rule?.Id ?? "").Trim();
			if (rule == null)
			{
				return "规则为空。";
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				return "RuleId 为空。";
			}
			if (rule.RagShortTexts != null)
			{
				for (int i = 0; i < rule.RagShortTexts.Count; i++)
				{
					string text2 = (rule.RagShortTexts[i] ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
					if (!string.IsNullOrWhiteSpace(text2) && text2.Length > KnowledgeLibraryBehavior.RagShortTextMaxLength)
					{
						return "RAG专用短句超过 " + KnowledgeLibraryBehavior.RagShortTextMaxLength + " 字符限制。";
					}
				}
			}
			if (!KnowledgeImportValidationOwner.ValidateKnowledgeKeywordsForSingleRuleImport(kb, rule, overwriteExisting, out var error))
			{
				return error;
			}
			if (KnowledgeImportSupport.TryFindDuplicateKnowledgeVariantCondition(rule, out var firstIndex, out var secondIndex))
			{
				return "规则内部存在重复条件的提示词：第 " + (firstIndex + 1) + " 条与第 " + (secondIndex + 1) + " 条条件完全相同。";
			}
			return "规则写入失败，可能是规则结构未通过校验。";
		}
		catch (Exception ex)
		{
			return "规则校验异常：" + ex.Message;
		}
	}

}
