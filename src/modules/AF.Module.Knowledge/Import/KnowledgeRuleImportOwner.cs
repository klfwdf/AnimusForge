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
				return KnowledgeUiTexts.ImportRuleEmpty;
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				return KnowledgeUiTexts.ImportRuleIdEmpty;
			}
			if (rule.RagShortTexts != null)
			{
				for (int i = 0; i < rule.RagShortTexts.Count; i++)
				{
					string text2 = (rule.RagShortTexts[i] ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
					if (!string.IsNullOrWhiteSpace(text2) && text2.Length > KnowledgeLibraryBehavior.RagShortTextMaxLength)
					{
						return KnowledgeUiTexts.ImportRagTooLong(KnowledgeLibraryBehavior.RagShortTextMaxLength);
					}
				}
			}
			if (!KnowledgeImportValidationOwner.ValidateKnowledgeKeywordsForSingleRuleImport(kb, rule, overwriteExisting, out var error))
			{
				return error;
			}
			if (KnowledgeImportSupport.TryFindDuplicateKnowledgeVariantCondition(rule, out var firstIndex, out var secondIndex))
			{
				return KnowledgeUiTexts.ImportDuplicateCondition(firstIndex + 1, secondIndex + 1);
			}
			return KnowledgeUiTexts.ImportWriteFailed;
		}
		catch (Exception ex)
		{
			return KnowledgeUiTexts.ImportValidationFailed(ex.Message);
		}
	}

}
