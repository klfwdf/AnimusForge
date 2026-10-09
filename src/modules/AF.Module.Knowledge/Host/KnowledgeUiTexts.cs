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

	public static string NoValue => Ui("AF_KNOWLEDGE_NoValue", "(none)");
	public static string PlayerNameConflictStatus => Ui("AF_KNOWLEDGE_PlayerNameConflictStatus", "(could not be saved because it conflicts with another knowledge entry)");
	public static string PlayerKeywordSetupTitle => Ui("AF_KNOWLEDGE_PlayerKeywordSetupTitle", "Player name settings");
	public static string PlayerKeywordSetupBody(string name, string conflict, string optional) => Ui("AF_KNOWLEDGE_PlayerKeywordSetupBody", "Welcome to AnimusForge! Use this screen to add extra names for your character. If you do not need them, continue to appearance and background settings.\n\nCharacter name: {NAME}{CONFLICT}\nExtra names: {OPTIONAL}", ("NAME", name), ("CONFLICT", conflict), ("OPTIONAL", optional));
	public static string AddOptionalName => Ui("AF_KNOWLEDGE_AddOptionalName", "Add extra name");
	public static string RemoveOptionalName => Ui("AF_KNOWLEDGE_RemoveOptionalName", "Remove extra name");
	public static string OpenAppearanceBackground => Ui("AF_KNOWLEDGE_OpenAppearanceBackground", "Write appearance and background");
	public static string AddOptionalNameTitle => Ui("AF_KNOWLEDGE_AddOptionalNameTitle", "Add extra name");
	public static string AddOptionalNamePrompt => Ui("AF_KNOWLEDGE_AddOptionalNamePrompt", "Enter an extra name. The player's real name is already kept by the system and does not need to be entered again.");
	public static string RemoveOptionalNameTitle => Ui("AF_KNOWLEDGE_RemoveOptionalNameTitle", "Remove extra name");
	public static string RemoveOptionalNamePrompt => Ui("AF_KNOWLEDGE_RemoveOptionalNamePrompt", "The player's system-kept real name cannot be removed. Only extra names you added will be removed.");

	public static string PlayerAppearanceOption => Ui("AF_KNOWLEDGE_PlayerAppearanceOption", "Appearance (always visible)");
	public static string PlayerBackgroundOption => Ui("AF_KNOWLEDGE_PlayerBackgroundOption", "Background (general)");
	public static string PlayerDetailedBackgroundOption => Ui("AF_KNOWLEDGE_PlayerDetailedBackgroundOption", "Different views of your background");
	public static string PlayerAppearanceBackgroundDone => Ui("AF_KNOWLEDGE_PlayerAppearanceBackgroundDone", "Finish and start the game");
	public static string PlayerAppearanceBackgroundDoneDetail => Ui("AF_KNOWLEDGE_PlayerAppearanceBackgroundDoneDetail", "Save the current settings and leave this screen.");
	public static string PlayerAppearanceBackgroundTitle => Ui("AF_KNOWLEDGE_PlayerAppearanceBackgroundTitle", "Player appearance and background");
	public static string PlayerAppearanceBackgroundDescription => Ui("AF_KNOWLEDGE_PlayerAppearanceBackgroundDescription", "Player information is divided into appearance and background information.");
	public static string PlayerAppearanceBackgroundBody(string appearance, string background, int detailedCount) => Ui("AF_KNOWLEDGE_PlayerAppearanceBackgroundBody", "Player information is divided into appearance and background information.\n\nAppearance (always visible):\n{APPEARANCE}\n\nBackground (general):\n{BACKGROUND}\n\nConditional background details: {COUNT}", ("APPEARANCE", appearance), ("BACKGROUND", background), ("COUNT", detailedCount.ToString()));
	public static string PlayerOptionUnset => Ui("AF_KNOWLEDGE_PlayerOptionUnset", "Not filled in. Select this option to enter the full text.");
	public static string PlayerOptionFilled(int length) => Ui("AF_KNOWLEDGE_PlayerOptionFilled", "{COUNT} characters entered. The full text is shown on the left; select this option to edit it.", ("COUNT", length.ToString()));
	public static string PlayerDetailedBackgroundCount(int count) => Ui("AF_KNOWLEDGE_PlayerDetailedBackgroundCount", "{COUNT} conditional background entries configured.", ("COUNT", count.ToString()));
	public static string PlayerAppearanceEditorTitle => Ui("AF_KNOWLEDGE_PlayerAppearanceEditorTitle", "Player appearance (always visible)");
	public static string PlayerAppearanceEditorIntro => Ui("AF_KNOWLEDGE_PlayerAppearanceEditorIntro", "The current appearance information is loaded in the input box below.");
	public static string PlayerAppearanceEditorHint => Ui("AF_KNOWLEDGE_PlayerAppearanceEditorHint", "Enter appearance traits that NPCs can observe directly. Leave empty to disable this information.");
	public static string PlayerBackgroundEditorTitle => Ui("AF_KNOWLEDGE_PlayerBackgroundEditorTitle", "Player background (general)");
	public static string PlayerBackgroundEditorIntro => Ui("AF_KNOWLEDGE_PlayerBackgroundEditorIntro", "The current general background information is loaded in the input box below.");
	public static string PlayerBackgroundEditorHint => Ui("AF_KNOWLEDGE_PlayerBackgroundEditorHint", "Enter background, origin, identity and similar information. Leave empty to disable this information.");
	public static string DetailedBackgroundListTitle => Ui("AF_KNOWLEDGE_DetailedBackgroundListTitle", "Detailed player background");
	public static string DetailedBackgroundListDescription => Ui("AF_KNOWLEDGE_DetailedBackgroundListDescription", "These entries contain conditional background information. Their stored structure is the same as other knowledge prompts.");
	public static string AddDetailedBackground => Ui("AF_KNOWLEDGE_AddDetailedBackground", "Add a detailed entry");
	public static string EmptyPreview => Ui("AF_KNOWLEDGE_EmptyPreview", "(empty)");
	public static string DetailedBackgroundEditorTitle => Ui("AF_KNOWLEDGE_DetailedBackgroundEditorTitle", "Edit detailed background");
	public static string DetailedBackgroundEditorDescription(string condition, string preview) => Ui("AF_KNOWLEDGE_DetailedBackgroundEditorDescription", "Current condition: {CONDITION}\n\nCurrent content preview:\n{PREVIEW}", ("CONDITION", condition), ("PREVIEW", preview));
	public static string EditDetailedBackgroundContent => Ui("AF_KNOWLEDGE_EditDetailedBackgroundContent", "Edit entry content");
	public static string EditDetailedBackgroundCondition => Ui("AF_KNOWLEDGE_EditDetailedBackgroundCondition", "Edit condition");
	public static string DeleteDetailedBackground => Ui("AF_KNOWLEDGE_DeleteDetailedBackground", "Delete this detailed entry");
	public static string DetailedBackgroundContentTitle => Ui("AF_KNOWLEDGE_DetailedBackgroundContentTitle", "Edit background content");
	public static string DetailedBackgroundContentIntro => Ui("AF_KNOWLEDGE_DetailedBackgroundContentIntro", "This conditional player background information is loaded in the input box below.");
	public static string DetailedBackgroundContentHint => Ui("AF_KNOWLEDGE_DetailedBackgroundContentHint", "Edit the complete text directly.");

	public static string RetrievalSourceMcm => Ui("AF_KNOWLEDGE_RetrievalSourceMcm", "MCM (currently active)");
	public static string RetrievalSourceFile => Ui("AF_KNOWLEDGE_RetrievalSourceFile", "RuleBehaviorPrompts.json (currently active)");
	public static string RetrievalMode => Ui("AF_KNOWLEDGE_RetrievalMode", "Per-term retrieval + local reranking");
	public static string RetrievalOn => Ui("AF_KNOWLEDGE_RetrievalOn", "enabled");
	public static string RetrievalOff => Ui("AF_KNOWLEDGE_RetrievalOff", "disabled");
	public static string RetrievalYes => Ui("AF_KNOWLEDGE_RetrievalYes", "yes");
	public static string RetrievalNoKeywordFirst => Ui("AF_KNOWLEDGE_RetrievalNoKeywordFirst", "no (keyword first)");
	public static string RetrievalDescription(string source, string mode, string enabled, string first, int topK) => Ui("AF_KNOWLEDGE_RetrievalDescription", "Configuration source: {SOURCE}\nCurrent mode: {MODE}\nSemantic retrieval: {ENABLED}\nSemantic first: {FIRST}\nKnowledge return limit: {TOPK}\nDetails: each mentioned term is retrieved independently in importance order, with one knowledge slot per term. If the highest-scoring knowledge is already used by an earlier term, the next unused knowledge is selected. Additional terms are only filled when the limit is greater than the number of terms.", ("SOURCE", source), ("MODE", mode), ("ENABLED", enabled), ("FIRST", first), ("TOPK", topK.ToString()));
	public static string RetrievalSwitchSource(string source) => Ui("AF_KNOWLEDGE_RetrievalSwitchSource", "Configuration source: switch to {SOURCE}", ("SOURCE", source));
	public static string RetrievalToggle(string state) => Ui("AF_KNOWLEDGE_RetrievalToggle", "Semantic retrieval: {STATE}", ("STATE", state));
	public static string RetrievalTopK(int topK) => Ui("AF_KNOWLEDGE_RetrievalTopK", "Set knowledge return limit (current: {TOPK})", ("TOPK", topK.ToString()));
	public static string RetrievalReset => Ui("AF_KNOWLEDGE_RetrievalReset", "Restore defaults (MCM)");
	public static string RetrievalSettingsTitle => Ui("AF_KNOWLEDGE_RetrievalSettingsTitle", "Knowledge retrieval settings");
	public static string RetrievalSelect => Ui("AF_KNOWLEDGE_RetrievalSelect", "Select");
	public static string RetrievalTopKTitle => Ui("AF_KNOWLEDGE_RetrievalTopKTitle", "Set knowledge return limit");
	public static string RetrievalTopKPrompt => Ui("AF_KNOWLEDGE_RetrievalTopKPrompt", "Enter an integer from 1 to 12:");
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

	public static string PrototypeCountSummary(int count, string query, int page, int pages, bool empty)
		=> Ui("AF_KNOWLEDGE_PrototypeCountSummary", "{COUNT} entries, page {PAGE}/{PAGES}.{SEARCH}{EMPTY}",
			("COUNT", count.ToString()), ("PAGE", page.ToString()), ("PAGES", pages.ToString()),
			("SEARCH", string.IsNullOrWhiteSpace(query) ? "" : Ui("AF_KNOWLEDGE_PrototypeCurrentSearch", "\nCurrent search: {QUERY}", ("QUERY", query))),
			("EMPTY", empty ? Ui("AF_KNOWLEDGE_PrototypeNoContent", "\nThere is no content on this page.") : ""));
	public static string VariantEditorDescription(string condition, string preview, bool truncated)
		=> Ui("AF_KNOWLEDGE_VariantEditorDescription", "Current conditions: {CONDITION}\n\nCurrent content preview:\n{PREVIEW}{TRUNCATED}",
			("CONDITION", condition), ("PREVIEW", string.IsNullOrEmpty(preview) ? NoValue : preview),
			("TRUNCATED", truncated ? Ui("AF_KNOWLEDGE_VariantTruncated", "\n... (content too long; showing the first 220 characters)") : ""));
	public static string RoleSearchOption(string query)
		=> Ui("AF_KNOWLEDGE_RoleSearchOption", string.IsNullOrWhiteSpace(query) ? "Search specific NPCs in this category..." : "Search: {QUERY}", ("QUERY", query ?? ""));
	public static string RoleClearSpecificOption(int count)
		=> Ui("AF_KNOWLEDGE_RoleClearSpecificOption", "Clear selected specific entries in this category ({COUNT})", ("COUNT", count.ToString()));
	public static string RoleDescription(string role, bool selected, int selectedSpecific, int total, int page, int pages)
		=> Ui("AF_KNOWLEDGE_RoleDescription", "{ROLE}: select the whole category or individual NPCs.\nWhole category: {SELECTED}\nSpecific entries selected: {SPECIFIC}\nTotal results: {TOTAL}, page {PAGE}/{PAGES}",
			("ROLE", role), ("SELECTED", selected ? Ui("AF_KNOWLEDGE_RoleSelected", "selected") : Ui("AF_KNOWLEDGE_RoleNotSelected", "not selected")), ("SPECIFIC", selectedSpecific.ToString()),
			("TOTAL", total.ToString()), ("PAGE", page.ToString()), ("PAGES", pages.ToString()));
	public static string PickerNoMatch(string kind, string query)
		=> Ui("AF_KNOWLEDGE_PickerNoMatch", string.IsNullOrWhiteSpace(query) ? "No selectable {KIND} found." : "No matching {KIND} found: {QUERY}", ("KIND", kind), ("QUERY", query ?? ""));
	public static string PickerPageSummary(string kind, int count, int page, int pages, string query, string suffix)
		=> Ui("AF_KNOWLEDGE_PickerPageSummary", "{COUNT} {KIND}, page {PAGE}/{PAGES}.{SUFFIX}{SEARCH}",
			("COUNT", count.ToString()), ("KIND", kind), ("PAGE", page.ToString()), ("PAGES", pages.ToString()),
			("SUFFIX", suffix), ("SEARCH", string.IsNullOrWhiteSpace(query) ? "" : Ui("AF_KNOWLEDGE_PickerCurrentSearch", "\nCurrent search: {QUERY}", ("QUERY", query))));
	public static string PickerEntryWithLeader(string name, string id, string leaderLabel, string leader)
		=> Ui("AF_KNOWLEDGE_PickerEntryWithLeader", string.IsNullOrWhiteSpace(leader) ? "{NAME} ({ID})" : "{NAME} ({LEADER_LABEL}: {LEADER})",
			("NAME", name), ("ID", id), ("LEADER_LABEL", leaderLabel), ("LEADER", leader));
	public static string PickerSearchPrompt(string kind, string idName)
		=> Ui("AF_KNOWLEDGE_PickerSearchPrompt", "Enter {KIND} name or {ID}: ", ("KIND", kind), ("ID", idName));
	public static string PickerRoleSearchPrompt(string role)
		=> Ui("AF_KNOWLEDGE_PickerRoleSearchPrompt", "Enter a name, HeroId or CharacterId. Search is limited to this category.", ("ROLE", role));
	public static string StringListRemoveTitle(string title) => Ui("AF_KNOWLEDGE_StringListRemoveTitle", "Delete - {TITLE}", ("TITLE", title));
	public static string SkillItem(string id, int value) => Ui("AF_KNOWLEDGE_SkillItem", "{ID} >= {VALUE}", ("ID", id), ("VALUE", value.ToString()));
	public static string WhenLabelExclusive(string categories) => Ui("AF_KNOWLEDGE_WhenLabelExclusive", "Specific ({CATEGORIES})", ("CATEGORIES", categories));
	public static string WhenDetail(string culture, string kingdom, string settlement, string roles, string identities, string skills, string gender, string clanLeader)
		=> Ui("AF_KNOWLEDGE_WhenDetail", "Current conditions:\nCultureId: {CULTURE}\nKingdomId: {KINGDOM}\nSettlementId: {SETTLEMENT}\nRoles: {ROLES}\nSpecific identities: {IDENTITIES}\nSkillMin: {SKILLS}\nGender: {GENDER}\nClanLeader: {CLAN_LEADER}",
			("CULTURE", culture), ("KINGDOM", kingdom), ("SETTLEMENT", settlement), ("ROLES", roles), ("IDENTITIES", identities),
			("SKILLS", skills), ("GENDER", gender), ("CLAN_LEADER", clanLeader));
}
