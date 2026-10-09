using System;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>
/// Localized text used by the in-game knowledge editor and its result notifications.
/// The knowledge payload, rule IDs and prompt text remain data; only the surrounding UI copy is localized here.
/// </summary>
internal static class KnowledgeUiTexts
{
	public static string Ui(string id, string fallback)
	{
		return Text(id, fallback);
	}

	public static string Ui(string id, string fallback, params (string Name, string Value)[] variables)
	{
		return Text(id, fallback, variables);
	}

	public static string EditorExisting => Ui("AF_KNOWLEDGE_EditorExisting", "Edit existing knowledge");
	public static string EditorCreate => Ui("AF_KNOWLEDGE_EditorCreate", "Create new knowledge");
	public static string EditorTitle => Ui("AF_KNOWLEDGE_EditorTitle", "Knowledge editor");
	public static string ChooseAction => Ui("AF_KNOWLEDGE_ChooseAction", "Choose an action:");
	public static string Enter => Ui("AF_KNOWLEDGE_Enter", "Enter");
	public static string Select => Ui("AF_KNOWLEDGE_Select", "Select");
	public static string Continue => Ui("AF_KNOWLEDGE_Continue", "Continue");
	public static string Skip => Ui("AF_KNOWLEDGE_Skip", "Skip");
	public static string Delete => Ui("AF_KNOWLEDGE_Delete", "Delete");
	public static string Cancel => Ui("AF_KNOWLEDGE_Cancel", "Cancel");
	public static string Back => Ui("AF_KNOWLEDGE_Back", "Back");
	public static string Confirm => Ui("AF_KNOWLEDGE_Confirm", "Confirm");
	public static string Search => Ui("AF_KNOWLEDGE_Search", "Search");
	public static string SearchByKeywordId => Ui("AF_KNOWLEDGE_SearchByKeywordId", "Search (keyword/ID)");
	public static string PreviousPage => Ui("AF_KNOWLEDGE_PreviousPage", "Previous page");
	public static string NextPage => Ui("AF_KNOWLEDGE_NextPage", "Next page");
	public static string ClearSearch => Ui("AF_KNOWLEDGE_ClearSearch", "Clear search");
	public static string Create => Ui("AF_KNOWLEDGE_Create", "Create");

	public static string RuleListTitle(int count, int page, int pages) => Ui("AF_KNOWLEDGE_RuleListTitle", "Edit existing knowledge ({COUNT} entries, page {PAGE}/{PAGES})", ("COUNT", count.ToString()), ("PAGE", page.ToString()), ("PAGES", pages.ToString()));
	public static string RuleListEmptyTitle => Ui("AF_KNOWLEDGE_RuleListEmptyTitle", "Edit existing knowledge (0 entries)");
	public static string RuleListChoose => Ui("AF_KNOWLEDGE_RuleListChoose", "Choose a knowledge entry to edit:");
	public static string RuleListSearch(string query) => Ui("AF_KNOWLEDGE_RuleListSearch", "Current search: {QUERY}", ("QUERY", query));
	public static string RuleListNoMatches => Ui("AF_KNOWLEDGE_RuleListNoMatches", "No matches. Use \"Search\" or \"Create new knowledge\".");
	public static string BulkDeleteTitle(int count, int page, int pages) => Ui("AF_KNOWLEDGE_BulkDeleteTitle", "Bulk delete knowledge ({COUNT} entries, page {PAGE}/{PAGES})", ("COUNT", count.ToString()), ("PAGE", page.ToString()), ("PAGES", pages.ToString()));
	public static string BulkDeleteEmptyTitle => Ui("AF_KNOWLEDGE_BulkDeleteEmptyTitle", "Bulk delete knowledge (0 entries)");
	public static string BulkDeleteDescription(string query, bool empty) => Ui("AF_KNOWLEDGE_BulkDeleteDescription", "Select knowledge entries on the current page to delete them together.{QUERY}{EMPTY}", ("QUERY", string.IsNullOrWhiteSpace(query) ? "" : "\nCurrent search: " + query), ("EMPTY", empty ? "\n\nNo matches." : ""));
	public static string DeleteSelected => Ui("AF_KNOWLEDGE_DeleteSelected", "Delete selected");
	public static string BulkDelete => Ui("AF_KNOWLEDGE_BulkDelete", "Bulk delete knowledge");
	public static string SearchKnowledgeTitle => Ui("AF_KNOWLEDGE_SearchKnowledgeTitle", "Search knowledge");
	public static string SearchKnowledgePrompt => Ui("AF_KNOWLEDGE_SearchKnowledgePrompt", "Enter keywords or a RuleId (separate multiple terms with spaces):");
	public static string CreateKnowledgeTitle => Ui("AF_KNOWLEDGE_CreateKnowledgeTitle", "Create new knowledge");
	public static string CreateKnowledgePrompt => Ui("AF_KNOWLEDGE_CreateKnowledgePrompt", "Enter the first keyword for this knowledge entry. It becomes the entry ID and export filename, must be unique, and more keywords can be added later:");

