using System;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>
/// Localized text used by the in-game knowledge editor and its result notifications.
/// The knowledge payload, rule IDs and prompt text remain data; only the surrounding UI copy is localized here.
/// </summary>
internal static class KnowledgeUiTexts
{
	private static string Text(string id, string fallback)
	{
		return new TextObject("{=" + id + "}" + fallback).ToString();
	}

	private static string Text(string id, string fallback, params (string Name, string Value)[] variables)
	{
		TextObject text = new TextObject("{=" + id + "}" + fallback);
		foreach ((string name, string value) in variables)
		{
			text.SetTextVariable(name, value ?? string.Empty);
		}
		return text.ToString();
	}

	public static string DuplicateVariantCondition(string item, string condition) => Text("AF_KNOWLEDGE_DuplicateCondition", "Identical conditions cannot be used by multiple prompts.\n\nDuplicate item: {ITEM} prompt\n\nCondition:\n{CONDITION}", ("ITEM", item), ("CONDITION", condition));
	public static string PlayerPersonaCreateFailed => Text("AF_KNOWLEDGE_PlayerPersonaCreateFailed", "Could not open the player profile editor: the knowledge entry could not be created.");
	public static string PlayerAppearanceCreateFailed => Text("AF_KNOWLEDGE_PlayerAppearanceCreateFailed", "Could not open the player appearance and background editor: the knowledge entry could not be created.");
	public static string PlayerNameKeywordConflict(string ruleId) => Text("AF_KNOWLEDGE_PlayerNameKeywordConflict", "The player's real-name keyword could not be added because another knowledge entry already uses it (RuleId={RULE_ID}).", ("RULE_ID", ruleId));
	public static string KeywordAlreadyExists => Text("AF_KNOWLEDGE_KeywordAlreadyExists", "This name is already present.");
	public static string KeywordConflict(string ruleId) => Text("AF_KNOWLEDGE_KeywordConflict", "Could not add the name: another knowledge entry already uses it (RuleId={RULE_ID}).", ("RULE_ID", ruleId));
	public static string DetailedBackgroundRequiresCondition => Text("AF_KNOWLEDGE_DetailedBackgroundRequiresCondition", "A detailed background needs at least one condition. To write a general background, return and use \"Background information (legacy)\".");
	public static string SettingsUnavailable => Text("AF_KNOWLEDGE_SettingsUnavailable", "The MCM settings could not be read.");
	public static string InvalidInteger => Text("AF_KNOWLEDGE_InvalidInteger", "Enter a valid integer.");
	public static string CreateRuleEmpty => Text("AF_KNOWLEDGE_CreateRuleEmpty", "Creation failed: the first keyword is empty or cannot be used as a name.");
	public static string CreateRuleDuplicate(string id) => Text("AF_KNOWLEDGE_CreateRuleDuplicate", "Creation failed: a knowledge entry with this name already exists (ID={ID}). Choose another keyword.", ("ID", id));
	public static string CreateRuleKeywordConflict(string keyword, string ruleId) => Text("AF_KNOWLEDGE_CreateRuleKeywordConflict", "Creation failed: the keyword is already used by another knowledge entry (keyword={KEYWORD}, RuleId={RULE_ID}).", ("KEYWORD", keyword), ("RULE_ID", ruleId));
	public static string BulkActionsCannotMix => Text("AF_KNOWLEDGE_BulkActionsCannotMix", "The bulk-delete page cannot select an operation together with knowledge entries.");
	public static string NoRulesSelected => Text("AF_KNOWLEDGE_NoRulesSelected", "No deletable knowledge entries are selected.");
	public static string ConfirmBulkDelete(string preview, int count) => Text("AF_KNOWLEDGE_ConfirmBulkDelete", "The following knowledge entries will be deleted:\n\n{PREVIEW}\n\nTotal: {COUNT}. This action cannot be undone.", ("PREVIEW", preview), ("COUNT", count.ToString()));
	public static string Deleted(int count) => Text("AF_KNOWLEDGE_Deleted", "Deleted {COUNT} knowledge entries.", ("COUNT", count.ToString()));
	public static string AgeRangeTitle => Text("AF_KNOWLEDGE_AgeRangeTitle", "Set age range");
	public static string AgeRangePrompt(int min, int max) => Text("AF_KNOWLEDGE_AgeRangePrompt", "Enter an age range in minimum-maximum format.\nExample: 18-35\nCurrent value: {MIN}-{MAX}", ("MIN", min.ToString()), ("MAX", max.ToString()));
	public static string InvalidAgeRange => Text("AF_KNOWLEDGE_InvalidAgeRange", "Enter a valid range, for example 18-35.");
	public static string KeywordInRuleAlreadyExists => Text("AF_KNOWLEDGE_KeywordInRuleAlreadyExists", "Could not add the keyword: it is already present in this knowledge entry.");
	public static string RagTooLong(int max) => Text("AF_KNOWLEDGE_RagTooLong", "Could not add the RAG short text: it cannot exceed {MAX} characters.", ("MAX", max.ToString()));
	public static string RagAlreadyExists => Text("AF_KNOWLEDGE_RagAlreadyExists", "Could not add the RAG short text: it is already present in this knowledge entry.");
	public static string RagGenerationNoSource => Text("AF_KNOWLEDGE_RagGenerationNoSource", "[Knowledge] Generation failed: this entry has no content that can be used for generation.");
	public static string RagGenerating => Text("AF_KNOWLEDGE_RagGenerating", "[Knowledge] Generating RAG short texts. Please wait...");
	public static string RagGenerated(int count) => Text("AF_KNOWLEDGE_RagGenerated", "[Knowledge] Generated and added {COUNT} RAG short texts.", ("COUNT", count.ToString()));
	public static string RagNoNew => Text("AF_KNOWLEDGE_RagNoNew", "[Knowledge] No new RAG short texts were generated; they may duplicate existing content.");
	public static string PrototypeDisabled => Text("AF_KNOWLEDGE_PrototypeDisabled", "[Knowledge] Prototype sentences are disabled. Maintain keywords, RAG short texts and prompts directly.");
}
