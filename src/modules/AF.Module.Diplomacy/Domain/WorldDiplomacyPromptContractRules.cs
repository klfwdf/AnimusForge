using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyPromptContractRules
{
	internal const int MaxGeneratedDraftRepairAttempts = 1;
	internal const string CanonicalHistoryCacheAffinityKey = "diplomacy-history:v31";
	internal static readonly string CanonicalHistoryContractMarker = CompatibilityContractText.Values["AF_DIP_Compatibility_000"];
	internal static readonly string DiplomaticDeclarationWritingContractMarker = CompatibilityContractText.Values["AF_DIP_Compatibility_001"];
	internal static readonly string DiplomacyModeDispatchContractMarker = CompatibilityContractText.Values["AF_DIP_Compatibility_002"];
	internal static readonly string DiplomaticDeclarationModeContractMarker = CompatibilityContractText.Values["AF_DIP_Compatibility_003"];
	internal static readonly string CanonicalHistoryCompressionModeContractMarker = CompatibilityContractText.Values["AF_DIP_Compatibility_004"];
	internal static readonly string DiplomacyAnalysisTaskMarker = CompatibilityContractText.Values["AF_DIP_Compatibility_005"];
	internal static readonly string RoundPlanTaskMarker = CompatibilityContractText.Values["AF_DIP_Compatibility_006"];

    private static readonly string[] EnglishHeadings = {
        "[AI diplomacy shared long-term memory mode]", "[Diplomatic document writing contract]",
        "[AI diplomacy fixed task MODE dispatch]", "[MODE=DECLARE fixed task contract]",
        "[MODE=COMPACT fixed task contract]", "[Diplomacy analysis task]", "[Diplomacy round planning task]"
    };

    internal static string MarkerForPrompt(string source, Func<string, string, string> localize)
    {
        if (localize == null) return source;
        for (int index = 0; index < EnglishHeadings.Length; index++)
            if (source == CompatibilityContractText.Values["AF_DIP_Compatibility_" + index.ToString("000", CultureInfo.InvariantCulture)])
                return localize("AF_DIP_Heading_" + index.ToString("000", CultureInfo.InvariantCulture), EnglishHeadings[index]);
        return source;
    }

    private static int MarkerIndex(string text, string marker, bool last = false)
    {
        int raw = last ? text.LastIndexOf(marker, StringComparison.Ordinal) : text.IndexOf(marker, StringComparison.Ordinal);
        string english = MarkerForPrompt(marker, (key, fallback) => fallback);
        int projected = last ? text.LastIndexOf(english, StringComparison.Ordinal) : text.IndexOf(english, StringComparison.Ordinal);
        return raw < 0 ? projected : projected < 0 ? raw : last ? Math.Max(raw, projected) : Math.Min(raw, projected);
    }

    internal static string ContractText(Func<string, string, string> localize, string id, string englishFallback)
        => localize == null ? CompatibilityContractText.Values[id] : localize(id, englishFallback);

    // Old signatures preserve their historical text without Chinese source literals.
    // Live request assembly passes the package-selected resolver explicitly.
    private static class CompatibilityContractText
    {
        internal static readonly Dictionary<string, string> Values = Load();
        private static Dictionary<string, string> Load()
        {
            using (var stream = typeof(WorldDiplomacyPromptContractRules).Assembly.GetManifestResourceStream("AnimusForge.Diplomacy.PromptCompatibility.json"))
            using (var reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8))
                return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
        }
    }

	public static string StablePromptHash(string text)
	{
		unchecked
		{
			ulong hash = AppendStablePromptHash(1469598103934665603UL, text);
			return hash.ToString("x16", CultureInfo.InvariantCulture);
		}
	}

	public static string StablePromptHashPair(string first, string second)
	{
		unchecked
		{
			ulong hash = AppendStablePromptHash(1469598103934665603UL, first);
			hash = AppendStablePromptHash(hash, "\n");
			hash = AppendStablePromptHash(hash, second);
			return hash.ToString("x16", CultureInfo.InvariantCulture);
		}
	}

	public static string StablePromptHashMessagePrefix(IReadOnlyList<WorldDiplomacyLlmMessage> messages, int messageCount)
	{
		unchecked
		{
			ulong hash = 1469598103934665603UL;
			int count = Math.Min(Math.Max(0, messageCount), messages?.Count ?? 0);
			for (int i = 0; i < count; i++)
			{
				if (i > 0) hash = AppendStablePromptHash(hash, "\n");
				WorldDiplomacyLlmMessage message = messages[i];
				hash = AppendStablePromptHash(hash, message?.Role);
				hash = AppendStablePromptHash(hash, ":");
				hash = AppendStablePromptHash(hash, message?.Content);
			}
			return hash.ToString("x16", CultureInfo.InvariantCulture);
		}
	}

	public static ulong AppendStablePromptHash(ulong hash, string text)
	{
		unchecked
		{
			foreach (char ch in text ?? "")
			{
				hash ^= ch;
				hash *= 1099511628211UL;
			}
			return hash;
		}
	}

	public static bool IsValidSemanticRepairMessageChain(WorldDiplomacyJob job)
	{
		List<WorldDiplomacyLlmMessage> messages = job?.LlmMessages;
		if (job == null
			|| job.SemanticRepairAttempts <= 0
			|| job.SemanticRepairAttempts > MaxGeneratedDraftRepairAttempts
			|| messages == null
			|| messages.Count != 3 + 2 * job.SemanticRepairAttempts
			|| !WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate")) return false;
		WorldDiplomacyLlmMessage first = messages[0];
		WorldDiplomacyLlmMessage history = messages[1];
		WorldDiplomacyLlmMessage originalTail = messages[2];
		WorldDiplomacyLlmMessage rejected = messages[messages.Count - 2];
		WorldDiplomacyLlmMessage correction = messages[messages.Count - 1];
		return string.Equals(first?.Role, "system", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(first?.Content ?? "", job.SystemPrompt ?? "", StringComparison.Ordinal)
			&& string.Equals(history?.Role, "system", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(StablePromptHashPair(first?.Content, history?.Content), job.HistoryPrefixHash ?? "", StringComparison.Ordinal)
			&& string.Equals(originalTail?.Role, "user", StringComparison.OrdinalIgnoreCase)
			&& (originalTail?.Content ?? "").IndexOf("【MODE=DECLARE】", StringComparison.Ordinal) >= 0
			&& string.Equals(rejected?.Role, "assistant", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(correction?.Role, "user", StringComparison.OrdinalIgnoreCase)
			&& (correction?.Content ?? "").IndexOf("【MODE=DECLARE】", StringComparison.Ordinal) >= 0
			&& string.Equals(correction?.Content ?? "", job.UserPrompt ?? "", StringComparison.Ordinal);
	}

	public static bool HasCurrentCanonicalPromptContract(WorldDiplomacyJob job)
	{
		if (!WorldDiplomacyRoundLifecycleRules.UsesCanonicalHistory(job)) return true;
		string expectedMode = WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "compress")
			? "【MODE=COMPACT】"
			: "【MODE=DECLARE】";
		string modePrompt = job.SemanticRepairAttempts > 0 && job.LlmMessages?.Count > 0
			? job.LlmMessages[job.LlmMessages.Count - 1]?.Content ?? ""
			: job.UserPrompt ?? "";
		string systemPrompt = job.SystemPrompt ?? "";
		return string.Equals((job.CacheAffinityKey ?? "").Trim(), CanonicalHistoryCacheAffinityKey, StringComparison.Ordinal)
			&& MarkerIndex(systemPrompt, DiplomaticDeclarationWritingContractMarker) >= 0
			&& MarkerIndex(systemPrompt, DiplomacyModeDispatchContractMarker) >= 0
			&& MarkerIndex(systemPrompt, DiplomaticDeclarationModeContractMarker) >= 0
			&& MarkerIndex(systemPrompt, CanonicalHistoryCompressionModeContractMarker) >= 0
			&& MarkerIndex(systemPrompt, CanonicalHistoryContractMarker) >= 0
			&& modePrompt.IndexOf(expectedMode, StringComparison.Ordinal) >= 0
			&& MarkerIndex(modePrompt, DiplomaticDeclarationWritingContractMarker) < 0
			&& MarkerIndex(modePrompt, DiplomacyModeDispatchContractMarker) < 0
			&& MarkerIndex(modePrompt, DiplomaticDeclarationModeContractMarker) < 0
			&& MarkerIndex(modePrompt, CanonicalHistoryCompressionModeContractMarker) < 0;
	}

	public static bool TryExtractCommonContractFromJob(WorldDiplomacyJob job, out string contract)
	{
		contract = "";
		if (job == null) return false;
		if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate"))
		{
			string generateMessageSystem = job.LlmMessages?.FirstOrDefault(x => x != null
				&& string.Equals(x.Role, "system", StringComparison.OrdinalIgnoreCase))?.Content;
			return TryExtractCommonContractBeforeMarker(generateMessageSystem, DiplomaticDeclarationWritingContractMarker, out contract)
				|| TryExtractCommonContractBeforeMarker(job.SystemPrompt, DiplomaticDeclarationWritingContractMarker, out contract)
				|| TryExtractCommonContractBeforeMarker(generateMessageSystem, CanonicalHistoryContractMarker, out contract)
				|| TryExtractCommonContractBeforeMarker(job.SystemPrompt, CanonicalHistoryContractMarker, out contract);
		}
		string marker;
		if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "round_plan"))
		{
			marker = RoundPlanTaskMarker;
		}
		else if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "analyze"))
		{
			marker = DiplomacyAnalysisTaskMarker;
		}
		else
		{
			return false;
		}
		string messageSystem = job.LlmMessages?.FirstOrDefault(x => x != null
			&& string.Equals(x.Role, "system", StringComparison.OrdinalIgnoreCase))?.Content;
		return TryExtractCommonContractBeforeMarker(messageSystem, marker, out contract)
			|| TryExtractCommonContractBeforeMarker(job.SystemPrompt, marker, out contract);
	}

	public static bool TryExtractCommonContractBeforeMarker(string systemPrompt, string marker, out string contract)
	{
		contract = "";
		if (string.IsNullOrEmpty(systemPrompt) || string.IsNullOrEmpty(marker)) return false;
		int markerIndex = MarkerIndex(systemPrompt, marker, true);
		if (markerIndex < 0) return false;
		contract = systemPrompt.Substring(0, markerIndex).TrimEnd('\r', '\n');
		return true;
	}

	public static StringBuilder CreateSystemPromptBuilder(string commonContract)
	{
		StringBuilder sb = new StringBuilder();
		if (string.IsNullOrEmpty(commonContract)) return sb;
		sb.Append(commonContract);
		char tail = commonContract[commonContract.Length - 1];
		if (tail != '\r' && tail != '\n') sb.AppendLine();
		return sb;
	}

	public static void AppendDiplomaticDeclarationWritingContract(StringBuilder sb, int minimumCharacters, int maximumCharacters)
		=> AppendDiplomaticDeclarationWritingContract(sb, minimumCharacters, maximumCharacters, null);

    // The package supplies the prose unit (CN characters / EN words). This is
    // output capacity, not a required spend or publication length check.
    public static int GetDeclarationOutputTokenBudget(int maximumLength, int minimumBudget)
        => Math.Max(minimumBudget, Math.Max(1, Math.Min(1000, maximumLength)) * 4 + 1024);

	public static void AppendDiplomaticDeclarationWritingContract(StringBuilder sb, int minimumCharacters, int maximumCharacters, Func<string, string, string> localize)
	{
		if (sb == null) return;
		sb.AppendLine(MarkerForPrompt(DiplomaticDeclarationWritingContractMarker, localize));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_000", "This section applies only in MODE=DECLARE. In MODE=COMPACT, ignore it and strictly follow the fixed MODE=COMPACT contract in the system message and the trailing dynamic parameters."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_001", "Do not discuss behind-the-scenes scheduling, generation rules, candidate plans, data judgments or technical processes. Internal fields belong only in the JSON structure and must never become document prose."));
		sb.AppendLine(DiplomacyMiscPromptTexts.GetRequired("Declaration_DefaultStyle"));
		sb.Append(ContractText(localize, "AF_DIP_Contract_002", "Suggested body length: "))
			.Append(minimumCharacters.ToString(CultureInfo.InvariantCulture))
			.Append(ContractText(localize, "AF_DIP_Contract_003", " to "))
			.Append(maximumCharacters.ToString(CultureInfo.InvariantCulture))
			.AppendLine(ContractText(localize, "AF_DIP_Contract_004", " words. This is a preference, not a minimum or maximum for publication. Simple replies may be shorter; complete terms and clear meaning take precedence over length. Do not pad or omit terms to fit the range."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_005", "Include a final decision unless further discussion is still needed."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_008", "Do not copy internal decision-making assessments into the body. Do not disclose war-progress score leads, peace openness, disadvantage scores, relation points or military strength values. Express natural judgments supported by battle reports and actual outcomes instead. Exact tribute, truce duration and other formal terms are exempt from this restriction."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_009", "Geographical descriptions must follow the current geographical relationships in the user message. Only kingdoms explicitly marked as sharing a border may call each other neighbors or border states or claim a common frontier. When marked as not bordering, do not describe them as neighbors or invent a border dispute, even if they are close partners, share a culture, once ruled adjacent territory or are in the same negotiations. Do not disclose internal distance tiers or map distances in the body."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_010", "Do not copy other documents in the global long-term diplomatic history. The document must be entirely original."));
	}

	public static string BuildDiplomaticDeclarationModeContract()
		=> BuildDiplomaticDeclarationModeContract(null);

	public static string BuildDiplomaticDeclarationModeContract(Func<string, string, string> localize)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_011", "[Unified task: public diplomatic declaration] Based on the profiles in the user message, draft a diplomatic document for the current issuing kingdom, authorized or signed by its ruler and addressed to other kingdoms."));
        sb.AppendLine(ContractText(localize, "AF_DIP_Contract_012", "If the request material states player_diplomacy=true, this document involves the player. The following autonomous-AI restrictions do not apply: action or target counts, successive different actions against the same target, cooldowns, war counts, civil war, answering a particular offer first, ending negotiations or round turns. Do not alter meaning or cancel publication because of those limits. Still use real kingdom IDs and accurately bind actual proposal sources. List each action separately and preserve all explicit actions in order. Pure-AI documents follow the autonomous restrictions below."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_013", "A declaration may simultaneously perform one action each against 1 to 4 kingdoms; each target may appear only once in actions. For the opening, the issuing kingdom independently selects from the candidates."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_014", "Do not speak for other kingdoms or invent territory, institutions, family relationships, battles or hard events unsupported by the input."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_015", "warning means condemnation, not advice, concern or a friendly reminder. The body must demand an end to specific hostile or military behavior and state that refusal will escalate to an ultimatum or formal declaration of war. ultimatum means a war ultimatum."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_016", "Fill actions[].intent only with an action currently available for that target. The system determines internal fields from the target and intent. statement must be used alone."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_017", "Do not use statement in the first document of a round. It is available only when listed among the current options in a later AI turn. In that case negotiation_move is required and must describe a meaningful negotiation move, not a mechanical diplomatic act. Allowed values: question|clarification|state_position|justify_demand|acknowledge_concern|dispute_claim|counterproposal|conditional_acceptance|partial_concession|request_concession|revise_terms|request_delay|consult_court|set_deadline|final_offer|withdraw_offer|end_negotiation|declare_deadlock. Do not repeat the previous turn verbatim."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_018", "accept_* means unconditional acceptance of all original terms with immediate effect. Do not choose it when bargaining, approval or changed terms are still required. Wording for establishing versus ending alliances or trade must not contradict the chosen action."));
		sb.AppendLine(DiplomacyMiscPromptTexts.GetRequired("Declaration_SemanticStance"));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_019", "Prioritize national survival and practical interests, long-term strategy, the war situation and bilateral relations, then national character; only afterward use the target's international reputation as evidence of credibility. National character determines the weight placed on reliability and honoring commitments; long-term strategy determines how to use that judgment. International reputation adjusts trust in foreign commitments, cooperation terms and diplomatic risk only. It must not independently trigger or prevent war or override territorial, security or containment goals. High reputation does not imply peacefulness, likability or an absence of expansion; low reputation is not automatically a cause for war. A powerful expansionist kingdom that honors its commitments may still be a threat requiring containment."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_020", "This kingdom's international reputation is strategic capital that dissipates over time and requires sustained conduct to maintain, not the supreme goal or a score that must be maximized. National character determines the price it will pay for credibility, while long-term strategy determines the desired tier. Kingdoms that value trade, alliances, commitments or mediation usually prize high reputation; pragmatic, expansionist or endangered kingdoms may deliberately accept losses for survival, territory, security or containment. When given a diplomatic speaking opportunity and the actual situation permits, proactively maintain reputation by honoring commitments, offering executable cooperation, accepting responsibility, mediating effectively or making other substantive declarations. Gains require actual fulfillment, executable concessions, costs borne or real results. Repeated polite statements, empty promises and declarations without progress cannot farm reputation. Assess gains and losses only after drafting the declaration; they must not force the kingdom to change its position or remain silent."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_021", "The title should concisely summarize the event or decision, normally in no more than 12 words."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_022", "First complete title, body and actions independently, then assess the drafted declaration's international reputation impact afterward. The assessment must not alter, soften or cancel the declaration or silence the kingdom. Every declaration must receive a nonzero assessment: only -10 to -1 or 1 to 10, never 0. Honoring commitments, executable compromises, effective mediation, taking responsibility and reliable cooperation usually increase reputation; breaches, repeatedly changing terms, deception, delay, abuse of threats and truce violations usually reduce it. For simply rejecting demands, assign the minimum magnitude of +/-1 based on timeliness, clarity, consistency and whether reasons allow negotiations to continue. Repeated empty positions with no new terms, explanation, action or negotiating progress receive -1; politeness alone cannot repeatedly earn reputation. reason contains only a brief factual explanation."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_023", "When the user message asks to determine participants for this diplomatic event in the same response, round_plan.selected_kingdom_ids must contain every action target."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_024", "Output only one JSON object, without code fences:"));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_025", "title and body must fully express every action in actions."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_026", "{\"title\":\"brief title\",\"body\":\"complete diplomatic document\",\"actions\":[{\"target_kingdom_id\":\"target ID\",\"intent\":\"currently available action\",\"commitment\":\"non_binding|proposal|acceptance|rejection|binding\",\"negotiation_move\":\"required for statement, otherwise empty string\",\"peace_terms\":{}}],\"mentioned_kingdom_ids\":[],\"tone\":\"conciliatory|neutral|firm|hostile\",\"round_plan\":{\"topic\":\"topic or empty\",\"selected_kingdom_ids\":[\"ID\"]},\"international_reputation_delta\":1,\"international_reputation_reason\":\"post-drafting assessment reason\"}"));
		return sb.ToString().TrimEnd();
	}

	public static string BuildCanonicalHistoryCompressionModeContract()
		=> BuildCanonicalHistoryCompressionModeContract(null);

	public static string BuildCanonicalHistoryCompressionModeContract(Func<string, string, string> localize)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_027", "Compress only the preceding global long-term diplomatic history. Do not draft declarations, execute diplomatic actions or cite dynamic national states outside the trailing parameters."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_028", "Merge the old snapshot and increments, preserving key changes in world reports, policy lifecycles, each kingdom's final declared position, links between proposals and responses, and diplomatic results confirmed by game mechanisms. Keep proposals, acceptance, rejection and confirmed outcomes distinct; do not turn unexecuted claims into actual state. Repetition may be merged, but facts must not be changed or invented."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_029", "Within the total budget, the program separately retains a short section of recent confirmed outcomes and response links. summary must still cover the full time range, especially earlier key outcomes, without copying every internal ID. summary must not exceed the target ceiling given at the end."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_030", "Output only one JSON object, without code fences or explanations. Set covered_through_sequence exactly to the trailing coverage cutoff seq: {\"summary\":\"compressed long-term diplomatic history\",\"covered_through_sequence\":0}"));
		return sb.ToString().TrimEnd();
	}

	public static string BuildCanonicalHistorySystemPrompt(string commonContract, int minimumCharacters, int maximumCharacters)
		=> BuildCanonicalHistorySystemPrompt(commonContract, minimumCharacters, maximumCharacters, null);

	public static string BuildCanonicalHistorySystemPrompt(string commonContract, int minimumCharacters, int maximumCharacters, Func<string, string, string> localize)
	{
		StringBuilder sb = CreateSystemPromptBuilder(commonContract);
		AppendDiplomaticDeclarationWritingContract(sb, minimumCharacters, maximumCharacters, localize);
		sb.AppendLine(MarkerForPrompt(DiplomacyModeDispatchContractMarker, localize));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_031", "MODE at the end of the final user message is the sole task selector. Execute only the matching fixed task contract and ignore every other MODE contract. Do not mix their actions, fields or JSON structures. The trailing user message supplies only the current dynamic facts, parameters and MODE; it does not override this dispatch rule."));
		sb.AppendLine(MarkerForPrompt(DiplomaticDeclarationModeContractMarker, localize));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_032", "Execute this contract only in MODE=DECLARE; ignore this section entirely in MODE=COMPACT."));
		sb.AppendLine(BuildDiplomaticDeclarationModeContract(localize));
		sb.AppendLine(MarkerForPrompt(CanonicalHistoryCompressionModeContractMarker, localize));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_033", "Execute this contract only in MODE=COMPACT; ignore this section entirely in MODE=DECLARE."));
		sb.AppendLine(BuildCanonicalHistoryCompressionModeContract(localize));
		sb.AppendLine(MarkerForPrompt(CanonicalHistoryContractMarker, localize));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_034", "The next system message contains historical facts. In MODE=DECLARE it provides only this event and a recent relevant history window, not the complete archive. In MODE=COMPACT it provides the continuous global history prefix to compress. MODE in the final user message determines the sole task and output structure. Current dynamic state takes precedence over conflicting history."));
		return PromptComposerTexts.ApplyOutputLanguage(sb.ToString().TrimEnd());
	}

	public static string BuildGenerationSystemPrompt(string commonContract, int minimumCharacters, int maximumCharacters)
		=> BuildGenerationSystemPrompt(commonContract, minimumCharacters, maximumCharacters, null);

	public static string BuildGenerationSystemPrompt(string commonContract, int minimumCharacters, int maximumCharacters, Func<string, string, string> localize)
	{
		return BuildCanonicalHistorySystemPrompt(commonContract, minimumCharacters, maximumCharacters, localize);
	}

	public static string BuildRelayGenerationSystemPrompt(string commonContract, int minimumCharacters, int maximumCharacters)
		=> BuildRelayGenerationSystemPrompt(commonContract, minimumCharacters, maximumCharacters, null);

	public static string BuildRelayGenerationSystemPrompt(string commonContract, int minimumCharacters, int maximumCharacters, Func<string, string, string> localize)
	{
		return BuildCanonicalHistorySystemPrompt(commonContract, minimumCharacters, maximumCharacters, localize);
	}

	public static string BuildDeclareModePrompt(string dynamicPrompt)
		=> BuildDeclareModePrompt(dynamicPrompt, null);

	public static string BuildDeclareModePrompt(string dynamicPrompt, Func<string, string, string> localize)
	{
		StringBuilder sb = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(dynamicPrompt)) sb.AppendLine(dynamicPrompt.Trim());
		sb.AppendLine("【MODE=DECLARE】");
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_035", "Activate only the fixed MODE=DECLARE task contract in the first system message and output only the JSON object specified by that contract."));
		return sb.ToString().TrimEnd();
	}

	public static void AppendRoundSubstantiveProgressRequirement(StringBuilder sb, WorldDiplomacyRound round, int age, int targetDays)
	    => AppendRoundSubstantiveProgressRequirement(sb, round, age, targetDays, null);

	public static void AppendRoundSubstantiveProgressRequirement(StringBuilder sb, WorldDiplomacyRound round, int age, int targetDays, Func<string, string, string> localize)
	{
		if (sb == null || round == null) return;
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_059", "Public events provide only background that has already occurred; they do not predetermine outcomes. This kingdom may select one currently available action for each of one or more targets. Generating an action does not mean its game mechanism has executed successfully."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_060", "Explicit diplomatic attempts already made=") + Math.Max(0, round.SubstantiveProgressCount).ToString(CultureInfo.InvariantCulture)
			+ ContractText(localize, "AF_DIP_Contract_061", "; of these, attempts aimed at changing relations=") + Math.Max(0, round.DiplomaticActionAttemptCount).ToString(CultureInfo.InvariantCulture)
			+ ContractText(localize, "AF_DIP_Contract_062", "; diplomatic actions that have formally taken effect=") + Math.Max(0, round.ExecutedActionCount).ToString(CultureInfo.InvariantCulture) + ContractText(localize, "AF_DIP_Contract_063", "."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_064", "Consecutive complete exchanges without a game-mechanism diplomatic action=") + Math.Max(0, round.ConsecutiveNoActionPasses).ToString(CultureInfo.InvariantCulture) + "。");
		if (WorldDiplomacyRoundLifecycleRules.ShouldForceTerminalMove(round.ConsecutiveNoActionPasses)
			|| round.FinalActionOpportunityIssued)
		{
			sb.AppendLine(ContractText(localize, "AF_DIP_Contract_065", "This document is in the final resolution stage. Choose a final offer, acceptance, rejection, concession, condemnation, ultimatum or another currently available game-mechanism diplomatic action. If there is truly no intention to continue, use only statement with negotiation_move set to end_negotiation or declare_deadlock. Do not produce ordinary discussion, delay or repeated positions, and do not invent results as already effective."));
		}
		else if (round.ConsecutiveNoActionPasses == 1)
		{
			sb.AppendLine(ContractText(localize, "AF_DIP_Contract_066", "The previous complete exchange triggered no game-mechanism diplomatic action. If this document continues negotiations, it must offer new terms, answer a specific question, make a partial concession, revise terms, set a deadline or make an explicit counterproposal. Do not merely rephrase the previous position. statement must be used alone."));
		}
		else
		{
			sb.AppendLine(ContractText(localize, "AF_DIP_Contract_067", "These negotiations still allow one round of substantive negotiation without triggering a game mechanism. You may ask questions, clarify, explain reasons, address concerns or propose conditions, or directly choose a currently available game-mechanism diplomatic action."));
		}
	}

	public static string BuildAnalysisSystemPrompt(string commonContract)
	    => BuildAnalysisSystemPrompt(commonContract, null);

	public static string BuildAnalysisSystemPrompt(string commonContract, Func<string, string, string> localize)
	{
		StringBuilder sb = CreateSystemPromptBuilder(commonContract);
		sb.AppendLine(MarkerForPrompt(DiplomacyAnalysisTaskMarker, localize) + ContractText(localize, "AF_DIP_Contract_103", "MODE=ANALYZE in the final message determines the task and output structure."));
		sb.AppendLine(DiplomacyMiscPromptTexts.GetRequired("Declaration_SemanticStance"));
		return PromptComposerTexts.ApplyOutputLanguage(sb.ToString().TrimEnd());
	}

	public static string BuildAnalysisModeContract()
	    => BuildAnalysisModeContract(null);

	public static string BuildAnalysisModeContract(Func<string, string, string> localize)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_036", "This player declaration has already been officially published. Only understand and extract its meaning; do not decide whether publication is allowed or reject the document because it contains no game-mechanism action. The player's stylistic preferences do not participate in semantic adjudication."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_037", "warning means condemnation, not advice, concern or a friendly reminder. Use it only when the text explicitly demands an end to specific hostile or military behavior and states that refusal will escalate to an ultimatum or war. ultimatum means a war ultimatum; use declare_war when declaring war outright."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_038", "Prioritize actual diplomatic actions that register or execute a game-mechanism state. Do not invent actions for the author. If the body contains none, still return status=success: use statement for an ordinary position, condemn for general condemnation, apology for an explicit formal apology, and concession for an explicit formal concession. These public meanings do not imply declaring war, proposing, accepting or rejecting."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_039", "If the material lists formal proposals currently awaiting this kingdom's response, explicit acceptance or rejection must use the corresponding accept_* or reject_* and bind the original proposing kingdom and source. Accepting an original peace proposal inherits its terms; changed terms must be recognized as a new propose_peace. The player may propose independent diplomatic actions. Round turns or legal-action lists in the material must not alter semantic extraction; game mechanisms determine whether execution is permitted."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_040", "Use comply_ultimatum as intent only when the body explicitly, affirmatively and unconditionally complies with a pending condemnation or ultimatum listed in the material. Use binding for commitment, set primary_target_kingdom_id to the issuing kingdom, and put the source document ID for the current stage in responding_to_threat_document_id. This document constitutes the target kingdom's one-time decision. Ambiguity, silence, conditions, counterconditions, merely wishing to continue negotiations, or any other intent all mean no compliance; leave that field empty."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_041", "Also generate title_summary: concisely summarize the announcement's core from the issuing ruler's standpoint, without using a letter heading, in no more than 12 words."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_042", "addressed_kingdom_ids lists kingdoms directly named, asked to respond, or subject to formal claims; mentioned_kingdom_ids lists only kingdoms discussed without being directly asked to respond. Use only kingdom IDs supplied in the user message."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_043", "For propose_peace, extract into peace_terms only terms explicitly stated in the body; for accept_peace, the system inherits the original proposal. Extract amounts, durations, territorial cessions and their direction exactly; do not remove or alter them because they exceed suggested amounts or permitted lists. When no duration is agreed, default to 100 days if tribute exists and 0 days otherwise."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_044", "After semantic extraction, assess the already published declaration's international reputation impact afterward. This assessment must never change or invalidate the declaration. Every declaration must receive a nonzero assessment, only -10 to -1 or 1 to 10, never 0. Honoring commitments, executable compromises, effective mediation, accepting responsibility and reliable cooperation usually increase reputation; breaches, repeatedly changing terms, deception, delay, abuse of threats and truce violations usually decrease it. For merely rejecting demands, assign the minimum magnitude of +/-1 based on timeliness, clarity, consistency and whether an explanation allows negotiations to continue. Repeated empty statements without new terms, explanations, actions or negotiating progress receive -1. reason contains only a brief factual explanation."));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_045", "When a suzerain voluntarily releases a direct subject kingdom and permits its independence, use release_subject with commitment=binding and requires_response=false. This is unilateral termination by the suzerain of the current subject treaty, not break_alliance, Alliance or a new subject proposal, and does not require the subject's consent. Extract this action only from an explicit announced release decision; hypotheticals, questions, negations and a subject proclaiming its own independence do not count. The system binds the treaty credentials for player declarations; the model must not fill in or change those credentials."));
        sb.AppendLine(ContractText(localize, "AF_DIP_Contract_046", "actions must list every explicit action in the execution order stated in the body. Each item contains intent, target_kingdom_id, commitment, requires_response and that action's own source/terms fields; the same target may have multiple different actions. For example, ending trade and then declaring war must list cancel_trade followed by declare_war, not just the principal action. Preserve public meanings such as statement even without a game-mechanism action. Do not omit player meaning because of AI cooldowns, war counts, civil war, rounds or action-count limits. Unused terms fields may be omitted, and sources must not be assigned to other actions. New output must carry each action's fields in the actions array; whole-document fields such as title_summary/tone/confidence/international reputation remain at the top level."));
        sb.AppendLine(ContractText(localize, "AF_DIP_Contract_047", "Example: {\"status\":\"success\",\"title_summary\":\"End trade and declare war\",\"actions\":[{\"target_kingdom_id\":\"target ID\",\"intent\":\"cancel_trade\",\"commitment\":\"binding\"},{\"target_kingdom_id\":\"target ID\",\"intent\":\"declare_war\",\"commitment\":\"binding\"}],\"tone\":\"hostile\",\"confidence\":1,\"international_reputation_delta\":-1,\"international_reputation_reason\":\"Publicly repudiating an agreement\"}"));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_048", "Output only one JSON object, without explanations or code fences:"));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_049", "{\"status\":\"success\",\"title_summary\":\"announcement summary title\",\"addressed_kingdom_ids\":[\"kingdom ID\"],\"mentioned_kingdom_ids\":[\"kingdom ID\"],\"tone\":\"conciliatory|neutral|firm|hostile\",\"confidence\":0.0,\"international_reputation_delta\":1,\"international_reputation_reason\":\"post-publication assessment reason\",\"actions\":[{\"responding_to_offer_document_id\":\"proposal source document ID or empty string\",\"responding_to_offer_action_id\":\"source action ID or empty string\",\"responding_to_threat_document_id\":\"source document ID of the condemnation or ultimatum being complied with, or empty string\",\"intent\":\"statement|condemn|apology|concession|warning|ultimatum|comply_ultimatum|propose_peace|accept_peace|reject_peace|propose_alliance|accept_alliance|reject_alliance|break_alliance|propose_trade|accept_trade|reject_trade|cancel_trade|release_subject|withdraw_offer|propose_annexation|accept_annexation|reject_annexation|propose_tributary|accept_tributary|reject_tributary|propose_garrison|accept_garrison|reject_garrison|propose_vassal|accept_vassal|reject_vassal|declare_war\",\"commitment\":\"non_binding|proposal|acceptance|rejection|binding\",\"requires_response\":true,\"treaty_terms\":{\"receiving_kingdom_id\":\"receiving kingdom or suzerain kingdom ID\",\"joining_kingdom_id\":\"joining kingdom or subject kingdom ID\"},\"peace_terms\":{\"tribute_payer_kingdom_id\":\"ID or empty\",\"tribute_receiver_kingdom_id\":\"ID or empty\",\"daily_tribute\":0,\"duration_days\":0,\"cession_from_kingdom_id\":\"ID or empty\",\"cession_to_kingdom_id\":\"ID or empty\",\"cession_settlement_id\":\"ID or empty\"},\"target_kingdom_id\":\"kingdom ID or empty string\"}]}"));
		return sb.ToString().TrimEnd();
	}

	public static string BuildTokenCompressionPrompt(string batchId, long throughSequence, long tokenCount, int summaryTargetTokens, long protectedTokens)
	    => BuildTokenCompressionPrompt(batchId, throughSequence, tokenCount, summaryTargetTokens, protectedTokens, null);

	public static string BuildTokenCompressionPrompt(string batchId, long throughSequence, long tokenCount, int summaryTargetTokens, long protectedTokens, Func<string, string, string> localize)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_050", "[Current compression parameters]"));
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_051", "Compression batch=") + (batchId ?? "") + ContractText(localize, "AF_DIP_Contract_052", "; coverage cutoff seq=") + Math.Max(0L, throughSequence).ToString(CultureInfo.InvariantCulture)
			+ ContractText(localize, "AF_DIP_Contract_053", "; current estimated tokens=") + Math.Max(0L, tokenCount).ToString(CultureInfo.InvariantCulture)
			+ ContractText(localize, "AF_DIP_Contract_054", "; recent hard-fact budget consumption tokens=") + Math.Max(0L, protectedTokens).ToString(CultureInfo.InvariantCulture)
			+ ContractText(localize, "AF_DIP_Contract_055", "; summary target ceiling tokens=") + Math.Max(1, summaryTargetTokens).ToString(CultureInfo.InvariantCulture) + "。");
		sb.AppendLine("【MODE=COMPACT】");
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_056", "Activate only the fixed MODE=COMPACT task contract in the first system message and output only the JSON object specified by that contract."));
		return sb.ToString().TrimEnd();
	}

	public static string BuildRoundCompressionSystemPrompt()
	    => BuildRoundCompressionSystemPrompt(null);

	public static string BuildRoundCompressionSystemPrompt(Func<string, string, string> localize)
	{
		return PromptComposerTexts.ApplyOutputLanguage(ContractText(localize, "AF_DIP_Contract_057", "You are a chronicler of Calradian diplomacy. Compress a diplomatic event that has naturally concluded into a global chronicle summary and atomic facts that can be filtered by source document. Do not invent facts.\n")
			+ ContractText(localize, "AF_DIP_Contract_058", "Output only JSON: {\"summary\":\"event summary\",\"facts\":[{\"text\":\"atomic fact\",\"source_document_ids\":[\"document ID\"],\"kingdom_ids\":[\"relevant kingdom ID\"]}]}"));
	}

	public static string BuildRealmInstitutionalVoiceText(string kingdomName, string cultureName, string rulerTitle, string governmentHardFact, string lore)
	    => BuildRealmInstitutionalVoiceText(kingdomName, cultureName, rulerTitle, governmentHardFact, lore, null);

	public static string BuildRealmInstitutionalVoiceText(string kingdomName, string cultureName, string rulerTitle, string governmentHardFact, string lore, Func<string, string, string> localize)
	{
		return "RealmInstitutionalVoice{kingdom=" + kingdomName
			+ ",culture=" + cultureName
			+ ",ruler_title_hard_fact=" + WorldDiplomacyTextRules.CompactPromptFact(rulerTitle, 80)
			+ ",government_hard_fact=" + WorldDiplomacyTextRules.CompactPromptFact(governmentHardFact, 520)
			+ ",imported_lore=" + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(WorldDiplomacyTextRules.CompactPromptFact(lore, 1100), ContractText(localize, "AF_DIP_Contract_104", "No match; use only neutral terms such as kingdom, court, nobles and subjects. Do not invent a specific system of government."))
			+ ContractText(localize, "AF_DIP_Contract_105", ",precedence=Hard facts take precedence over retrieved chronicle excerpts. Discard conflicting excerpts. Institutional names must not be used as the ruler's personal title.")
			+ "}";
	}

	public static void AppendVoiceTrait(List<string> target, int value, string positive, string negative)
	{
		if (value > 0)
		{
			target?.Add(positive);
		}
		else if (value < 0)
		{
			target?.Add(negative);
		}
	}


	public static string BuildCurrentLegalDiplomaticOptions(
		IReadOnlyDictionary<string, List<string>> actionsByTarget)
		=> BuildCurrentLegalDiplomaticOptions(actionsByTarget, null);

	public static string BuildCurrentLegalDiplomaticOptions(
		IReadOnlyDictionary<string, List<string>> actionsByTarget, Func<string, string, string> localize)
	{
		List<string> lines = (actionsByTarget ?? new Dictionary<string, List<string>>())
			.Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Value?.Count > 0)
			.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
			.Select(x => x.Key + "=" + string.Join("/", x.Value))
			.ToList();
		return lines.Count == 0
			? ContractText(localize, "AF_DIP_Contract_106", "Currently available actions: none; do not generate a filler declaration.")
			: ContractText(localize, "AF_DIP_Contract_107", "Currently available actions: ") + string.Join("；", lines) + "。";
	}

	public static string DescribePotentialDiplomaticActions(IEnumerable<string> intents)
	    => DescribePotentialDiplomaticActions(intents, null);

	public static string DescribePotentialDiplomaticActions(IEnumerable<string> intents, Func<string, string, string> localize)
	{
		List<string> labels = WorldDiplomacyRoundLifecycleRules.NormalizeOrderedIdList((intents ?? Enumerable.Empty<string>())
			.Select(WorldDiplomacyIntentVocabulary.NormalizeIntent));
		return labels.Count == 0 ? ContractText(localize, "AF_DIP_Contract_108", "No actual diplomatic action can be published in the current technical state") : string.Join("、", labels);
	}
	internal static readonly string KingdomStrategicProfileMarkerPrefix = CompatibilityContractText.Values["AF_DIP_Compatibility_007"];
	internal static readonly string KingdomStrategicIntentRule = CompatibilityContractText.Values["AF_DIP_Compatibility_008"];