	public static string RuleMenuTitle(string label) => Ui("AF_KNOWLEDGE_RuleMenuTitle", "Knowledge - {LABEL}", ("LABEL", label));
	public static string RuleMenuDescription(int keywords, int rag, int variants, int mappings) => Ui("AF_KNOWLEDGE_RuleMenuDescription", "Keywords: {KEYWORDS}\nRAG short texts: {RAG}\nPrompts: {VARIANTS}\nText mappings: {MAPPINGS}", ("KEYWORDS", keywords.ToString()), ("RAG", rag.ToString()), ("VARIANTS", variants.ToString()), ("MAPPINGS", mappings.ToString()));
	public static string EditKeywords => Ui("AF_KNOWLEDGE_EditKeywords", "Edit keywords");
	public static string EditRag => Ui("AF_KNOWLEDGE_EditRag", "Edit RAG short texts");
	public static string GenerateRag => Ui("AF_KNOWLEDGE_GenerateRag", "Generate RAG short texts");
	public static string EditVariants => Ui("AF_KNOWLEDGE_EditVariants", "Edit prompts (conditional variants)");
	public static string EditMappings => Ui("AF_KNOWLEDGE_EditMappings", "Edit text mappings");
	public static string DeleteKnowledge => Ui("AF_KNOWLEDGE_DeleteKnowledge", "Delete this knowledge");

	public static string AddMapping => Ui("AF_KNOWLEDGE_AddMapping", "Add text mapping");
	public static string MappingListDescription => Ui("AF_KNOWLEDGE_MappingListDescription", "Only mappings belonging to this knowledge entry are shown; other knowledge entries are unaffected.");
	public static string MappingEditorTitle => Ui("AF_KNOWLEDGE_MappingEditorTitle", "Edit text mapping");
	public static string AddMappingTitle => Ui("AF_KNOWLEDGE_AddMappingTitle", "Add text mapping");
	public static string AddMappingPrompt => Ui("AF_KNOWLEDGE_AddMappingPrompt", "Enter the source text to replace. When this knowledge is matched, it will be replaced with the selected dynamic value.");
	public static string NextStep => Ui("AF_KNOWLEDGE_NextStep", "Next");
	public static string EditSource => Ui("AF_KNOWLEDGE_EditSource", "Edit source text");
	public static string EditTarget => Ui("AF_KNOWLEDGE_EditTarget", "Edit mapping target");
	public static string EditTrueText => Ui("AF_KNOWLEDGE_EditTrueText", "Edit true-state text");
	public static string EditFalseText => Ui("AF_KNOWLEDGE_EditFalseText", "Edit false-state text");
	public static string EditEmptyText => Ui("AF_KNOWLEDGE_EditEmptyText", "Edit empty-value text");
	public static string EditAgeRange => Ui("AF_KNOWLEDGE_EditAgeRange", "Edit age range");
	public static string DeleteMapping => Ui("AF_KNOWLEDGE_DeleteMapping", "Delete mapping");
	public static string EditSourcePrompt => Ui("AF_KNOWLEDGE_EditSourcePrompt", "Enter the new source text:");
	public static string TrueTextPrompt => Ui("AF_KNOWLEDGE_TrueTextPrompt", "When the status is true, use this text to replace the source. Leave empty to keep the source unchanged.");
	public static string FalseTextPrompt => Ui("AF_KNOWLEDGE_FalseTextPrompt", "When the status is false, use this text to replace the source. Leave empty to keep the source unchanged.");
	public static string EmptyTextPrompt => Ui("AF_KNOWLEDGE_EmptyTextPrompt", "When this mapping has no value, use this text to replace the source. Leave empty to keep the source unchanged.");
	public static string SetTrueTextTitle => Ui("AF_KNOWLEDGE_SetTrueTextTitle", "Set true-state text");
	public static string SetFalseTextTitle => Ui("AF_KNOWLEDGE_SetFalseTextTitle", "Set false-state text");
	public static string SetTrueTextPrompt => Ui("AF_KNOWLEDGE_SetTrueTextPrompt", "Enter the text to use when the status is true. Leave empty to keep the source unchanged.");
	public static string SetFalseTextPrompt => Ui("AF_KNOWLEDGE_SetFalseTextPrompt", "Enter the text to use when the status is false. Leave empty to keep the source unchanged.");
	public static string Finish => Ui("AF_KNOWLEDGE_Finish", "Finish");

