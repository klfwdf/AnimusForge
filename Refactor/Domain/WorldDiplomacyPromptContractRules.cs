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
	internal const string CanonicalHistoryCacheAffinityKey = "diplomacy-history:v28";
	internal const string CanonicalHistoryContractMarker = "【AI外交长期记忆共同模式】";
	internal const string DiplomaticDeclarationWritingContractMarker = "【国家外交公文文体契约】";
	internal const string DiplomacyModeDispatchContractMarker = "【AI外交固定任务MODE分派】";
	internal const string DiplomaticDeclarationModeContractMarker = "【MODE=DECLARE 固定任务合同】";
	internal const string CanonicalHistoryCompressionModeContractMarker = "【MODE=COMPACT 固定任务合同】";
	internal const string DiplomacyAnalysisTaskMarker = "【任务：外交宣言语义裁判】";
	internal const string RoundPlanTaskMarker = "【当前任务：一次性规划外交事件参与国】";

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
			&& systemPrompt.IndexOf(DiplomaticDeclarationWritingContractMarker, StringComparison.Ordinal) >= 0
			&& systemPrompt.IndexOf(DiplomacyModeDispatchContractMarker, StringComparison.Ordinal) >= 0
			&& systemPrompt.IndexOf(DiplomaticDeclarationModeContractMarker, StringComparison.Ordinal) >= 0
			&& systemPrompt.IndexOf(CanonicalHistoryCompressionModeContractMarker, StringComparison.Ordinal) >= 0
			&& systemPrompt.IndexOf(CanonicalHistoryContractMarker, StringComparison.Ordinal) >= 0
			&& modePrompt.IndexOf(expectedMode, StringComparison.Ordinal) >= 0
			&& modePrompt.IndexOf(DiplomaticDeclarationWritingContractMarker, StringComparison.Ordinal) < 0
			&& modePrompt.IndexOf(DiplomacyModeDispatchContractMarker, StringComparison.Ordinal) < 0
			&& modePrompt.IndexOf(DiplomaticDeclarationModeContractMarker, StringComparison.Ordinal) < 0
			&& modePrompt.IndexOf(CanonicalHistoryCompressionModeContractMarker, StringComparison.Ordinal) < 0;
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
		int markerIndex = systemPrompt.LastIndexOf(marker, StringComparison.Ordinal);
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
	{
		if (sb == null) return;
		sb.AppendLine(DiplomaticDeclarationWritingContractMarker);
		sb.AppendLine("本节仅在MODE=DECLARE时生效；MODE=COMPACT时忽略本节，并严格执行system中的MODE=COMPACT固定任务合同与尾部动态参数。");
		sb.AppendLine("不得讨论幕后调度、生成规则、候选方案、数据判定或技术流程；内部字段只出现在JSON结构中，绝不能变成公文内容。");
		sb.Append("body应以王国，王庭，王国，政府等为发言主体,正文必须最少")
			.Append(minimumCharacters.ToString(CultureInfo.InvariantCulture))
			.Append("个中文字符，最多")
			.Append(maximumCharacters.ToString(CultureInfo.InvariantCulture))
			.AppendLine("个中文字符（标点计入）。");
		sb.AppendLine("文风应当符合国家设定，禁止使用文言文，内容要有最终决定（除非还需继续讨论）");
		sb.AppendLine("可以坚定、务实、冷峻、和缓或骄傲，但讥讽也必须是一个国家对另一个国家的公开评价");
		sb.AppendLine("不必讲述自身的文化与状况，只需发言不违反文化与状况，只针对当前外交局面做出必要的回应，尽量说明意图之事，言简意赅即可，不要一大堆废话。");
		sb.AppendLine("不要把供决策的后台态势照抄进正文。不能说战争进展领先多少分、议和开放度或劣势评分达到多少、关系点和战力值是多少；应改成由战报和现实结果支撑的自然判断。精确贡金、停战期限及其他正式条款不受此限制。");
		sb.AppendLine("地理称谓必须服从用户消息中的当前地理关系：只有明确标为接壤的两国才可互称邻国、边境国家或声称拥有共同边界；标为不接壤时，即使关系密切、同属一种文化、曾经统治相邻领土或正在参与同一场交涉，也不得写成邻国或边界争端。不得把供判断的距离档位和地图距离写进正文。");
		sb.AppendLine("严禁抄袭【全局长期外交历史】中的其他公文，必须完全原创公文");
	}

	public static string BuildDiplomaticDeclarationModeContract()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【统一任务：公开外交宣言】根据用户消息提供的档案，为当前发布国起草一篇由其统治者授权或署名、面向其他国家的外交措辞");
		sb.AppendLine("一篇宣言可同时对1至4个国家各执行一项动作；actions中每个对象只能出现一次。开场由发布国从候选国中自主选择；");
		sb.AppendLine("不得替其他王国发言，不得编造输入中没有支持的领土、制度、亲属关系、战斗和硬事件。");
		sb.AppendLine("warning表示谴责，不是劝告、关心或善意提醒；正文必须要求停止具体敌对或军事行为，并说明拒不停止将升级最后通牒或正式宣战。ultimatum表示战争最后通牒。");
		sb.AppendLine("actions[].intent只填写该对象当前可用的动作，内部字段由系统按对象和intent确定。statement必须单独使用。");
		sb.AppendLine("回合首篇不得使用statement；仅后续AI轮次的当前可选动作出现statement时可用。此时必须填写negotiation_move，表示有意义的谈判动作，而非机械外交行为。可用值为question|clarification|state_position|justify_demand|acknowledge_concern|dispute_claim|counterproposal|conditional_acceptance|partial_concession|request_concession|revise_terms|request_delay|consult_court|set_deadline|final_offer|withdraw_offer|end_negotiation|declare_deadlock。不得重复上一轮原话。");
		sb.AppendLine("accept_*只表示无条件接受全部原条件并立即生效；仍需议价、审批或修改条款时不得选择，且建立与解除同盟或贸易的措辞不得相反。");
		sb.AppendLine("决策顺序是国家生存与现实利益、长期战略、战争局势与双边关系、国家性格，最后才以对象国国际声誉作为可信度证据。国家性格决定本国多看重守约与可靠，长期战略决定如何利用这份判断；国际声誉只调整对外国承诺的信任、合作条件与外交风险，不得单独触发或阻止宣战，不得覆盖领土、安全或遏制目标。高声誉不等于爱好和平、值得喜欢或不会扩张，低声誉也不自动构成宣战理由；一个守约但强大的扩张国仍可能是必须遏制的威胁。");
		sb.AppendLine("本国国际声誉是会随时间消散、需要由持续行为维护的战略资本，不是最高目标，也不是必须最大化的分数。国家性格决定愿意为信誉付出多少代价，长期战略决定希望维持何种档位：重视贸易、联盟、守约或调停的国家通常更珍惜高声誉，务实、扩张或危急中的国家可以为了生存、领土、安全与遏制主动承受声誉损失。在获得外交发言机会且现实局势允许时，可主动用履行承诺、提出可执行合作、承担责任、有效调停或其他有实际内容的宣言维护声誉；提高声誉必须由实际履约、可执行让步、承担代价或现实成果支撑，重复礼貌表态、空洞承诺和没有进展的宣言不能刷取声誉。声誉得失只在宣言拟定后评估，不得反过来强迫国家改口或沉默。");
		sb.AppendLine("标题应简洁概括事件或决定，通常不超过20个字。");
		sb.AppendLine("先独立完成title、body与actions，再对这篇已经拟定的宣言做事后国际声誉评估。评估不能反过来改变、软化、取消宣言或令国家沉默。每篇宣言都必须产生非零评价：只能填写-10到-1或1到10，不得为0。履约、可执行的妥协、有效调停、承担责任和可靠协作通常提高；违约、反复改条件、欺骗、拖延、滥用威胁和违反停战通常降低。单纯拒绝要求时，根据是否及时、明确、前后一致以及是否给出可继续谈判的说明判定最低幅度±1；重复没有新条件、没有新解释、没有新行动或没有谈判进展的空洞表态应当判-1，不能靠礼貌套话反复获得声誉。reason只写简短事实理由。");
		sb.AppendLine("用户消息含“同次确定本次外交事件参与国”时，round_plan.selected_kingdom_ids必须包含全部动作对象。");
		sb.AppendLine("只输出一个JSON对象，不要代码围栏：");
		sb.AppendLine("title与body必须完整表达actions中的全部动作。");
		sb.AppendLine("{\"title\":\"简短标题\",\"body\":\"完整外交措辞正文\",\"actions\":[{\"target_kingdom_id\":\"对象ID\",\"intent\":\"当前可选动作\",\"commitment\":\"non_binding|proposal|acceptance|rejection|binding\",\"negotiation_move\":\"statement时必填否则空字符串\",\"peace_terms\":{}}],\"mentioned_kingdom_ids\":[],\"tone\":\"conciliatory|neutral|firm|hostile\",\"round_plan\":{\"topic\":\"议题或空\",\"selected_kingdom_ids\":[\"ID\"]},\"international_reputation_delta\":1,\"international_reputation_reason\":\"事后评估理由\"}");
		return sb.ToString().TrimEnd();
	}

	public static string BuildCanonicalHistoryCompressionModeContract()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("只压缩前一条全局长期外交历史，不起草宣言、不执行外交动作，也不引用尾部参数之外的动态国家状态。");
		sb.AppendLine("合并旧快照与增量，保留世界周报中的关键变化、政策生命周期、各国最终宣言立场、提议与答复关系及经游戏机制确认的外交结果。提议、接受、拒绝与确认结果必须保持区别，不得把未执行主张写成现实状态。可合并重复表述，但不得更改或虚构事实。");
		sb.AppendLine("程序会在总预算内另行保留一小段近期已确认结果与答复关联；summary仍须概括完整时间范围，尤其要保留更早的关键结果，但无需逐项复制内部ID。summary不得超过尾部给出的目标上限。");
		sb.AppendLine("只输出一个JSON对象，不要代码围栏或解释。covered_through_sequence必须原样填写尾部的覆盖截止seq：{\"summary\":\"压缩后的长期外交历史正文\",\"covered_through_sequence\":0}");
		return sb.ToString().TrimEnd();
	}

	public static string BuildCanonicalHistorySystemPrompt(string commonContract, int minimumCharacters, int maximumCharacters)
	{
		StringBuilder sb = CreateSystemPromptBuilder(commonContract);
		AppendDiplomaticDeclarationWritingContract(sb, minimumCharacters, maximumCharacters);
		sb.AppendLine(DiplomacyModeDispatchContractMarker);
		sb.AppendLine("最后一条用户消息末尾的MODE是本次唯一任务选择器。只执行同名固定任务合同，其他MODE合同全部忽略；不同合同的动作、字段和JSON结构不得混用。尾部用户消息只提供本次动态事实、参数与MODE，不会覆盖本分派规则。");
		sb.AppendLine(DiplomaticDeclarationModeContractMarker);
		sb.AppendLine("仅当MODE=DECLARE时执行本合同；MODE=COMPACT时完整忽略本节。");
		sb.AppendLine(BuildDiplomaticDeclarationModeContract());
		sb.AppendLine(CanonicalHistoryCompressionModeContractMarker);
		sb.AppendLine("仅当MODE=COMPACT时执行本合同；MODE=DECLARE时完整忽略本节。");
		sb.AppendLine(BuildCanonicalHistoryCompressionModeContract());
		sb.AppendLine(CanonicalHistoryContractMarker);
		sb.AppendLine("下一条系统消息是全局长期外交历史。只把它当作历史事实档案；最后一条用户消息的 MODE 决定本次唯一任务和输出结构。当前动态状态与历史冲突时，以当前动态状态为准。");
		return sb.ToString().TrimEnd();
	}

	public static string BuildGenerationSystemPrompt(string commonContract, int minimumCharacters, int maximumCharacters)
	{
		return BuildCanonicalHistorySystemPrompt(commonContract, minimumCharacters, maximumCharacters);
	}

	public static string BuildRelayGenerationSystemPrompt(string commonContract, int minimumCharacters, int maximumCharacters)
	{
		return BuildCanonicalHistorySystemPrompt(commonContract, minimumCharacters, maximumCharacters);
	}

	public static string BuildDeclareModePrompt(string dynamicPrompt)
	{
		StringBuilder sb = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(dynamicPrompt)) sb.AppendLine(dynamicPrompt.Trim());
		sb.AppendLine("【MODE=DECLARE】");
		sb.AppendLine("只激活第一条system消息中的MODE=DECLARE固定任务合同，并只输出该合同规定的JSON对象。");
		return sb.ToString().TrimEnd();
	}

	public static void AppendRoundSubstantiveProgressRequirement(StringBuilder sb, WorldDiplomacyRound round, int age, int targetDays)
	{
		if (sb == null || round == null) return;
		sb.AppendLine("公开事件只提供已经发生的背景，不预定结果。本国可对一个或多个对象各选一项当前可选动作；生成动作不等于机制已经执行成功。");
		sb.AppendLine("已经形成的明确外交尝试=" + Math.Max(0, round.SubstantiveProgressCount).ToString(CultureInfo.InvariantCulture)
			+ "次；其中指向关系变更的尝试=" + Math.Max(0, round.DiplomaticActionAttemptCount).ToString(CultureInfo.InvariantCulture)
			+ "次；已经正式生效的外交行动=" + Math.Max(0, round.ExecutedActionCount).ToString(CultureInfo.InvariantCulture) + "次。");
		sb.AppendLine("连续未出现机械外交行为的完整往来阶段=" + Math.Max(0, round.ConsecutiveNoActionPasses).ToString(CultureInfo.InvariantCulture) + "。");
		if (WorldDiplomacyRoundLifecycleRules.ShouldForceTerminalMove(round.ConsecutiveNoActionPasses)
			|| round.FinalActionOpportunityIssued)
		{
			sb.AppendLine("本篇处于最终解决阶段：必须选择最终提案、接受、拒绝、让步、谴责、最后通牒或其他当前可用机械外交行为；若确实无意继续，只可用statement并将negotiation_move设为end_negotiation或declare_deadlock。不得再输出普通讨论、拖延或重复立场，也不得虚构已经生效的结果。");
		}
		else if (round.ConsecutiveNoActionPasses == 1)
		{
			sb.AppendLine("上一完整往来阶段没有触发机械外交行为。本篇若继续谈判，必须提出新的条件、回答具体问题、给出部分让步、修订条款、设定期限或明确反提案；不得只换一种说法重复原立场。statement只能单独使用。");
		}
		else
		{
			sb.AppendLine("本次交涉仍允许一轮不触发机制的实质谈判。可询问、澄清、陈述理由、回应关切或提出条件，也可直接选择当前可用机械外交行为。");
		}
	}

	public static string BuildAnalysisSystemPrompt(string commonContract)
	{
		StringBuilder sb = CreateSystemPromptBuilder(commonContract);
		sb.AppendLine(DiplomacyAnalysisTaskMarker + "最后一条消息的 MODE=ANALYZE 决定本次任务和输出结构。");
		return sb.ToString().TrimEnd();
	}

	public static string BuildAnalysisModeContract()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("这份玩家宣言已经正式公开发布。只负责理解和提取语义，不得决定是否允许发布，也不得因没有游戏机制动作而退回公文；玩家文风偏好不参与语义裁判。");
		sb.AppendLine("warning表示谴责，不是劝告、关心或善意提醒；只有明确要求停止具体敌对或军事行为，并说明否则升级最后通牒或战争时才可使用。ultimatum表示战争最后通牒；已经开战用declare_war。");
		sb.AppendLine("优先提取会登记或执行机制状态的实际外交动作，不得替作者臆造动作。正文没有这类动作时仍返回status=success：普通立场用statement，一般谴责用condemn，明确正式道歉用apology，明确正式让步用concession；这些公开语义不等于宣战、提案、接受或拒绝。");
		sb.AppendLine("若材料列出当前待本国答复的正式提案，明确接受或拒绝时必须使用对应accept_*或reject_*并绑定原提出国和来源。和平原案只能原样接受或明确拒绝，不得改写条款或另提和平方案；其他提案只能使用材料列出的当前合法动作。");
		sb.AppendLine("只有正文明确、肯定且无条件地服从材料列出的未决谴责或最后通牒时，intent才可使用comply_ultimatum，commitment用binding，primary_target_kingdom_id填发出国，并把当前阶段来源公文ID填入responding_to_threat_document_id。对象国本篇就是一次性决定；含糊、沉默、附带条件、反条件、仅愿继续谈判或任何其他intent一律是不退让，该字段留空。");
		sb.AppendLine("同时生成title_summary：以发文国统治者的立场简洁概括公告核心，不使用书信标题，不超过20个汉字。");
		sb.AppendLine("addressed_kingdom_ids列出被直接点名、要求答复或承受正式主张的国家；mentioned_kingdom_ids只列被谈及但未被直接要求回应的国家。只允许使用用户消息给出的王国ID。");
		sb.AppendLine("propose_peace的peace_terms只提取正文明确条款；accept_peace由系统继承原案。领地必须来自允许清单，清单为空就留空。");
		sb.AppendLine("在完成语义提取后，对这篇已经公开的宣言做事后国际声誉评估；该评估绝不能改变或否定宣言。每篇宣言都必须产生非零评价，只能填写-10到-1或1到10，不得为0。履约、可执行妥协、有效调停、承担责任和可靠协作通常提高；违约、反复改条件、欺骗、拖延、滥用威胁和违反停战通常降低。单纯拒绝要求时，根据是否及时、明确、前后一致以及是否提供可继续谈判的说明判定最低幅度±1；重复没有新条件、新解释、新行动或谈判进展的空洞表态判-1。reason只写简短事实理由。");
		sb.AppendLine("只输出一个JSON对象，不要解释或代码围栏：");
		sb.AppendLine("{\"status\":\"success\",\"title_summary\":\"公告要点标题\",\"responding_to_offer_document_id\":\"提议来源公文ID或空字符串\",\"responding_to_threat_document_id\":\"退让对象的谴责或最后通牒来源公文ID或空字符串\",\"primary_target_kingdom_id\":\"王国ID或空字符串\",\"addressed_kingdom_ids\":[\"王国ID\"],\"mentioned_kingdom_ids\":[\"王国ID\"],\"intent\":\"statement|condemn|apology|concession|warning|ultimatum|comply_ultimatum|propose_peace|accept_peace|reject_peace|propose_alliance|accept_alliance|reject_alliance|break_alliance|propose_trade|accept_trade|reject_trade|cancel_trade|declare_war\",\"commitment\":\"non_binding|proposal|acceptance|rejection|binding\",\"requires_response\":true,\"tone\":\"conciliatory|neutral|firm|hostile\",\"confidence\":0.0,\"international_reputation_delta\":1,\"international_reputation_reason\":\"事后评估理由\",\"peace_terms\":{\"tribute_payer_kingdom_id\":\"ID或空\",\"tribute_receiver_kingdom_id\":\"ID或空\",\"daily_tribute\":0,\"duration_days\":0,\"cession_from_kingdom_id\":\"ID或空\",\"cession_to_kingdom_id\":\"ID或空\",\"cession_settlement_id\":\"ID或空\"}}");
		return sb.ToString().TrimEnd();
	}

	public static string BuildTokenCompressionPrompt(string batchId, long throughSequence, long tokenCount, int summaryTargetTokens, long protectedTokens)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【本次压缩参数】");
		sb.AppendLine("压缩批次=" + (batchId ?? "") + "；覆盖截止seq=" + Math.Max(0L, throughSequence).ToString(CultureInfo.InvariantCulture)
			+ "；当前估算tokens=" + Math.Max(0L, tokenCount).ToString(CultureInfo.InvariantCulture)
			+ "；近期硬事实预算占用tokens=" + Math.Max(0L, protectedTokens).ToString(CultureInfo.InvariantCulture)
			+ "；summary目标上限tokens=" + Math.Max(1, summaryTargetTokens).ToString(CultureInfo.InvariantCulture) + "。");
		sb.AppendLine("【MODE=COMPACT】");
		sb.AppendLine("只激活第一条system消息中的MODE=COMPACT固定任务合同，并只输出该合同规定的JSON对象。");
		return sb.ToString().TrimEnd();
	}

	public static string BuildRoundCompressionSystemPrompt()
	{
		return "你是卡拉迪亚外交编年史官。将一个已经自然结束的外交事件压缩为全局编年摘要与可按来源公文过滤的原子事实。不得编造。\n"
			+ "只输出JSON：{\"summary\":\"事件摘要\",\"facts\":[{\"text\":\"原子事实\",\"source_document_ids\":[\"公文ID\"],\"kingdom_ids\":[\"相关王国ID\"]}]}";
	}

	public static string BuildRealmInstitutionalVoiceText(string kingdomName, string cultureName, string rulerTitle, string governmentHardFact, string lore)
	{
		return "RealmInstitutionalVoice{kingdom=" + kingdomName
			+ ",culture=" + cultureName
			+ ",ruler_title_hard_fact=" + WorldDiplomacyTextRules.CompactPromptFact(rulerTitle, 80)
			+ ",government_hard_fact=" + WorldDiplomacyTextRules.CompactPromptFact(governmentHardFact, 520)
			+ ",imported_lore=" + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(WorldDiplomacyTextRules.CompactPromptFact(lore, 1100), "未命中；只可使用王国、王庭、贵族、臣民等中性称谓，不得发明具体制度")
			+ ",precedence=硬事实高于编年史检索片段；若有冲突必须舍弃检索片段，机构名称不得充当统治者个人头衔"
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
	{
		List<string> lines = (actionsByTarget ?? new Dictionary<string, List<string>>())
			.Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Value?.Count > 0)
			.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
			.Select(x => x.Key + "=" + string.Join("/", x.Value))
			.ToList();
		return lines.Count == 0
			? "当前可选动作：无；不得生成填充宣言。"
			: "当前可选动作：" + string.Join("；", lines) + "。";
	}

	public static string DescribePotentialDiplomaticActions(IEnumerable<string> intents)
	{
		List<string> labels = WorldDiplomacyRoundLifecycleRules.NormalizeOrderedIdList((intents ?? Enumerable.Empty<string>())
			.Select(WorldDiplomacyIntentVocabulary.NormalizeIntent));
		return labels.Count == 0 ? "技术状态下没有可发布的实际外交动作" : string.Join("、", labels);
	}
	internal const string KingdomStrategicProfileMarkerPrefix = "【AnimusForge 发文国国家卡：";
	internal const string KingdomStrategicIntentRule = "需要为长期战略寻找理由，尤其是战争，应依据当前局势，以现实利益、争端或安全诉求作为公开理由。";

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
public static string UpsertKingdomStrategicProfilePrompt(string existing, string profilePrompt, string authorId)
	{
		if (string.IsNullOrEmpty(existing)) return profilePrompt ?? "";
		string marker = BuildKingdomStrategicProfileMarker(authorId);
		int markerIndex = existing.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
		if (markerIndex < 0) return InsertKingdomStrategicProfilePrompt(existing, profilePrompt, authorId);
		int ruleIndex = existing.IndexOf(KingdomStrategicIntentRule, markerIndex, StringComparison.Ordinal);
		if (ruleIndex < 0) return InsertKingdomStrategicProfilePrompt(existing, profilePrompt, authorId);
		int endIndex = ruleIndex + KingdomStrategicIntentRule.Length;
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

		const string institutionalHeading = "【发文国制度、合法性与礼制声音】";
		const string familyHeading = "【权威人物与亲属关系】";
		int institutionalIndex = prompt.IndexOf(institutionalHeading, StringComparison.Ordinal);
		if (institutionalIndex >= 0)
		{
			int familyIndex = prompt.IndexOf(familyHeading, institutionalIndex + institutionalHeading.Length, StringComparison.Ordinal);
			if (familyIndex >= 0) return familyIndex;
		}

		const string actorProfileHeading = "【本发布国首次进入公文链的稳定决策档案】";
		int actorProfileIndex = prompt.IndexOf(actorProfileHeading, StringComparison.Ordinal);
		if (actorProfileIndex >= 0)
		{
			int actorFamilyIndex = prompt.IndexOf("\n王室与亲属=", actorProfileIndex + actorProfileHeading.Length, StringComparison.Ordinal);
			if (actorFamilyIndex >= 0) return actorFamilyIndex + 1;
		}

		const string relayProfileHeading = "【本发布国当前决策档案】";
		int relayProfileIndex = prompt.IndexOf(relayProfileHeading, StringComparison.Ordinal);
		if (relayProfileIndex >= 0)
		{
			int relayFamilyIndex = prompt.IndexOf("\n王室与亲属=", relayProfileIndex + relayProfileHeading.Length, StringComparison.Ordinal);
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
				int participantInstitutionIndex = prompt.IndexOf("\n国家制度与礼制声音=", participantIndex, StringComparison.Ordinal);
				if (participantInstitutionIndex >= 0 && participantInstitutionIndex < participantEnd)
				{
					int participantFamilyIndex = prompt.IndexOf("\n王室与亲属=", participantInstitutionIndex, StringComparison.Ordinal);
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
	{
		if (sb == null || round == null || string.IsNullOrWhiteSpace(authorKingdomId)) return;
		bool hasAnswerableOffer = (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
			.Any(x => WorldDiplomacyRoundLifecycleRules.IsOpenOfferToTarget(x, authorKingdomId));
		if (!hasAnswerableOffer) return;
		sb.AppendLine("本国有尚未答复的正式提议；必须按当前可选动作处理。accept_*只表示无条件接受全部原条件并立即生效。");
	}

	public static string BuildPolicySignalContext(WorldDiplomacyPolicySignal signal)
	{
		return "【已经发生的公开政策事件】\n"
			+ "事件键=" + (signal.SignalKey ?? "") + "\n"
			+ (signal.IssuerKingdomName ?? signal.IssuerKingdomId) + "已经使《" + (signal.PolicyName ?? "未命名政策") + "》生效，"
			+ "该政策直接影响" + (signal.TargetKingdomName ?? signal.TargetKingdomId) + "。\n"
			+ "政策公开摘要：" + (signal.PolicySummary ?? "") + "\n"
			+ (string.IsNullOrWhiteSpace(signal.DirectEffect) ? "" : "对该国的直接措施：" + signal.DirectEffect + "\n")
			+ "这是已经生效的政策事实，但它尚未自动形成战争、和约、同盟或其他外交结果。统治者可以辩护、评价、反对、要求修改、索取补偿、提出交换条件或借机谋利。";
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
    {
        JObject rejectedAction = rejectedActionIndex >= 0
            && rejectedJson?["actions"] is JArray rejectedActions
            && rejectedActionIndex < rejectedActions.Count
            ? rejectedActions[rejectedActionIndex] as JObject
            : null;
        string rejectedIntent = WorldDiplomacyIntentVocabulary.NormalizeIntent(WorldDiplomacyEnvelopeJsonRules.ReadString(rejectedAction ?? rejectedJson, "intent", "author_intent.intent"));
        correctionBuilder.AppendLine("【未发布草稿的硬事实纠正】");
        correctionBuilder.AppendLine("上一份assistant内容只是未发布草稿，不属于外交历史，不得引用、延续或假定其中事件已经发生。");
        correctionBuilder.AppendLine("草稿未通过JSON、字段或事实校验，请按下列说明重新起草。");
        correctionBuilder.AppendLine("当前发文国=" + authorId + "=" + authorName + "。"
            + (string.IsNullOrWhiteSpace(repairTargetId) ? "本次可从原任务授权范围选择1至4个合法对象，每个对象一项实际外交动作。"
                : "出错项对象国=" + repairTargetId + "=" + repairTargetName + "；实时关系=" + bilateralState + "；其他合法对象可保留。"));
        if (string.Equals(reason, "peace_intent_between_kingdoms_not_at_war", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine("双方当前没有战争，因此不得提出、接受或拒绝和平，不得写停战、议和、退出战争、归还战争失地或战争补偿。请改选当前可选动作。");
        }
        else if (string.Equals(reason, "non_response_claims_offer_source", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine("只有accept_*或reject_*可以绑定提议来源；其他动作不得填写来源，且必须从当前可选动作中选择。");
        }
        else if (string.Equals(reason, "comply_ultimatum_missing_source_document", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "non_compliance_claims_threat_source", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine("只有明确且无条件退让时才选择comply_ultimatum；含糊回应、附带条件或反条件都不是退让。");
        }
        else if (string.Equals(reason, "required_peace_offer_response_missing", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine(requiredPeaceOffer == null
                ? "原和平提议状态已经变化；只从当前可选动作中重新选择。"
                : "actions必须答复和平原案：来源=" + requiredPeaceOffer.SourceDocumentId
                    + "|action=" + (requiredPeaceOffer.SourceActionId ?? "")
                    + "|提出国=" + requiredPeaceOffer.ProposerKingdomId
                    + "；只能原样接受或明确拒绝，其他合法对象动作可保留。");
        }
        else if (string.Equals(reason, "multiple_peace_acceptances_have_cross_terms", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine("同一篇接受多份和平原案时不得包含割地；本篇只保留一份含割地的接受，其他原案改为明确拒绝或留待下一篇处理。");
        }
        else if (reason.StartsWith("offer_response_", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "new_proposal_claims_third_party_offer", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine("接受或拒绝只能由原提议对象国对原提出国作出；否则改选当前可选动作。");
        }
        else if (string.Equals(reason, "internal_metric_exposed_in_public_declaration", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine("正文泄露了后台态势指标。统治者不知道战争进展分、议和开放度、劣势评分、关系点、压力阈值或总战力数值。保留原本合法的外交行动与精确条款，但把后台指标改写成由战报、军情、领地得失和王庭账簿支撑的自然判断；贡金金额、条约期限和真实事件数量可以保留。");
        }
        else if (string.Equals(reason, "internal_round_term_exposed_in_public_declaration", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine("草稿泄露了系统内部的外交调度用语。公开标题和正文不得出现‘回合’、‘接力’、‘最后行动机会’、‘程序核验’等说法；应按语境改写为本次交涉、公文往来、最后立场、正式决定或外交结果。round_*字段仍按JSON契约填写，但绝不能出现在title和body中。");
        }
        else if (string.Equals(reason, "private_chat_style_in_public_declaration", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine("草稿把外交公文写成了两位君主的私人对话。保留已有事实、条件和外交意图，但改由发文国、王庭或档案明确给出的制度作为叙述主体。把‘你’改为对方国名或‘贵国’，删除‘让我说说’‘你应该谢我’‘你自己选’等互相回嘴的口语。统治者的个性只体现在国家判断、条件和威慑的分寸中。");
        }
        else if (string.Equals(reason, "realm_ruler_title_conflicts_with_hard_fact", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "southern_empire_government_conflicts_with_hard_fact", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "western_empire_government_conflicts_with_hard_fact", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine("草稿混淆了发文国的政体与统治者个人头衔。以下是必须逐字服从的王国身份硬事实：" + (realmGovernmentHardFact?.Invoke() ?? ""));
            correctionBuilder.AppendLine("机构名称只能表示国家制度或权力来源，不能替代统治者个人头衔。三大帝国的最高统治者均使用皇帝或女皇称号；不得把任何一位帝国统治者称为元老、议员、执政官、国王、大公或可汗。保留合法外交内容，重新起草整份公文。");
        }
        else if (string.Equals(reason, "json_parse_failed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "output_truncated", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "semantic_envelope_incomplete", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "generated_semantic_envelope_incomplete", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "empty_public_document", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine("只输出一个完整、可解析的JSON对象，不加代码围栏或解释。契约字段必须齐全，字符串正确转义，公开正文不能为空。");
        }
        else if (string.Equals(reason, "unsupported_intent_or_commitment", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "non_actionable_diplomatic_intent", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "intent_commitment_mismatch", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine("intent只从当前可选动作中选择；只有当前列出statement时才可使用无动作宣言。");
        }
        else if (string.Equals(reason, "statement_missing_negotiation_move", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine("statement必须填写negotiation_move，并实际完成一项结构化谈判动作，例如question、counterproposal、request_delay、final_offer、end_negotiation或declare_deadlock；不能只发表空泛立场。");
        }
        else if (string.Equals(reason, "statement_requires_terminal_negotiation_move", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine("本次交涉已经连续缺少实质外交动作；statement只能使用end_negotiation或declare_deadlock，或改选当前可用的实际外交动作，不得继续普通拖延。");
        }
        else if (string.Equals(reason, "target_kingdom_not_found", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "target_kingdom_not_eligible", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "referenced_kingdom_not_eligible", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "kingdom_not_in_autonomous_candidate_set", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "kingdom_not_in_targeted_generation_scope", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "kingdom_not_in_relay_route", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "diplomatic_action_has_no_target", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine("actions中的target_kingdom_id只能使用当前列出的合法王国ID；同一对象只能出现一次。");
        }
        else if (string.Equals(reason, "autonomous_round_plan_incomplete", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "autonomous_round_plan_has_invalid_participant", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "autonomous_round_plan_exceeds_participant_limit", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, "autonomous_round_plan_omits_direct_target", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine("自主开场必须同时填写round_plan.topic和selected_kingdom_ids。参与国只能来自候选范围，总数不得超过上限；actions中的全部对象必须列入selected_kingdom_ids。");
        }
        else if (string.Equals(reason, "accept_peace_changes_offer_terms", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine("和平原案只能原样接受或明确拒绝，不得附加、改写条款或另提和平方案；来源及原条款由系统自动绑定。");
        }
        else if ((reason ?? "").StartsWith("visible_intent_mismatch:", StringComparison.OrdinalIgnoreCase)
            || (reason ?? "").StartsWith("peace_terms_not_visible:", StringComparison.OrdinalIgnoreCase))
        {
            correctionBuilder.AppendLine("JSON意图与公开正文必须一致。正式动作要在标题或正文中明确写出；议和提案中的贡金、期限和割地必须逐项公开，不能只藏在JSON字段里。若正文没有实际动作，必须改选当前可选动作。");
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
            correctionBuilder.AppendLine("所选外交行动与当前真实关系不相容。保持国家自主判断，但改选当前状态下可以成立的对象与行动；不得把尚未生效的关系写成既成事实。");
        }
        if (!string.IsNullOrWhiteSpace(rejectedIntent)
            && !string.IsNullOrWhiteSpace(repairTargetId)
            && (string.Equals(reason, "intent_not_in_current_legal_action_list", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reason, "alliance_intent_conflicts_with_current_state", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reason, "trade_intent_conflicts_with_current_state", StringComparison.OrdinalIgnoreCase)))
        {
            correctionBuilder.AppendLine("原草稿组合" + repairTargetId + "=" + rejectedIntent + "无效，不得再次输出；只选当前可选组合。");
        }
        correctionBuilder.AppendLine("重新输出完整JSON并重写title和body；不要提到草稿、纠正、系统或上述错误。");
    }

}