public static bool GenerationJobContainsKingdomStrategicProfile(WorldDiplomacyJob job, string authorId, string marker, string currentProfilePrompt)
	{
		if (job == null || string.IsNullOrEmpty(authorId) || string.IsNullOrEmpty(marker) || string.IsNullOrEmpty(currentProfilePrompt)) return false;
		if (job.LlmMessages?.Count > 0)
		{
			for (int index = job.LlmMessages.Count - 1; index >= 0; index--)
			{
				WorldDiplomacyLlmMessage message = job.LlmMessages[index];
				if (message == null || !string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase)) continue;
				string content = message.Content ?? "";
				int markerIndex = content.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
				if (markerIndex < 0) continue;
				if (content.IndexOf(currentProfilePrompt, markerIndex, StringComparison.Ordinal) != markerIndex) return false;
				message.StrategicProfileKingdomId = authorId;
				return true;
			}
			return false;
		}
		string userPrompt = job.UserPrompt ?? "";
		int promptMarkerIndex = userPrompt.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
		return promptMarkerIndex >= 0
			&& userPrompt.IndexOf(currentProfilePrompt, promptMarkerIndex, StringComparison.Ordinal) == promptMarkerIndex;
	}
public static string BuildKingdomStrategicProfileMarker(string kingdomId)
	{
		return KingdomStrategicProfileMarkerPrefix + (kingdomId ?? "").Trim() + "】";
	}