	public static string MappingSearchTitle => Ui("AF_KNOWLEDGE_MappingSearchTitle", "Search mapping types");
	public static string MappingSearchPrompt => Ui("AF_KNOWLEDGE_MappingSearchPrompt", "Enter keywords separated by spaces.\nExamples: clan kingdom / settlement ruler / spouse / age / status alive / kingdom eliminated");
	public static string MappingSearchMenuTitle => Ui("AF_KNOWLEDGE_MappingSearchMenuTitle", "Who and what do you want to retrieve?");
	public static string MappingSearchMenuDescription => Ui("AF_KNOWLEDGE_MappingSearchMenuDescription", "Choose a category, or search for a specific mapping. Status checks are supported as well. When there are many options, searching is faster.");
	public static string MappingChooseTypeTitle => Ui("AF_KNOWLEDGE_MappingChooseTypeTitle", "Choose mapping type");
	public static string MappingChooseType => Ui("AF_KNOWLEDGE_MappingChooseType", "Choose a mapping type.");
	public static string MappingSearchResults(string query, int count, bool empty) => Ui("AF_KNOWLEDGE_MappingSearchResults", "Enter keywords to search mapping types.\nExamples: clan kingdom / settlement ruler / spouse / age / status alive / kingdom eliminated{QUERY}\nMatches: {COUNT}{EMPTY}", ("QUERY", string.IsNullOrWhiteSpace(query) ? "" : "\nCurrent search: " + query), ("COUNT", count.ToString()), ("EMPTY", empty ? "\n\nNo matches. Try another keyword." : ""));

	public static string KeywordMenuTitle => Ui("AF_KNOWLEDGE_KeywordMenuTitle", "Edit keywords");
	public static string KeywordMenuDescription => Ui("AF_KNOWLEDGE_KeywordMenuDescription", "Enter one keyword at a time; it is added after confirmation.");
	public static string AddKeyword => Ui("AF_KNOWLEDGE_AddKeyword", "Add keyword (one at a time)");
	public static string RemoveKeyword => Ui("AF_KNOWLEDGE_RemoveKeyword", "Remove keyword");
	public static string KeywordLabel(string keyword) => Ui("AF_KNOWLEDGE_KeywordLabel", "Keyword: {KEYWORD}", ("KEYWORD", keyword));
	public static string AddKeywordTitle => Ui("AF_KNOWLEDGE_AddKeywordTitle", "Add keyword");
	public static string AddKeywordPrompt => Ui("AF_KNOWLEDGE_AddKeywordPrompt", "Enter a keyword:");
	public static string RemoveKeywordTitle => Ui("AF_KNOWLEDGE_RemoveKeywordTitle", "Remove keyword");
	public static string RemoveKeywordPrompt => Ui("AF_KNOWLEDGE_RemoveKeywordPrompt", "Choose a keyword to remove:");

	public static string AddRag => Ui("AF_KNOWLEDGE_AddRag", "Add RAG short text (one at a time)");
	public static string RemoveRag => Ui("AF_KNOWLEDGE_RemoveRag", "Remove RAG short text");
	public static string RagLabel(string text) => Ui("AF_KNOWLEDGE_RagLabel", "RAG short text: {TEXT}", ("TEXT", text));
	public static string RagMenuTitle => Ui("AF_KNOWLEDGE_RagMenuTitle", "Edit RAG short texts");
	public static string RagMenuDescription(int max, int generatedMax, int maxTokens) => Ui("AF_KNOWLEDGE_RagMenuDescription", "Short texts used for knowledge retrieval and reranking. Write concise descriptions that match likely questions; do not copy the full prompt. Manual entries allow up to {MAX} characters, AI generation is limited to {GENERATED_MAX} characters, and max_tokens is {MAX_TOKENS}.", ("MAX", max.ToString()), ("GENERATED_MAX", generatedMax.ToString()), ("MAX_TOKENS", maxTokens.ToString()));
	public static string AddRagTitle => Ui("AF_KNOWLEDGE_AddRagTitle", "Add RAG short text");
	public static string AddRagPrompt(int max) => Ui("AF_KNOWLEDGE_AddRagPrompt", "Enter one RAG short text (up to {MAX} characters):", ("MAX", max.ToString()));
	public static string RemoveRagTitle => Ui("AF_KNOWLEDGE_RemoveRagTitle", "Remove RAG short text");
	public static string RemoveRagPrompt => Ui("AF_KNOWLEDGE_RemoveRagPrompt", "Choose a RAG short text to remove:");
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