public static string UpsertKingdomStrategicProfilePrompt(string existing, string profilePrompt, string authorId, Func<string, string, string> localize = null)
	{
		if (string.IsNullOrEmpty(existing)) return profilePrompt ?? "";
		string marker = BuildKingdomStrategicProfileMarker(authorId);
		int markerIndex = existing.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
		if (markerIndex < 0) return InsertKingdomStrategicProfilePrompt(existing, profilePrompt, authorId);
		string endRule = ContractText(localize, "AF_DIP_Compatibility_008", "Find reasons for the long-term strategy, especially war, based on current circumstances; use real interests, disputes or security concerns as public reasons.");
		int ruleIndex = existing.IndexOf(endRule, markerIndex, StringComparison.Ordinal);
		if (ruleIndex < 0 && localize != null)
		{
			endRule = KingdomStrategicIntentRule;
			ruleIndex = existing.IndexOf(endRule, markerIndex, StringComparison.Ordinal);
		}
		if (ruleIndex < 0) return InsertKingdomStrategicProfilePrompt(existing, profilePrompt, authorId);
		int endIndex = ruleIndex + endRule.Length;
		string prefix = existing.Substring(0, markerIndex);
		string suffix = existing.Substring(endIndex).TrimStart('\r', '\n');
		if (!prefix.EndsWith("\n", StringComparison.Ordinal)) prefix += "\n";
		return prefix + profilePrompt.TrimEnd('\r', '\n') + "\n" + suffix;
	}
public static string InsertKingdomStrategicProfilePrompt(string existing, string profilePrompt, string authorId)
	{
		if (string.IsNullOrEmpty(existing)) return profilePrompt ?? "";
		if (string.IsNullOrEmpty(profilePrompt)) return existing;

		int insertionIndex = FindKingdomStrategicProfileInsertionIndex(existing, authorId);
		if (insertionIndex < 0)
		{
			return existing.EndsWith("\n", StringComparison.Ordinal)
				? existing + "\n" + profilePrompt
				: existing + "\n\n" + profilePrompt;
		}

		string prefix = existing.Substring(0, insertionIndex);
		string suffix = existing.Substring(insertionIndex);
		if (!prefix.EndsWith("\n", StringComparison.Ordinal)) prefix += "\n";
		return prefix + profilePrompt.TrimEnd('\r', '\n') + "\n" + suffix;
	}
public static int FindKingdomStrategicProfileInsertionIndex(string prompt, string authorId)
	{
		if (string.IsNullOrEmpty(prompt)) return -1;

		string institutionalHeading = CompatibilityContractText.Values["AF_DIP_Compatibility_009"];
		string familyHeading = CompatibilityContractText.Values["AF_DIP_Compatibility_010"];
		int institutionalIndex = prompt.IndexOf(institutionalHeading, StringComparison.Ordinal);
		if (institutionalIndex < 0) institutionalIndex = prompt.IndexOf("[Institutional, legitimacy and ceremonial voice of the issuing kingdom]", StringComparison.Ordinal);
		if (institutionalIndex >= 0)
		{
			int familyIndex = prompt.IndexOf(familyHeading, institutionalIndex + 1, StringComparison.Ordinal);
			if (familyIndex < 0) familyIndex = prompt.IndexOf("[Authoritative people and family relationships]", institutionalIndex + 1, StringComparison.Ordinal);
			if (familyIndex >= 0) return familyIndex;
		}

		string actorProfileHeading = CompatibilityContractText.Values["AF_DIP_Compatibility_011"];
		int actorProfileIndex = prompt.IndexOf(actorProfileHeading, StringComparison.Ordinal);
		if (actorProfileIndex >= 0)
		{
			int actorFamilyIndex = prompt.IndexOf(CompatibilityContractText.Values["AF_DIP_Compatibility_012"], actorProfileIndex + actorProfileHeading.Length, StringComparison.Ordinal);
			if (actorFamilyIndex >= 0) return actorFamilyIndex + 1;
		}

		string relayProfileHeading = CompatibilityContractText.Values["AF_DIP_Compatibility_013"];
		int relayProfileIndex = prompt.IndexOf(relayProfileHeading, StringComparison.Ordinal);
		if (relayProfileIndex >= 0)
		{
			int relayFamilyIndex = prompt.IndexOf(CompatibilityContractText.Values["AF_DIP_Compatibility_014"], relayProfileIndex + relayProfileHeading.Length, StringComparison.Ordinal);
			if (relayFamilyIndex >= 0) return relayFamilyIndex + 1;
		}

		string normalizedAuthorId = (authorId ?? "").Trim();
		if (normalizedAuthorId.Length > 0)
		{
			string participantAnchor = "-- " + normalizedAuthorId + "=";
			int participantIndex = prompt.IndexOf(participantAnchor, StringComparison.OrdinalIgnoreCase);
			if (participantIndex >= 0)
			{
				int participantEnd = prompt.IndexOf("\n-- ", participantIndex + participantAnchor.Length, StringComparison.Ordinal);
				if (participantEnd < 0) participantEnd = prompt.Length;
				int participantInstitutionIndex = prompt.IndexOf(CompatibilityContractText.Values["AF_DIP_Compatibility_015"], participantIndex, StringComparison.Ordinal);
				if (participantInstitutionIndex >= 0 && participantInstitutionIndex < participantEnd)
				{
					int participantFamilyIndex = prompt.IndexOf(CompatibilityContractText.Values["AF_DIP_Compatibility_016"], participantInstitutionIndex, StringComparison.Ordinal);
					return participantFamilyIndex >= 0 && participantFamilyIndex < participantEnd
						? participantFamilyIndex + 1
						: participantEnd;
				}
			}
		}

		// Keep MODE at the actual tail even for an unfamiliar dynamic prompt layout.
		// This also prevents a late strategic profile from separating the model from
		// the final, live legal-intent list immediately above MODE.
		return prompt.LastIndexOf("【MODE=DECLARE】", StringComparison.Ordinal);
	}
public static string ResolveCacheAffinityKey(WorldDiplomacyJob job)
	{
		if (!string.IsNullOrWhiteSpace(job?.CacheAffinityKey))
		{
			return job.CacheAffinityKey.Trim();
		}
		string kind = (job?.Kind ?? "unknown").Trim().ToLowerInvariant();
		return kind == "generate" ? kind + ":" + (job?.AuthorKingdomId ?? "") : kind;
	}
public static List<WorldDiplomacyLlmMessage> CloneLlmMessages(IEnumerable<WorldDiplomacyLlmMessage> messages)
	{
		return (messages ?? Enumerable.Empty<WorldDiplomacyLlmMessage>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Role))
			.Select(x => new WorldDiplomacyLlmMessage
			{
				Role = x.Role,
				Content = x.Content ?? "",
				StrategicProfileKingdomId = x.StrategicProfileKingdomId ?? ""
			})
			.ToList();
	}





	public static void AppendOpenOfferAnswerRequirement(StringBuilder sb, WorldDiplomacyRound round, string authorKingdomId)
	    => AppendOpenOfferAnswerRequirement(sb, round, authorKingdomId, null);

	public static void AppendOpenOfferAnswerRequirement(StringBuilder sb, WorldDiplomacyRound round, string authorKingdomId, Func<string, string, string> localize)
	{
		if (sb == null || round == null || string.IsNullOrWhiteSpace(authorKingdomId)) return;
		bool hasAnswerableOffer = (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
			.Any(x => WorldDiplomacyRoundLifecycleRules.IsOpenOfferToTarget(x, authorKingdomId));
		if (!hasAnswerableOffer) return;
		sb.AppendLine(ContractText(localize, "AF_DIP_Contract_068", "This kingdom has unanswered formal proposals and must handle them using the currently available actions. accept_* means only unconditional acceptance of all original terms with immediate effect."));
	}

	public static string BuildPolicySignalContext(WorldDiplomacyPolicySignal signal)
	    => BuildPolicySignalContext(signal, null);

	public static string BuildPolicySignalContext(WorldDiplomacyPolicySignal signal, Func<string, string, string> localize)
	{
		return ContractText(localize, "AF_DIP_Contract_109", "[Public policy event that has already occurred]\n")
			+ ContractText(localize, "AF_DIP_Contract_110", "Event key=") + (signal.SignalKey ?? "") + "\n"
			+ (signal.IssuerKingdomName ?? signal.IssuerKingdomId) + ContractText(localize, "AF_DIP_Contract_111", " has enacted \"") + (signal.PolicyName ?? ContractText(localize, "AF_DIP_Contract_112", "Unnamed policy")) + ContractText(localize, "AF_DIP_Contract_113", "\", ")
			+ ContractText(localize, "AF_DIP_Contract_114", "and this policy directly affects ") + (signal.TargetKingdomName ?? signal.TargetKingdomId) + "。\n"
			+ ContractText(localize, "AF_DIP_Contract_115", "Public policy summary: ") + (signal.PolicySummary ?? "") + "\n"
			+ (string.IsNullOrWhiteSpace(signal.DirectEffect) ? "" : ContractText(localize, "AF_DIP_Contract_116", "Direct measures affecting this kingdom: ") + signal.DirectEffect + "\n")
			+ ContractText(localize, "AF_DIP_Contract_117", "This policy is already in effect, but it has not automatically produced war, a peace treaty, an alliance or any other diplomatic outcome. Rulers may defend, assess or oppose it, demand changes, seek compensation, propose terms of exchange or exploit it for advantage.");
	}

        public static void AppendGeneratedRepairCorrection(
        StringBuilder correctionBuilder,
        string reason,
        int rejectedActionIndex,
        JObject rejectedJson,
        string authorId,
        string authorName,
        string repairTargetId,
        string repairTargetName,
        string bilateralState,
        WorldDiplomacyRoundOffer requiredPeaceOffer,
        Func<string> realmGovernmentHardFact)
            => AppendGeneratedRepairCorrection(correctionBuilder, reason, rejectedActionIndex, rejectedJson, authorId, authorName, repairTargetId, repairTargetName, bilateralState, requiredPeaceOffer, realmGovernmentHardFact, null);

        public static void AppendGeneratedRepairCorrection(
        StringBuilder correctionBuilder,
        string reason,
        int rejectedActionIndex,
        JObject rejectedJson,
        string authorId,
        string authorName,
        string repairTargetId,
        string repairTargetName,
        string bilateralState,
        WorldDiplomacyRoundOffer requiredPeaceOffer,
        Func<string> realmGovernmentHardFact, Func<string, string, string> localize)
    {
        JObject rejectedAction = rejectedActionIndex >= 0
            && rejectedJson?["actions"] is JArray rejectedActions
            && rejectedActionIndex < rejectedActions.Count
            ? rejectedActions[rejectedActionIndex] as JObject
            : null;
        string rejectedIntent = WorldDiplomacyIntentVocabulary.NormalizeIntent(WorldDiplomacyEnvelopeJsonRules.ReadString(rejectedAction ?? rejectedJson, "intent", "author_intent.intent"));
        correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_069", "[Hard-fact corrections for the unpublished draft]"));
        correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_070", "The previous assistant content is only an unpublished draft, not diplomatic history. Do not cite or continue it or assume its events have occurred."));
        correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_071", "The draft failed JSON, field or factual validation. Draft it again following the instructions below."));
        correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_072", "Current issuing kingdom=") + authorId + "=" + authorName + "。"
            + (string.IsNullOrWhiteSpace(repairTargetId) ? ContractText(localize, "AF_DIP_Contract_073", "This request may choose 1 to 4 legal targets from the original task's authorized scope, with one actual diplomatic action per target.")
                : ContractText(localize, "AF_DIP_Contract_074", "Target kingdom of the invalid item=") + repairTargetId + "=" + repairTargetName + ContractText(localize, "AF_DIP_Contract_075", "; current relationship=") + bilateralState + ContractText(localize, "AF_DIP_Contract_076", "; other legal targets may be retained.")));
        if (string.Equals(reason, "peace_intent_between_kingdoms_not_at_war", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_077", "The two sides are not currently at war. Do not propose, accept or reject peace, or describe a truce, peace settlement, withdrawal from war, restoration of territory lost in war or war compensation. Select a currently available action instead."));
        }
        else if (string.Equals(reason, "non_response_claims_offer_source", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_078", "Only accept_* or reject_* may bind a proposal source. Other actions must not supply a source and must be selected from the currently available actions."));
        }
        else if (string.Equals(reason, "comply_ultimatum_missing_source_document", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "non_compliance_claims_threat_source", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_079", "Choose comply_ultimatum only for explicit, unconditional compliance. Ambiguous responses, attached conditions and counterconditions do not constitute compliance."));
        }
        else if (string.Equals(reason, "required_peace_offer_response_missing", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(requiredPeaceOffer == null
                ? ContractText(localize, "AF_DIP_Contract_080", "The original peace proposal's state has changed. Select again only from the currently available actions.")
                : ContractText(localize, "AF_DIP_Contract_081", "actions must answer the original peace proposal: source=") + requiredPeaceOffer.SourceDocumentId
                    + "|action=" + (requiredPeaceOffer.SourceActionId ?? "")
                    + ContractText(localize, "AF_DIP_Contract_082", "|proposing kingdom=") + requiredPeaceOffer.ProposerKingdomId
                    + ContractText(localize, "AF_DIP_Contract_083", "; only accept it unchanged or explicitly reject it; actions against other legal targets may be retained."));
        }
        else if (string.Equals(reason, "multiple_peace_acceptances_have_cross_terms", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_084", "Accepting multiple original peace proposals in one document must not include territorial cessions. Retain only one acceptance involving cessions in this document; explicitly reject the other proposals or leave them for the next document."));
        }
        else if (reason.StartsWith("offer_response_", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "new_proposal_claims_third_party_offer", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_085", "Only the original proposal's target kingdom may accept or reject it, addressing the original proposing kingdom. Otherwise select a currently available action."));
        }
        else if (string.Equals(reason, "internal_metric_exposed_in_public_declaration", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_086", "The body exposed internal situation metrics. Rulers do not know war-progress scores, openness to peace, disadvantage scores, relationship points, pressure thresholds or total military-strength values. Preserve the original legal diplomatic actions and precise terms, but rewrite internal metrics as natural judgments supported by battle reports, military intelligence, territorial gains and losses, and court accounts. Tribute amounts, treaty durations and counts of real events may be retained."));
        }
        else if (string.Equals(reason, "internal_round_term_exposed_in_public_declaration", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_087", "The draft exposed internal diplomatic scheduling terminology. Public titles and bodies must not mention terms such as rounds, relays, final action opportunities or program validation. Rewrite them as the present negotiations, exchanges of documents, final positions, formal decisions or diplomatic outcomes as appropriate. Continue filling round_* fields according to the JSON contract, but never put them in title or body."));
        }
        else if (string.Equals(reason, "realm_ruler_title_conflicts_with_hard_fact", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "southern_empire_government_conflicts_with_hard_fact", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "western_empire_government_conflicts_with_hard_fact", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_089", "The draft confused the issuing kingdom's form of government with its ruler's personal title. The following hard facts about the kingdom's identity must be followed exactly:") + (realmGovernmentHardFact?.Invoke() ?? ""));
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_090", "Institutional names describe only the system of government or source of authority and cannot replace a ruler's personal title. The supreme rulers of all three Empires use Emperor or Empress. Do not call any Imperial ruler an elder, senator, consul, king, grand duke or khan. Preserve legal diplomatic content and redraft the entire document."));
        }
        else if (string.Equals(reason, "public_document_capacity_exceeded", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(DiplomacyMiscPromptTexts.GetRequired("Declaration_CapacityRepair"));
        }
        else if (string.Equals(reason, "json_parse_failed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "output_truncated", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "semantic_envelope_incomplete", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "generated_semantic_envelope_incomplete", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "empty_public_document", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_091", "Output only one complete, parseable JSON object, without code fences or explanations. Include all required contract fields, escape strings correctly, and do not leave the public body empty."));
        }
        else if (string.Equals(reason, "unsupported_intent_or_commitment", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "non_actionable_diplomatic_intent", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "intent_commitment_mismatch", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_092", "Select intent only from the currently available actions. A declaration without a game-mechanism action is permitted only when statement is currently listed."));
        }
        else if (string.Equals(reason, "statement_missing_negotiation_move", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_093", "statement requires negotiation_move and must actually perform a structured negotiating move such as question, counterproposal, request_delay, final_offer, end_negotiation or declare_deadlock. Do not merely express a vague position."));
        }
        else if (string.Equals(reason, "statement_requires_terminal_negotiation_move", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_094", "These negotiations have repeatedly lacked substantive diplomatic actions. statement may use only end_negotiation or declare_deadlock, or choose a currently available actual diplomatic action instead. Do not continue ordinary delaying."));
        }
        else if (string.Equals(reason, "target_kingdom_not_found", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "target_kingdom_not_eligible", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "referenced_kingdom_not_eligible", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "kingdom_not_in_autonomous_candidate_set", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "kingdom_not_in_targeted_generation_scope", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "kingdom_not_in_relay_route", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "diplomatic_action_has_no_target", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_095", "Within actions, target_kingdom_id may use only the currently listed legal kingdom IDs. Each target may appear only once."));
        }
        else if (string.Equals(reason, "autonomous_round_plan_incomplete", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "autonomous_round_plan_has_invalid_participant", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "autonomous_round_plan_exceeds_participant_limit", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "autonomous_round_plan_omits_direct_target", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_096", "An autonomous opening must fill both round_plan.topic and selected_kingdom_ids. Participants must come only from the candidates and must not exceed the total limit. Every target in actions must appear in selected_kingdom_ids."));
        }
        else if (string.Equals(reason, "accept_peace_changes_offer_terms", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_097", "The original peace proposal may only be accepted unchanged or explicitly rejected. Do not add or rewrite terms or propose another peace arrangement. The system automatically binds the source and original terms."));
        }
        else if ((reason ?? "").StartsWith("visible_intent_mismatch:", StringComparison.OrdinalIgnoreCase)
            || (reason ?? "").StartsWith("peace_terms_not_visible:", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_098", "JSON intentions and the public body must agree. State formal actions explicitly in the title or body. Disclose tribute, duration and territorial cessions in a peace proposal individually; do not hide them only in JSON fields. If the body contains no actual action, select a currently available action instead."));
        }
        else if ((reason ?? "").StartsWith("declare_war_not_legal:", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "intent_not_in_current_legal_action_list", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "break_alliance_without_alliance", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "cancel_trade_without_trade_agreement", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "alliance_system_unavailable", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "trade_system_unavailable", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "alliance_intent_conflicts_with_current_state", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "trade_intent_conflicts_with_current_state", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_099", "The chosen diplomatic action is incompatible with the actual current relationship. Preserve the kingdom's independent judgment, but choose targets and actions valid in the current state. Do not describe relationships that have not taken effect as accomplished facts."));
        }
        if (!string.IsNullOrWhiteSpace(rejectedIntent)
            && !string.IsNullOrWhiteSpace(repairTargetId)
            && (string.Equals(reason, "intent_not_in_current_legal_action_list", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reason, "alliance_intent_conflicts_with_current_state", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reason, "trade_intent_conflicts_with_current_state", StringComparison.OrdinalIgnoreCase)))
        {
            correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_100", "The original draft combination ") + repairTargetId + "=" + rejectedIntent + ContractText(localize, "AF_DIP_Contract_101", " is invalid and must not be output again. Choose only a currently available combination."));
        }
        correctionBuilder.AppendLine(ContractText(localize, "AF_DIP_Contract_102", "Output the complete JSON again and rewrite title and body. Do not mention the draft, corrections, the system or the errors above."));
    }

}
