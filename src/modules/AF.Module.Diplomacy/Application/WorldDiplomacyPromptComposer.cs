using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Synchronous prompt policy. Only stable IDs, canonical records and value facts cross
// this port; nothing here retains a Campaign object or dispatches background work.
internal static class WorldDiplomacyPromptComposer
{
    private static void AppendPlayerContext(StringBuilder sb, IWorldDiplomacyOrchestration orchestration,
        string author, string target, WorldDiplomacyRound round, WorldDiplomacyDocument source = null)
    {
        if (source?.IsPlayerAuthored == true || (orchestration is WorldDiplomacyOrchestration owner
            && owner.IsPlayerDiplomacyContext(author, target, round, source)))
            sb.AppendLine("player_diplomacy=true；玩家相关外交：自主AI的冷却、战争数量、内战、回合轮次、动作数量、单对象单动作与强制先答某案限制不适用。保留全部明确动作及其先后，不得因这些限制拒绝发文或改变诉求。真实目标与原案来源仍须准确。");
    }

    internal static string BilateralStateLabel(IWorldDiplomacyPromptWorld world, string authorId, string targetId)
    {
        if (world.ResolveKingdom(authorId) == null || world.ResolveKingdom(targetId) == null) return "未知";
        return WorldDiplomacyTextRules.BuildBilateralStateLabel(
            world.IsAtWar(authorId, targetId),
            world.IsAlly(authorId, targetId),
            world.HasTradeAgreement(authorId, targetId));
    }

	internal static string BuildRoundPlanSystemPrompt(IWorldDiplomacyPromptWorld world, WorldDiplomacyRound round)
	{
		StringBuilder sb = WorldDiplomacyPromptContractRules.CreateSystemPromptBuilder(world.GetCommonDiplomacyContract(round));
	sb.AppendLine(WorldDiplomacyPromptContractRules.RoundPlanTaskMarker + "最后一条消息的 MODE=ROUND_PLAN 决定本次任务和输出结构。");
		return sb.ToString().TrimEnd();
	}

	internal static string BuildRoundPlanPrompt(IWorldDiplomacyPromptWorld world,
		IWorldDiplomacyOrchestration orchestration, WorldDiplomacyDocument root, List<string> candidateIds)
	{
		StringBuilder sb = new StringBuilder();
        AppendPlayerContext(sb, orchestration, root.AuthorKingdomId, root.TargetKingdomId, world.ResolveRound(root.RoundId), root);
		string vassalageSnapshot = world.BuildWorldDiplomacyVassalageSnapshot();
		if (!string.IsNullOrWhiteSpace(vassalageSnapshot))
		{
			sb.AppendLine(vassalageSnapshot);
		}
	sb.AppendLine("【开场宣言】");
	sb.AppendLine("发起国=" + root.AuthorKingdomId + "=" + root.AuthorKingdomName);
	sb.AppendLine("标题=" + root.Title);
	sb.AppendLine("正文=" + WorldDiplomacyTextRules.Limit(root.Body, 2200));
	sb.AppendLine("明确指向=" + string.Join(",", root.AddressedKingdomIds ?? new List<string>()));
	sb.AppendLine("提及=" + string.Join(",", root.MentionedKingdomIds ?? new List<string>()));
	sb.AppendLine("本次参与国总数上限（包括发起国）=" + world.GetRoundParticipantLimit().ToString(CultureInfo.InvariantCulture));
	sb.AppendLine("候选国：");
		foreach (string id in candidateIds ?? new List<string>())
		{
			string kingdom = world.ResolveKingdom(id);
			if (kingdom == null) continue;
			WorldDiplomacyRound rootRound = world.ResolveRound(root.RoundId);
			sb.AppendLine(BuildCompactRoundPlanCandidateLine(world, world.ResolveKingdom(root.AuthorKingdomId), kingdom, rootRound,
				rootRound == null
					? orchestration.BuildPotentialDiplomaticActionIntents(world.ResolveKingdom(root.AuthorKingdomId), kingdom)
					: orchestration.BuildLegalDiplomaticActionIntents(rootRound, world.ResolveKingdom(root.AuthorKingdomId), kingdom)));
			string policy = world.BuildPolicySnapshot(id);
		if (!string.IsNullOrWhiteSpace(policy)) sb.AppendLine("  政策=" + WorldDiplomacyTextRules.Limit(policy, 500));
		}
	sb.AppendLine("【MODE=ROUND_PLAN】");
	sb.AppendLine("根据开场外交宣言和候选国现实利益，一次选定本次事件参与者；后续不会反复评估观察国。");
	sb.AppendLine("若宣言明确指向某国，该国必须参与。只选确实会介入本次交涉者，不选只会旁观评论者。参与国总数是上限，不必凑满；只可使用候选ID。");
	sb.AppendLine("事件由头不预定结果。参与国应能推动当前合法的结盟、解盟、贸易、断贸、宣战、议和，或提出、接受、拒绝、反提条件。");
	sb.AppendLine("只输出JSON：{\"topic\":\"简短外交议题\",\"selected_kingdom_ids\":[\"ID\"],\"reason\":\"简短理由\"}");
		return sb.ToString().TrimEnd();
	}

	internal static string BuildRelayConversationTurnPrompt(IWorldDiplomacyPromptWorld world,
		IWorldDiplomacyOrchestration orchestration,
		WorldDiplomacyRound round,
		string author,
		string previous,
		WorldDiplomacyDocument prioritySource = null,
		bool priorityResponseOnly = false)
	{
		orchestration.PruneInvalidOffers(round);
		StringBuilder sb = new StringBuilder();
        AppendPlayerContext(sb, orchestration, author, previous, round, prioritySource);
		List<string> legalTargetIds = round?.ResultSettlementPending == true
			? orchestration.GetResultSettlementActionableTargetIds(round, author)
			: (round?.RelayRouteKingdomIds ?? new List<string>())
				.Where(x => !string.Equals(x, author, StringComparison.OrdinalIgnoreCase)).ToList();
		Dictionary<string, List<string>> legalActionsByTarget = orchestration.BuildLegalDiplomaticDeclarationIntentMap(
			round,
			author,
			legalTargetIds,
			isRelayTurn: true,
			resultSettlementSlotId: round?.ResultSettlementCurrentSlotId,
			isExternalResponseOnly: priorityResponseOnly,
			responseSource: prioritySource);
		legalTargetIds = legalActionsByTarget.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
		List<string> exclusivePeaceResponseTargetIds = legalActionsByTarget
			.Where(x => WorldDiplomacyOfferContractRules.IsExclusivePeaceOfferResponseSet(x.Value))
			.Select(x => x.Key)
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
			.ToList();
	sb.AppendLine("【本次外交公文动态状态】");
	sb.AppendLine("长期档案中的宣言是已颁布公文，不是君主即时聊天。为当前王国另行起草一份可独立传阅的正式公文；可以进行有意义的谈判往来，但不得重述历史发言或无条件重复立场。机械外交行为才计入回合行动进展，谈判发言不会无限延长回合。");
	sb.AppendLine("议题=" + (round.RoundTopic ?? ""));
	sb.AppendLine("公文送达与发布顺序=" + string.Join(">", round.RelayRouteKingdomIds ?? new List<string>()));
	sb.AppendLine("本篇发布者=" + author + "=" + world.KingdomName(author) + "，授权统治者=" + world.RulerName(author));
		if (priorityResponseOnly && prioritySource != null)
		{
			string priorityActionFact = WorldDiplomacyDocumentFactRules.BuildSourceActionFactForTarget(prioritySource, author);
		sb.AppendLine("【本篇优先任务：回应玩家王国宣言】");
		sb.AppendLine("玩家王国的下列宣言已经送达本国王庭并直接指向本国，本篇必须正面回应它，而不是沿原定公文次序改谈其他国家：来源="
			+ prioritySource.DocumentId + "|发文国=" + prioritySource.AuthorKingdomId + "|标题=" + prioritySource.Title
			+ (string.IsNullOrWhiteSpace(priorityActionFact) ? "" : "|与本国相关动作=" + priorityActionFact));
		sb.AppendLine("必须选择当前可选动作。若玩家发出谴责或最后通牒，只有无条件退让才使用comply_ultimatum；任何其他实际动作都按不退让结算且威慑来源字段留空。");
		}
		AppendDiplomaticAuthorDecisionContext(sb, world, author, round.RoundId);
		AppendOtherKingdomRelationshipContext(sb, world, author, legalTargetIds);
	sb.AppendLine("最近送抵本国王庭的公文来源=" + (previous ?? "") + "=" + world.KingdomName(previous));
	sb.AppendLine("送件国只是最近来文来源，不是程序指定对象；本国必须从下方允许动作对象中选择。");
	sb.AppendLine("允许动作对象=" + string.Join(",", legalTargetIds));
		sb.AppendLine(WorldDiplomacyPromptContractRules.BuildCurrentLegalDiplomaticOptions(legalActionsByTarget));
		WorldDiplomacyRoundOffer requiredPeaceOffer = world.FindRequiredPeaceOfferResponse(
			round,
			author,
			round.ResultSettlementCurrentSlotId,
			priorityResponseOnly,
			prioritySource?.DocumentId,
			requireAnyOpenPeaceOffer: true);
		if (exclusivePeaceResponseTargetIds.Count > 0)
		{
		sb.AppendLine("和平原案答复：对象=" + string.Join(",", exclusivePeaceResponseTargetIds)
			+ "；只能选择accept_peace原样接受，或reject_peace明确拒绝；不得附加、修改条款或另提和平方案。");
		}
		if (orchestration.HasCessionBoundMultiplePeaceAcceptanceOptions(round, author, legalActionsByTarget))
		{
		sb.AppendLine("多份和平原案中含割地：本篇最多接受一份；其他原案可拒绝或留待下一篇。");
		}
		if (requiredPeaceOffer != null)
		{
		sb.AppendLine("本篇必须答复和平原案：来源=" + requiredPeaceOffer.SourceDocumentId
				+ "|action=" + (requiredPeaceOffer.SourceActionId ?? "")
			+ "|提出国=" + requiredPeaceOffer.ProposerKingdomId + "。");
		}
	sb.AppendLine("若当前可选动作含statement，它表示一项结构化谈判动作而非机械外交行为，必须填写negotiation_move并在正文中实际完成该动作；公文仍会沿原路线送交下一国。不得用空泛立场冒充新进展。");
	sb.AppendLine("吞并/朝贡/驻军/完全臣属使用 annexation/tributary/garrison/vassal 的 propose_/accept_/reject_ 动作。新提案附 treaty_terms={receiving_kingdom_id:接收国或宗主国ID,joining_kingdom_id:并入国或臣属国ID}，正文同样明确角色。接受原案必须绑定 responding_to_offer_document_id 和 responding_to_offer_action_id，采用全部原条款。withdraw_offer 仅能单独撤回本国尚未被接受的明确原案，不能撤销已发生效果。");
		AppendRelayResponseSourceContext(
			sb,
			world,
			round,
			author,
			prioritySource,
			requiredPeaceOffer?.SourceDocumentId);
		foreach (WorldDiplomacyRoundOffer offer in (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Where(x => WorldDiplomacyRoundLifecycleRules.IsOfferOfStatus(x, "open")))
		{
			bool canAnswer = string.Equals(offer.TargetKingdomId, author, StringComparison.OrdinalIgnoreCase);
		sb.AppendLine("待回应提议=" + offer.Intent + "|提出国=" + offer.ProposerKingdomId + "|对象国=" + offer.TargetKingdomId + "|来源=" + offer.SourceDocumentId
			+ (canAnswer ? "|答复资格=本国可以接受或拒绝" : "|答复资格=本国不是对象国，不得接受或拒绝；只能另提新案或改选其他合法动作"));
		}
		int age = Math.Max(0, world.CurrentDay() - round.StartedDay);
		int targetDays = Math.Max(1, round.SoftEndDay - round.StartedDay);
		int remainingDays = Math.Max(0, targetDays - age);
	sb.AppendLine("本次交涉已经进行=" + age.ToString(CultureInfo.InvariantCulture) + "天；预计时长=" + targetDays.ToString(CultureInfo.InvariantCulture)
		+ "天；距预计收束=" + remainingDays.ToString(CultureInfo.InvariantCulture) + "天；当前公文往来阶段=" + round.RelayPassNumber.ToString(CultureInfo.InvariantCulture));
		WorldDiplomacyPromptContractRules.AppendRoundSubstantiveProgressRequirement(sb, round, age, targetDays);
		WorldDiplomacyPromptContractRules.AppendOpenOfferAnswerRequirement(sb, round, author);
	if (age * 100 >= targetDays * 85) sb.AppendLine("当前已进入最后阶段：必须选择能够收束局面的当前可选动作。");
	else if (age * 100 >= targetDays * 70) sb.AppendLine("当前已进入回合后段：优先收束分歧并形成明确结果。");
		if (!string.IsNullOrWhiteSpace(round.ExternalOpeningContext))
		{
		sb.AppendLine("【本次外交事件已知的外部动向】");
			sb.AppendLine(WorldDiplomacyTextRules.Limit(round.ExternalOpeningContext, 1800));
		}
		string gatheringContext = world.BuildGatheringSnapshot(round.RelayRouteKingdomIds, 3);
		if (!string.IsNullOrWhiteSpace(gatheringContext))
		{
		sb.AppendLine("【近期相关宴会】");
			sb.AppendLine(WorldDiplomacyTextRules.Limit(gatheringContext, 900));
		sb.AppendLine("宴会只是当前可利用或评论的公开动向，不预设赞扬、嘲讽或敌意，也不自动产生外交结果。");
		}
		List<string> peaceProposalTargetIds = new List<string>();
		foreach (string id in legalTargetIds)
		{
			string other = world.ResolveKingdom(id);
			if (other == null || other == author) continue;
			if (!legalActionsByTarget.TryGetValue(id, out List<string> targetActions)) continue;
			bool includePeaceNegotiationTerms = targetActions.Any(x => string.Equals(
				WorldDiplomacyIntentVocabulary.NormalizeIntent(x),
				"propose_peace",
				StringComparison.OrdinalIgnoreCase));
			AppendDiplomaticTargetDecisionContext(
				sb,
				world,
				round,
				author,
				other,
				includePeaceNegotiationTerms,
				targetActions);
			if (includePeaceNegotiationTerms)
			{
				peaceProposalTargetIds.Add(id);
			}
		}
		if (peaceProposalTargetIds.Count > 0)
		{
		sb.AppendLine("当前可提出和平方案的对象="
			+ string.Join(",", peaceProposalTargetIds) + "。");
		}
		return sb.ToString().TrimEnd();
	}

	internal static string BuildAutonomousOpeningPrompt(IWorldDiplomacyPromptWorld world,
		IWorldDiplomacyOrchestration orchestration, string author, string roundId, List<string> candidateIds)
	{
		if (author == null) return "";
		StringBuilder sb = new StringBuilder();
		AppendDiplomaticAuthorDecisionContext(sb, world, author, roundId);
		WorldDiplomacyRound round = world.ResolveRound(roundId);
        AppendPlayerContext(sb, orchestration, author, null, round);
		if (!string.IsNullOrWhiteSpace(round?.ExternalOpeningContext))
		{
		sb.AppendLine("【已经发生的外部外交事件】");
			sb.AppendLine(WorldDiplomacyTextRules.Limit(round.ExternalOpeningContext, 1800));
		sb.AppendLine("这是可供本国利用或回应的真实事件，但不预定本国的对象、立场或行动。");
		}
	sb.AppendLine("【同次确定本次外交事件参与国】");
	sb.AppendLine("依据发文国国家卡和当前真实局势，自主决定一个或多个对象、动作与参与国；每个对象只能使用候选ID及其当前可选动作。");
	sb.AppendLine("在同一个JSON中填写round_plan。本次参与国总数上限（包括发起国）=" + world.GetRoundParticipantLimit().ToString(CultureInfo.InvariantCulture) + "。直接指向的国家必须列入selected_kingdom_ids；只选择确实需要进入本次连续公文的国家，不要凑满。");
	sb.AppendLine("【可选择的外交对象与即时硬事实】");
		foreach (string id in candidateIds ?? new List<string>())
		{
			string candidate = world.ResolveKingdom(id);
			if (candidate == null || candidate == author || world.IsEliminated(candidate) || !world.HasIndependentWorldDiplomacyAuthority(candidate)) continue;
			sb.AppendLine(BuildCompactRoundPlanCandidateLine(world, author, candidate, round,
				round == null
					? orchestration.BuildPotentialDiplomaticActionIntents(author, candidate)
					: orchestration.BuildLegalDiplomaticActionIntents(round, author, candidate)));
			if (world.IsAtWar(author, candidate))
			{
			sb.AppendLine("  战争判断=" + WorldDiplomacyTextRules.CompactPromptFact(BuildWarDecisionContext(world, author, candidate, true), 900));
			}
		}
		int activity = world.GetActivityLevel();
		sb.AppendLine(activity switch
		{
		0 => "外交活跃程度为低：优先选择代价较低的提案或答复，但仍必须采取至少一项实际动作。",
		2 => "外交活跃程度为高：更积极寻找推进国家目标的外交机会，但不得无理由发动战争。",

		_ => "外交活跃程度为正常：平衡推进国家目标与回应代价。"

		});
		return sb.ToString();
	}

	internal static string BuildGenerationPrompt(IWorldDiplomacyPromptWorld world,
		IWorldDiplomacyOrchestration orchestration,
		string author,
		string target,
		WorldDiplomacyExchange exchange,
		bool isResponse,
		WorldDiplomacyDocument sourceDocument,
		bool isReminder,
		string roundId,
		bool allowUntargeted,
		List<string> roundPlanCandidateIds,
		bool isExternalResponseOnly)
	{
		if (author == null) return "";
		if (target == null && !isResponse)
		{
			return BuildAutonomousOpeningPrompt(world, orchestration, author, roundId, roundPlanCandidateIds);
		}
		if (target == null) return "";
		string authorId = author;
		string targetId = target;
		string resolvedRoundId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(roundId, exchange?.ExchangeId, sourceDocument?.RoundId);
		WorldDiplomacyRound activeRound = world.ResolveRound(resolvedRoundId);
		List<string> relevantKingdomIds = new List<string> { authorId, targetId };
		if (activeRound?.RelayRouteKingdomIds != null) relevantKingdomIds.AddRange(activeRound.RelayRouteKingdomIds);
		string gatheringSnapshot = world.BuildGatheringSnapshot(relevantKingdomIds, 3);
		StringBuilder sb = new StringBuilder();
        AppendPlayerContext(sb, orchestration, author, target, activeRound, sourceDocument);
		AppendDiplomaticAuthorDecisionContext(sb, world, author, resolvedRoundId);
	sb.AppendLine("【本篇对象与合法动作】");
	sb.AppendLine("主要对象国：" + world.KingdomName(target) + "（ID=" + targetId + "），统治者：" + world.RulerName(target));
		List<string> legalActions = orchestration.BuildLegalDiplomaticDeclarationIntents(
			activeRound,
			author,
			target,
			isRelayTurn: false,
			resultSettlementSlotId: null,
			isExternalResponseOnly: isExternalResponseOnly,
			responseSource: sourceDocument);
		bool statementAllowed = legalActions.Contains("statement", StringComparer.OrdinalIgnoreCase);
		bool exclusivePeaceResponse = WorldDiplomacyOfferContractRules.IsExclusivePeaceOfferResponseSet(legalActions);
		bool canProposePeace = legalActions.Any(x => string.Equals(
			WorldDiplomacyIntentVocabulary.NormalizeIntent(x),
			"propose_peace",
			StringComparison.OrdinalIgnoreCase));
	sb.AppendLine("本篇合法动作=" + string.Join("、", legalActions) + "。必须选择其中一项。");
		if (exclusivePeaceResponse)
		{
		sb.AppendLine("和平原案只能选择accept_peace原样接受，或reject_peace明确拒绝；不得附加、修改条款或另提和平方案。");
		}
		WorldDiplomacyRoundOffer requiredPeaceOffer = world.FindRequiredPeaceOfferResponse(
			activeRound,
			author,
			resultSettlementSlotId: null,
			isExternalResponseOnly: isExternalResponseOnly,
			sourceDocumentId: sourceDocument?.DocumentId,
			requireAnyOpenPeaceOffer: false);
		if (requiredPeaceOffer != null)
		{
		sb.AppendLine("本篇必须答复和平原案：来源=" + requiredPeaceOffer.SourceDocumentId
				+ "|action=" + (requiredPeaceOffer.SourceActionId ?? "")
			+ "|提出国=" + requiredPeaceOffer.ProposerKingdomId + "。");
		}
		if (allowUntargeted)
		{
		sb.AppendLine("程序没有预先锁定对象；可从合法候选中选择一个或多个对象，各执行一项动作。");
		}
		if (!string.IsNullOrWhiteSpace(activeRound?.ExternalOpeningContext))
		{
		sb.AppendLine("【本次外交事件的外部起因】");
			sb.AppendLine(WorldDiplomacyTextRules.Limit(activeRound.ExternalOpeningContext, 1800));
		}
		if (!string.IsNullOrWhiteSpace(gatheringSnapshot))
		{
		sb.AppendLine("【近期相关宴会】");
			sb.AppendLine(WorldDiplomacyTextRules.Limit(gatheringSnapshot, 900));
		sb.AppendLine("宴会只是可供统治者利用、评价或回应的公开动向，不预设其态度，也不自动产生任何外交结果。");
		}
		AppendDiplomaticTargetDecisionContext(
			sb,
			world,
			activeRound,
			author,
			target,
			includePeaceNegotiationTerms: canProposePeace,
			legalActions: legalActions);
		AppendRulerCaptivityDecisionContext(sb, world, author, target);
		if (isResponse || isExternalResponseOnly || sourceDocument != null)
		{
			AppendOtherKingdomRelationshipContext(sb, world, author, new[] { targetId });
		}
		if (roundPlanCandidateIds != null && roundPlanCandidateIds.Count > 0)
		{
		sb.AppendLine("【同次确定本次外交事件参与国】");
		sb.AppendLine("在起草开场宣言的同时填写round_plan。本次参与国总数上限（包括发起国）=" + world.GetRoundParticipantLimit().ToString(CultureInfo.InvariantCulture) + "。宣言明确指向的王国必须优先入选；其余只选确有战争、同盟、贸易、安全或政治利益且能够采取外交行为者，不要为了热闹选满。候选简表：");
			foreach (string candidateId in roundPlanCandidateIds)
			{
				string candidate = world.ResolveKingdom(candidateId);
				if (candidate == null) continue;
				sb.AppendLine(BuildCompactRoundPlanCandidateLine(world, author, candidate, activeRound,
					activeRound == null
						? orchestration.BuildPotentialDiplomaticActionIntents(author, candidate)
						: orchestration.BuildLegalDiplomaticActionIntents(activeRound, author, candidate)));
			}
		}
		if (activeRound != null)
		{
			int age = Math.Max(0, world.CurrentDay() - activeRound.StartedDay);
		sb.AppendLine("当前外交事件已经持续" + age.ToString(CultureInfo.InvariantCulture) + "天，软时间尺度为" + Math.Max(1, activeRound.SoftEndDay - activeRound.StartedDay).ToString(CultureInfo.InvariantCulture) + "天。"
				+ (statementAllowed
				? "若当前列出statement，可以完成一项结构化谈判动作；必须填写negotiation_move并提出新内容，不能只换一种说法重复立场。"
				: "接近或超过软尺度时，必须选择能够收束交涉的当前合法动作；不得用最终立场式空话拖延。"));
		}
		if (isResponse && sourceDocument != null)
		{
		sb.AppendLine("下列公开外交宣言已经送达；必须从本篇当前合法动作中选择回应。");
			string sourceActionFact = WorldDiplomacyDocumentFactRules.BuildSourceActionFactForTarget(sourceDocument, author);
			string sourcePeaceTerms = WorldDiplomacyDocumentFactRules.BuildPeaceOfferTermsFact(sourceDocument, author);
		sb.AppendLine("来源公文ID：" + sourceDocument.DocumentId + "；提出国=" + sourceDocument.AuthorKingdomId
			+ (string.IsNullOrWhiteSpace(sourceActionFact) ? "" : "；与本国相关动作=" + sourceActionFact));
		if (!string.IsNullOrWhiteSpace(sourcePeaceTerms)) sb.AppendLine("和平原案条款=" + sourcePeaceTerms);
		if (requiredPeaceOffer != null)
		{
		sb.AppendLine("原案：来源=" + requiredPeaceOffer.SourceDocumentId
				+ "|action=" + (requiredPeaceOffer.SourceActionId ?? "")
			+ "；只能选择accept_peace原样接受，或reject_peace明确拒绝；不得附加、修改条款或另提和平方案。");
		}
		sb.AppendLine("标题=" + sourceDocument.Title);
		sb.AppendLine("正文=" + WorldDiplomacyTextRules.Limit(sourceDocument.Body, 2200));
		}
		if (isReminder)
		{
		sb.AppendLine("对象国迟迟没有回应。本篇仍必须采取一项实际动作，不得只催促、抱怨或假定对方已经接受。");
		}
		int activity = world.GetActivityLevel();
		sb.AppendLine(activity switch
		{
		0 => "外交活跃程度为低：优先克制、审慎和现实利益，但严重矛盾仍可升级。",
		2 => "外交活跃程度为高：应更积极提出可回应的主张、合作或冲突方案，但不得无理由发动战争。",
		_ => "外交活跃程度为标准：按当前利益与约束行动。"

		});
		return sb.ToString();
	}

	internal static string BuildAnalysisPrompt(IWorldDiplomacyPromptWorld world,
		IWorldDiplomacyOrchestration orchestration, WorldDiplomacyDocument document)
	{
		StringBuilder sb = new StringBuilder();
	sb.AppendLine("发文国：" + document.AuthorKingdomName + "（ID=" + document.AuthorKingdomId + "）");
	sb.AppendLine("吞并/朝贡/驻军/完全臣属分别用 annexation/tributary/garrison/vassal 的 propose_/accept_/reject_ 动作，新提案必须附 treaty_terms={receiving_kingdom_id:接收国或宗主国ID,joining_kingdom_id:并入国或臣属国ID}。接受/拒绝/撤回须准确绑定 responding_to_offer_document_id 及 responding_to_offer_action_id；撤回用 withdraw_offer，仅撤回本国未接受原案。玩家正文已公开，任何歧义或动作不成立都不能拦截宣言。");
		string documentAuthor = world.ResolveKingdom(document.AuthorKingdomId);
		WorldDiplomacyRound analysisRound = world.ResolveRound(document.RoundId);
		if (document.IsPlayerAuthored) orchestration.PruneInvalidOffers(analysisRound);
		if (documentAuthor != null) AppendDiplomaticThreatAnalysisContext(sb, world, documentAuthor);
		string vassalageSnapshot = world.BuildWorldDiplomacyVassalageSnapshot();
		if (!string.IsNullOrWhiteSpace(vassalageSnapshot)) sb.AppendLine(vassalageSnapshot);
	if (document.IsPlayerAuthored && document.SubjectReleaseTokens?.Count > 0)
            sb.AppendLine("本宣言提交时的直属臣属国ID（明确释放用release_subject）：" + string.Join(",", document.SubjectReleaseTokens.Keys));
        sb.AppendLine("候选对象国：");
		foreach (string kingdom in world.KingdomIds().Where(x => x != null && !world.IsEliminated(x) && !string.Equals(x, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
		{
			sb.AppendLine("- " + kingdom + " = " + world.KingdomName(kingdom));
		}
		if (!string.IsNullOrWhiteSpace(document.TargetKingdomId))
		{
		sb.AppendLine("系统当前候选主要对象：" + document.TargetKingdomId + " = " + document.TargetKingdomName);
			string author = world.ResolveKingdom(document.AuthorKingdomId);
			string candidateTarget = world.ResolveKingdom(document.TargetKingdomId);
			if (author != null && candidateTarget != null && world.IsAtWar(author, candidateTarget))
			{
				bool canProposePeace = orchestration.BuildLegalDiplomaticActionIntents(analysisRound, author, candidateTarget)
					.Any(x => string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(x), "propose_peace", StringComparison.OrdinalIgnoreCase));
				sb.AppendLine(BuildWarDecisionContext(world, author, candidateTarget, canProposePeace));
			}
		}
		if (document.IsPlayerAuthored)
		{
			WorldDiplomacyRoundOffer requiredPlayerPeaceOffer = world.FindRequiredPeaceOfferResponse(
				analysisRound,
				documentAuthor,
				document.ResultSettlementSlotId,
				isExternalResponseOnly: false,
				sourceDocumentId: document.SourceDocumentId,
				requireAnyOpenPeaceOffer: true);
            var analysisOffers = orchestration is WorldDiplomacyOrchestration live
                ? live.PlayerAnalysisOffers(document.AuthorKingdomId)
                : (analysisRound?.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
                    .Where(x => WorldDiplomacyRoundLifecycleRules.IsOpenOfferToTarget(x, document.AuthorKingdomId));
			List<WorldDiplomacyRoundOffer> openOffers = analysisOffers
				.OrderByDescending(x => requiredPlayerPeaceOffer != null
					&& WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(x.SourceDocumentId, requiredPlayerPeaceOffer.SourceDocumentId)
					&& string.Equals(x.SourceActionId ?? "", requiredPlayerPeaceOffer.SourceActionId ?? "", StringComparison.OrdinalIgnoreCase))
				.ThenByDescending(x => x.CreatedDay)
				.Take(13)
				.ToList();
			if (openOffers.Count > 0)
			{
			sb.AppendLine("本国已知的未决原案：本国提出的可撤回，外国提出的可接受或拒绝；修改条件是新提案。");
				foreach (WorldDiplomacyRoundOffer offer in openOffers.Take(12))
				{
					WorldDiplomacyDocument source = world.ResolveDocument(offer.SourceDocumentId);
                    var treaty = WorldDiplomacyDocumentFactRules.ResolveDocumentAction(source, offer.SourceActionId)?.TreatyTerms ?? source?.TreatyTerms;
					bool isPeaceOffer = string.Equals(
						WorldDiplomacyIntentVocabulary.NormalizeIntent(offer.Intent),
						"propose_peace",
						StringComparison.OrdinalIgnoreCase);
			sb.AppendLine("- 来源=" + offer.SourceDocumentId + "|动作=" + offer.SourceActionId + "|类型=" + offer.Intent
				+ "|提出国=" + offer.ProposerKingdomId + "=" + world.KingdomName(world.ResolveKingdom(offer.ProposerKingdomId))
                + "|接收国=" + offer.TargetKingdomId
                + "|条约角色=" + treaty?.ReceivingKingdomId + "/" + treaty?.JoiningKingdomId
				+ "|标题=" + WorldDiplomacyTextRules.Limit(source?.Title, 80) + "|要点=" + WorldDiplomacyTextRules.Limit(source?.Body, 240)
						+ (isPeaceOffer
					? "|原案条款=" + WorldDiplomacyOfferContractRules.FormatPeaceTermsForPrompt(WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(source, offer.SourceActionId))
						+ "|答复=原样接受或明确拒绝"
							: ""));
				}
                if (openOffers.Count > 12) sb.AppendLine("还有未列出的原案；不能推定唯一或猜测来源ID。");
			sb.AppendLine("接受、拒绝或撤回必须绑定来源公文及动作ID；接受继承原条款，修改条件提取为新提案。这里只提取玩家语义，不用回合轮次限制裁判玩家表达。只有评论且没有实际动作时返回statement或condemn。");
			}
		}
		WorldDiplomacyDocument sourceDocument = world.ResolveDocument(document.SourceDocumentId);
		if (sourceDocument != null)
		{
			sb.AppendLine("该公文正在回应：");
			string sourceActionFact = WorldDiplomacyDocumentFactRules.BuildSourceActionFactForTarget(sourceDocument, document.AuthorKingdomId);
			string sourcePeaceTerms = WorldDiplomacyDocumentFactRules.BuildPeaceOfferTermsFact(sourceDocument, document.AuthorKingdomId);
		if (!string.IsNullOrWhiteSpace(sourceActionFact)) sb.AppendLine("与本国相关动作=" + sourceActionFact);
		if (!string.IsNullOrWhiteSpace(sourcePeaceTerms)) sb.AppendLine("和平原案条款=" + sourcePeaceTerms);
		sb.AppendLine(sourceDocument.AuthorKingdomName + "《" + sourceDocument.Title + "》：" + WorldDiplomacyTextRules.Limit(sourceDocument.Body, 1400));
		}
	sb.AppendLine("公文标题：" + document.Title);
	sb.AppendLine("公文正文：" + document.Body);
	sb.AppendLine("【MODE=ANALYZE】");
		sb.AppendLine(WorldDiplomacyPromptContractRules.BuildAnalysisModeContract());
		return sb.ToString().TrimEnd();
	}
	internal static string BuildFallbackAnalysisJson(WorldDiplomacyDocument document, string targetKingdomId)
	{
		return new JObject
		{
			["status"] = "fallback",
			["title_summary"] = WorldDiplomacyTextRules.BuildFallbackDocumentTitle(document, "statement"),
			["responding_to_offer_document_id"] = "",
			["responding_to_threat_document_id"] = "",
			["primary_target_kingdom_id"] = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document?.TargetKingdomId, targetKingdomId),
			["addressed_kingdom_ids"] = new JArray(),
			["mentioned_kingdom_ids"] = new JArray(),
			["intent"] = "statement",
			["commitment"] = "non_binding",
			["requires_response"] = false,
			["tone"] = "neutral",
			["confidence"] = 0.0,
			["international_reputation_delta"] = 0,
		["international_reputation_reason"] = "语义分析服务未完成评估，交由本地结构化规则给出非零评价。"
		}.ToString(Formatting.None);
	}

	// Prompt section policy migrated from the host: every branch, ordering, and
	// candidate filter below is owned here; the world port supplies leaf facts only.
	private static void AppendDiplomaticThreatDynamicContext(StringBuilder sb, IWorldDiplomacyPromptWorld world, string authorId, string roundId)
	{
		if (sb == null || string.IsNullOrWhiteSpace(authorId)) return;
		IReadOnlyList<WorldDiplomacyThreat> threats = world.DiplomaticThreats() ?? (IReadOnlyList<WorldDiplomacyThreat>)new List<WorldDiplomacyThreat>();
		int prestige = world.NationalPrestige(authorId);
		int reputation = world.InternationalReputation(authorId);
		sb.AppendLine("【本国国家威望、国际声誉趋势与未结威慑；内部动态事实，不得在公文中公开数值】");
		sb.AppendLine("本国当前国家威望=" + prestige.ToString(CultureInfo.InvariantCulture)
			+ "/100：" + WorldDiplomacyReputationRules.DescribeNationalPrestige(prestige) + "）");
		sb.AppendLine("国家威望衡量本国威慑与承诺是否兑现：威望低会削弱威胁可信度，并按档位动态降低正式封臣家族领袖对国王的关系；恢复威望会撤回这部分动态关系惩罚。");
		sb.AppendLine("本国当前国际声誉=" + reputation.ToString(CultureInfo.InvariantCulture)
			+ "/100（外国公开评价档位=" + WorldDiplomacyReputationRules.DescribeInternationalReputation(reputation)
			+ "）；当前自然趋势=" + WorldDiplomacyReputationRules.DescribeInternationalReputationNaturalTrend(reputation)
			+ "。精确值、档位与趋势用于规划如何维护、修复或为核心利益消耗这项战略资本；现实局势允许时可主动发表有实际内容的宣言维护声誉，但不得为了声誉而沉默、回避合法立场、机械改选动作或发布空话。");
		List<string> recentReputationReasons = WorldDiplomacyTextRules.GetRecentOwnInternationalReputationReasons(
				world.Documents(), authorId, world.CurrentDay(), world.NegativeReputationFactRetentionDays(), world.FormatCampaignDate);
		if (recentReputationReasons.Count == 0)
		{
			sb.AppendLine("本国近期没有可供复盘的已结算国际声誉事件；不得编造得失原因。");
		}
		else
		{
			foreach (string recentReason in recentReputationReasons)
			{
				sb.AppendLine("本国近期国际声誉事实=" + recentReason + "。");
			}
		}
		List<WorldDiplomacyStandingChange> recentChanges = WorldDiplomacyRoundLifecycleRules.OrderDocumentsByRecency((world.Documents() ?? (IReadOnlyList<WorldDiplomacyDocument>)new List<WorldDiplomacyDocument>())
				.Where(x => x?.DiplomaticStandingChanges != null && x.IsReadyForPublication))
			.SelectMany(x => x.DiplomaticStandingChanges.AsEnumerable().Reverse())
			.Where(x => x != null
				&& string.Equals(x.KingdomId, authorId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.Kind, "national_prestige", StringComparison.OrdinalIgnoreCase))
			.Take(4)
			.ToList();
		foreach (WorldDiplomacyStandingChange change in recentChanges)
		{
			sb.AppendLine("近期国家威望结算=" + WorldDiplomacyReputationRules.FormatSignedDelta(change.Delta) + "；原因=" + change.Reason + "。");
		}

		WorldDiplomacyThreat outbound = threats.FirstOrDefault(x => WorldDiplomacyRoundLifecycleRules.IsOpenDiplomaticThreatStatus(x?.Status)
			&& string.Equals(x.IssuerKingdomId, authorId, StringComparison.OrdinalIgnoreCase));
		if (WorldDiplomacyRoundLifecycleRules.IsThreatDecisionNoncomplied(outbound))
		{
			WorldDiplomacyDocument source = world.ResolveDocument(outbound.StageDocumentId);
			if (WorldDiplomacyRoundLifecycleRules.IsThreatAtStage(outbound, "warning"))
			{
				sb.AppendLine("强制后果提示：" + world.KingdomName(outbound.TargetKingdomId) + "（ID=" + outbound.TargetKingdomId
					+ "）已对本国谴责作出不退让决定。本篇就是本国谴责后的下一份宣言，最好对该国升级为战争最后通牒（intent=ultimatum），否则本篇发布后立即扣除20点国家威望。最后通牒必须延续同一军事争端与核心要求，不得更换事项。原谴责标题="
					+ WorldDiplomacyTextRules.Limit(source?.Title, 80) + "；原谴责要点=" + WorldDiplomacyTextRules.Limit(source?.Body, 260) + "。");
			}
			else
			{
				sb.AppendLine("强制后果提示：" + world.KingdomName(outbound.TargetKingdomId) + "（ID=" + outbound.TargetKingdomId
					+ "）已对本国最后通牒作出不退让决定。本篇就是本国通牒后的下一份宣言，最好对该国宣战（intent=declare_war），否则本篇发布后立即扣除15点国家威望，但也要考虑战争的后果。");
			}
		}
		else if (outbound != null)
		{
			sb.AppendLine("本国已有等待对象国一次性决定的"
				+ WorldDiplomacyRoundLifecycleRules.DescribeThreatStageFormal(outbound.Stage)
				+ "：对象=" + outbound.TargetKingdomId + "=" + world.KingdomName(outbound.TargetKingdomId)
				+ "，来源=" + outbound.StageDocumentId + "。对象国尚未发布决定；在其决定前不得重复或提前升级该威慑。");
		}

		foreach (WorldDiplomacyThreat incoming in WorldDiplomacyRoundLifecycleRules.SelectPendingIncomingThreats(
			threats, authorId))
		{
			WorldDiplomacyDocument source = world.ResolveDocument(incoming.StageDocumentId);
			sb.AppendLine("本国收到的未决威慑："
				+ WorldDiplomacyRoundLifecycleRules.DescribeThreatStage(
					WorldDiplomacyRoundLifecycleRules.NormalizeThreatEventStage(incoming.Stage))
				+ "：发出国=" + incoming.IssuerKingdomId + "=" + world.KingdomName(incoming.IssuerKingdomId)
				+ "；来源=" + incoming.StageDocumentId
				+ "；标题=" + WorldDiplomacyTextRules.Limit(source?.Title, 80)
				+ "；要点=" + WorldDiplomacyTextRules.Limit(source?.Body, 260)
				+ "。选择intent=comply_ultimatum即为无条件退让；任何其他intent即不退让，后续不能反悔。退让会降低本国国家威望，并使本国每个正式封臣家族与当前王族关系下降20点，最后可能导致内战发生，请根据形势、战事、国家性格与长期战略权衡利弊。");
			if (!string.IsNullOrWhiteSpace(incoming.PolicyConditionPolicyId))
			{
				sb.AppendLine("附带政策条件：若本国选择comply_ultimatum，"
					+ WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(incoming.PolicyConditionPolicyName, incoming.PolicyConditionPolicyId)
					+ "》将由机制取消。");
			}
		}

		foreach (WorldDiplomacyThreat notice in WorldDiplomacyRoundLifecycleRules.SelectIssuerResolutionNotices(
			threats, authorId, null, 4))
		{
			sb.AppendLine("已确认：" + world.KingdomName(notice.TargetKingdomId) + "已明确服从本国此前的"
				+ WorldDiplomacyRoundLifecycleRules.DescribeThreatStage(
					WorldDiplomacyRoundLifecycleRules.NormalizeThreatEventStage(notice.Stage))
				+ "，后续宣言无需为该威慑宣战或继续升级，也不会因此扣除国家威望。"
				+ (WorldDiplomacyRoundLifecycleRules.IsThreatCancellationStatusCancelled(notice.PolicyConditionCancellationStatus)
					? "附带政策《" + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(notice.PolicyConditionPolicyName, notice.PolicyConditionPolicyId) + "》已经取消。"
					: ""));
		}
	}
	private static void AppendDiplomaticThreatAnalysisContext(StringBuilder sb, IWorldDiplomacyPromptWorld world, string authorId)
	{
		if (sb == null || string.IsNullOrWhiteSpace(authorId)) return;
		List<WorldDiplomacyThreat> incoming = WorldDiplomacyRoundLifecycleRules.SelectPendingIncomingThreats(
			world.DiplomaticThreats(), authorId);
		if (incoming.Count == 0) return;
		sb.AppendLine("当前可供语义裁定绑定的未决威慑：");
		foreach (WorldDiplomacyThreat threat in incoming)
		{
			WorldDiplomacyDocument source = world.ResolveDocument(threat.StageDocumentId);
			sb.AppendLine("- 来源=" + threat.StageDocumentId + "|类型=" + threat.Stage
				+ "|发出国=" + threat.IssuerKingdomId + "=" + world.KingdomName(threat.IssuerKingdomId)
				+ "|标题=" + WorldDiplomacyTextRules.Limit(source?.Title, 80) + "|要点=" + WorldDiplomacyTextRules.Limit(source?.Body, 260));
		}
		sb.AppendLine("只有玩家正文以本国为主语，明确、完整、无条件服从其中一项威慑时才裁定comply_ultimatum并绑定该来源；这是一次性决定，部分接受、原则接受、附带要求、反条件、沉默、第三国叙述或任何其他意图都立即算不退让。");
	}
	private static void AppendDiplomaticAuthorDecisionContext(
		StringBuilder sb,
		IWorldDiplomacyPromptWorld world,
		string authorId,
		string roundId)
	{
		if (sb == null || string.IsNullOrWhiteSpace(authorId)) return;
		sb.AppendLine("【发文者稳定档案】");
		sb.AppendLine("发文国：" + world.KingdomName(authorId) + "（ID=" + authorId + "），统治者：" + world.RulerName(authorId));
		string vassalageSnapshot = world.BuildWorldDiplomacyVassalageSnapshot();
		if (!string.IsNullOrWhiteSpace(vassalageSnapshot)) sb.AppendLine(vassalageSnapshot);
		List<string> currentWars = (world.CurrentWarKingdomIds(authorId) ?? (IReadOnlyList<string>)new List<string>())
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
			.Select(x => x + "=" + world.KingdomName(x))
			.ToList();
		sb.AppendLine("本国当前交战=" + (currentWars.Count == 0 ? "[]" : "[" + string.Join(",", currentWars) + "]") + "。此项只陈述战争状态，不授予名单外外交动作。");
		AppendRulerCaptivityDecisionContext(sb, world, authorId, null);
		AppendDiplomaticThreatDynamicContext(sb, world, authorId, roundId);
		sb.AppendLine("【发文者人格与声音】");
		sb.AppendLine(world.RulerVoiceContext(authorId));
		sb.AppendLine("按这位统治者的真实取舍起草国家公文；人格体现于利益、信任、代价与行动分寸，国家立场仍以王国、王庭、贵族和臣民表达。");
		sb.AppendLine("【发文国制度、合法性与礼制声音】");
		sb.AppendLine(world.RealmInstitutionalVoiceContext(authorId));
		sb.AppendLine("当前游戏身份与政体硬事实高于检索背景；背景只能补充语气，不得改写统治者头衔、政体或发明机构。");
		sb.AppendLine("【权威人物与亲属关系】");
		sb.AppendLine(world.AuthorRulerFamilyContext(authorId));
		sb.AppendLine("只有本段列出的直接亲属关系才是事实；仅在本次外交确实涉及王朝、联姻、人质或王室安全时使用。");
		string policySnapshot = world.BuildPolicySnapshot(authorId);
		if (!string.IsNullOrWhiteSpace(policySnapshot))
		{
			sb.AppendLine("【发文国政策快照】");
			sb.AppendLine(policySnapshot);
			sb.AppendLine("政策只用于判断当前目标、利益与压力，不证明未明确提供的外交或军事结果。");
		}
	}
	private static void AppendDiplomaticTargetDecisionContext(
		StringBuilder sb,
		IWorldDiplomacyPromptWorld world,
		WorldDiplomacyRound round,
		string authorId,
		string targetId,
		bool includePeaceNegotiationTerms,
		IReadOnlyCollection<string> legalActions)
	{
		if (sb == null || string.IsNullOrWhiteSpace(authorId) || string.IsNullOrWhiteSpace(targetId)
			|| string.Equals(authorId, targetId, StringComparison.OrdinalIgnoreCase)) return;
		WorldDiplomacyRealmRelationProfile relationProfile = world.RelationProfile(authorId, targetId);
		WorldDiplomacyBorderRelation border = world.BorderRelation(authorId, targetId);
		WarSituationSnapshot situation = world.WarSituation(authorId, targetId);
		string bilateralFamily = world.BilateralRulerFamilyContext(authorId, targetId);
		string recentBattles = world.RecentBilateralBattleContext(authorId, targetId);
		string nativeReasons = world.RecentNativeSignalContext(authorId, targetId);
		string targetPolicy = world.BuildPolicySnapshot(targetId);
		int relation = world.RulerRelation(authorId, targetId);
		int culturalFiefs = world.CulturalClaimCount(authorId, targetId);
		int pressure = world.WarPressure(authorId, targetId);
		bool peaceTermsVisible = includePeaceNegotiationTerms
			&& !WorldDiplomacyRoundLifecycleRules.IsImmediateWarResponsePeaceSuppressed(round, round?.ResultSettlementCurrentSlotId,
				authorId, targetId, world.ResolveDocument);

		sb.AppendLine("【对象决策硬事实】" + targetId + "】");
		sb.AppendLine("对象国=" + world.KingdomName(targetId) + "（ID=" + targetId + "），统治者=" + world.RulerName(targetId));
		int targetPrestige = world.NationalPrestige(targetId);
		int targetReputation = world.InternationalReputation(targetId);
		sb.AppendLine("对象国国家威望=" + targetPrestige.ToString(CultureInfo.InvariantCulture)
			+ "/100：" + WorldDiplomacyReputationRules.DescribeNationalPrestige(targetPrestige) + "）；外国对该国的公开国际声誉档位="
			+ WorldDiplomacyReputationRules.DescribeInternationalReputation(targetReputation)
			+ "。国家威望低意味着其威胁较不可信，但也可能迫使其为避免进一步失威而采取更冒险的兑现行动；国际声誉只用于判断其承诺可信度、合作条件与外交风险，不代表友好、和平倾向或不可宣战。");
		string reputationConflictOpportunity = WorldDiplomacyTextRules.BuildLowReputationConflictOpportunityContext(
				targetReputation, legalActions, world.Documents(), targetId,
				world.CurrentDay(), world.NegativeReputationFactRetentionDays(), world.FormatCampaignDate);
		if (!string.IsNullOrWhiteSpace(reputationConflictOpportunity))
		{
			sb.AppendLine(reputationConflictOpportunity);
		}
		if (!string.IsNullOrWhiteSpace(bilateralFamily)) sb.AppendLine(bilateralFamily);
		sb.AppendLine("当前关系=" + BilateralStateLabel(world, authorId, targetId)
			+ "；两国贵族整体关系=" + WorldDiplomacyTextRules.DescribeRealmRelationProfile(relationProfile)
			+ "；统治者私人关系=" + WorldDiplomacyTextRules.DescribeRulerRelation(relation)
			+ "；地理关系=" + (border.SharesBorder ? WorldDiplomacyTextRules.DescribeBorderRelation(border) : "不接壤")
			+ "；总体军力=" + WorldDiplomacyTextRules.DescribeStrengthBalance(situation.AuthorStrength, situation.TargetStrength) + "。");
		sb.AppendLine("对象国占有的发文国文化城镇城堡数量=" + culturalFiefs.ToString(CultureInfo.InvariantCulture)
			+ "；边境与政治压力=" + WorldDiplomacyTextRules.DescribeWarPressure(pressure) + "。这些只供王庭判断，不得写成分数或门槛。");
		if (!string.IsNullOrWhiteSpace(targetPolicy))
		{
			sb.AppendLine("对象国政策=" + WorldDiplomacyTextRules.Limit(targetPolicy, 700));
		}
		if (!string.IsNullOrWhiteSpace(nativeReasons))
		{
			sb.AppendLine("近期原版外交动机素材】");
			sb.AppendLine(WorldDiplomacyTextRules.Limit(nativeReasons, 800));
		}
		sb.AppendLine("近期双边战斗事实。");
		sb.AppendLine(WorldDiplomacyTextRules.Limit(recentBattles, 1500));
		sb.AppendLine("具体战斗只可引用上列硬事实；未列出的战役、战果、兵力、伤亡或俘虏不得补写。");
		if (situation?.IsAtWar == true)
		{
			sb.AppendLine("战争硬性状态：双方已经交战，不得再次宣战。");
			sb.AppendLine(BuildWarDecisionContext(world, authorId, targetId, peaceTermsVisible));
		}
		else
		{
			sb.AppendLine("战争硬性状态：双方当前没有战争；历史敌意、统一诉求或边境摩擦不等于已经交战。");
		}
	}
	private static void AppendRelayResponseSourceContext(
		StringBuilder sb,
		IWorldDiplomacyPromptWorld world,
		WorldDiplomacyRound round,
		string authorId,
		WorldDiplomacyDocument responseSource,
		string requiredSourceDocumentId)
	{
		if (string.IsNullOrWhiteSpace(authorId)) return;
		WorldDiplomacyRoundLifecycleRules.AppendRelayResponseSourceContext(
			sb, round, authorId, responseSource, requiredSourceDocumentId,
			world.Documents());
	}
	private static string BuildCompactRoundPlanCandidateLine(
		IWorldDiplomacyPromptWorld world,
		string authorId,
		string candidateId,
		WorldDiplomacyRound round,
		IReadOnlyList<string> legalActions)
	{
		List<string> actions = legalActions as List<string> ?? legalActions?.ToList() ?? new List<string>();
		string line = BuildCompactDiplomaticRelationshipLine(world, authorId, candidateId)
			+ "；可选动作=" + WorldDiplomacyPromptContractRules.DescribePotentialDiplomaticActions(actions);
		string captivityHint = BuildRulerCaptivityTargetHint(world, authorId, candidateId);
		if (!string.IsNullOrWhiteSpace(captivityHint)) line += "\n  " + captivityHint;
		string reputationConflictOpportunity = string.IsNullOrWhiteSpace(candidateId) ? "" : WorldDiplomacyTextRules.BuildLowReputationConflictOpportunityContext(
				world.InternationalReputation(candidateId), actions, world.Documents(), candidateId,
				world.CurrentDay(), world.NegativeReputationFactRetentionDays(), world.FormatCampaignDate);
		return string.IsNullOrWhiteSpace(reputationConflictOpportunity)
			? line
			: line + "\n  " + reputationConflictOpportunity;
	}
	private static void AppendOtherKingdomRelationshipContext(
		StringBuilder sb,
		IWorldDiplomacyPromptWorld world,
		string authorId,
		IEnumerable<string> detailedTargetIds)
	{
		if (sb == null || string.IsNullOrWhiteSpace(authorId)) return;
		HashSet<string> excludedIds = new HashSet<string>(
			(detailedTargetIds ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)),
			StringComparer.OrdinalIgnoreCase)
		{
			authorId
		};
		List<string> otherKingdomIds = (world.IndependentKingdomIds() ?? (IReadOnlyList<string>)new List<string>())
			.Where(x => !excludedIds.Contains(x))
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (otherKingdomIds.Count == 0) return;

		sb.AppendLine("【本国与其他王国的关系快照】");
		sb.AppendLine("下列信息只供全局判断，不授予额外动作；动作对象仍以本篇当前可选对象为准。");
		foreach (string otherId in otherKingdomIds)
		{
			sb.AppendLine(BuildCompactDiplomaticRelationshipLine(world, authorId, otherId));
			if (world.IsAtWar(authorId, otherId))
			{
				sb.AppendLine("  战争态势=" + WorldDiplomacyTextRules.CompactPromptFact(BuildWarDecisionContext(world, authorId, otherId, false), 650));
			}
		}
	}
	private static string BuildCompactDiplomaticRelationshipLine(IWorldDiplomacyPromptWorld world, string authorId, string candidateId)
	{
		if (string.IsNullOrWhiteSpace(authorId) || string.IsNullOrWhiteSpace(candidateId)) return "";
		string policy = WorldDiplomacyTextRules.CompactPromptFact(world.BuildPolicySnapshot(candidateId), 180);
		StringBuilder sb = new StringBuilder();
		WorldDiplomacyRealmRelationProfile relationProfile = world.RelationProfile(authorId, candidateId);
		WorldDiplomacyBorderRelation border = world.BorderRelation(authorId, candidateId);
		WarSituationSnapshot strengthSituation = world.WarSituation(authorId, candidateId);
		int candidateReputation = world.InternationalReputation(candidateId);
		sb.Append("- ").Append(candidateId).Append('=').Append(world.KingdomName(candidateId))
			.Append("；与本国=").Append(BilateralStateLabel(world, authorId, candidateId))
			.Append("；两国贵族整体关系=").Append(WorldDiplomacyTextRules.DescribeRealmRelationProfile(relationProfile))
			.Append("；统治者私人关系=").Append(WorldDiplomacyTextRules.DescribeRulerRelation(world.RulerRelation(authorId, candidateId)))
			.Append("；地理关系=").Append(border.SharesBorder ? WorldDiplomacyTextRules.DescribeBorderRelation(border) : "不接壤")
			.Append("；总体军力=").Append(WorldDiplomacyTextRules.DescribeStrengthBalance(strengthSituation.AuthorStrength, strengthSituation.TargetStrength))
			.Append("；国家威望=").Append(world.NationalPrestige(candidateId).ToString(CultureInfo.InvariantCulture))
			.Append("；外国对其公开国际声誉档位=").Append(WorldDiplomacyReputationRules.DescribeInternationalReputation(candidateReputation));
		if (!string.IsNullOrWhiteSpace(policy)) sb.Append("；政策倾向=").Append(policy);
		return sb.ToString();
	}
	private static string BuildWarDecisionContext(
		IWorldDiplomacyPromptWorld world,
		string authorId,
		string targetId,
		bool includePeaceNegotiationTerms)
	{
		WarSituationSnapshot snapshot = world.WarSituation(authorId, targetId);
		if (snapshot?.IsAtWar != true) return "";
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【仅供统治者判断的战争态势】战争已" + WorldDiplomacyTextRules.DescribeWarDuration(snapshot.WarDays, world.DaysPerYear()) + "。");
		sb.AppendLine("双方总体军力=" + WorldDiplomacyTextRules.DescribeStrengthBalance(snapshot.AuthorStrength, snapshot.TargetStrength)
			+ "；近期战局=" + WorldDiplomacyTextRules.DescribeWarProgress(snapshot.AuthorProgress, snapshot.TargetProgress)
			+ "；发文国=" + WorldDiplomacyTextRules.DescribeOtherWarBurden(snapshot.AuthorOtherWars)
			+ "；对象国=" + WorldDiplomacyTextRules.DescribeOtherWarBurden(snapshot.TargetOtherWars) + "。这些是综合判断，只能转写成世界内措辞，不得公开任何评分、分差、开放度或战力数值。");
		if (includePeaceNegotiationTerms)
		{
			IReadOnlyList<string> targetCanCede = world.CessionCandidates(targetId, authorId, snapshot.TargetCessionScore);
			IReadOnlyList<string> authorCanCede = world.CessionCandidates(authorId, targetId, snapshot.AuthorCessionScore);
			sb.AppendLine("【仅在本篇可选和平动作时使用的议和条件】发文国所受议和压力=" + WorldDiplomacyTextRules.DescribePeacePressure(snapshot.AuthorPeacePressure)
				+ "；对象国所受议和压力=" + WorldDiplomacyTextRules.DescribePeacePressure(snapshot.TargetPeacePressure) + "。");
		sb.AppendLine("贡金可与割地并存。参考每日贡金：若发文国付款约" + snapshot.AuthorSuggestedTribute + "，若对象国付款约" + snapshot.TargetSuggestedTribute + "；可以谈判但不得超出任务给出的合法上限。");
		sb.AppendLine("对象国当前可合法提出割让给发文国的领地=" + WorldDiplomacyTextRules.FormatCessionCandidates(targetCanCede) + "；发文国当前可合法提出割让给对象国的领地=" + WorldDiplomacyTextRules.FormatCessionCandidates(authorCanCede) + "。清单为空时不得提出或同意割地，也不得编造城名；优先考虑战争中尚未收复的失地。城镇只有在战局严重不利时才会进入清单。");
		}
		return sb.ToString().TrimEnd();
	}
	private static string BuildRulerCaptivityTargetHint(IWorldDiplomacyPromptWorld world, string authorId, string targetId)
	{
		WorldDiplomacyRulerCaptivity captivity = world.AuthorRulerCaptivity(authorId);
		if (captivity == null || !captivity.IsPrisoner) return "";
		bool holderKnown = !string.IsNullOrWhiteSpace(captivity.HolderKingdomId);
		if (holderKnown && string.Equals(captivity.HolderKingdomId, targetId, StringComparison.OrdinalIgnoreCase))
			return "君主被当前对象国关押或控制：本国应更重视停战、和平和可执行的让步，但仍不得绕过当前合法动作。";
		if (holderKnown)
			return "君主被其他王国关押或控制：本国整体处境恶化，应更重视稳定与谈判，不得把当前对象误认作关押方。";
		return "君主被俘但关押或控制方未知：本国处境恶化，应更重视稳定与谈判，不得猜测关押方。";
	}
	private static void AppendRulerCaptivityDecisionContext(StringBuilder sb, IWorldDiplomacyPromptWorld world, string authorId, string targetId)
	{
		if (sb == null || string.IsNullOrWhiteSpace(authorId)) return;
		WorldDiplomacyRulerCaptivity captivity = world.AuthorRulerCaptivity(authorId);
		if (captivity == null || !captivity.IsPrisoner) return;
		bool holderKnown = !string.IsNullOrWhiteSpace(captivity.HolderKingdomId);
		bool currentTargetIsHolder = holderKnown && !string.IsNullOrWhiteSpace(targetId)
			&& string.Equals(captivity.HolderKingdomId, targetId, StringComparison.OrdinalIgnoreCase);
		string pressure = string.IsNullOrWhiteSpace(targetId)
			? "需结合具体外交对象判断"
			: currentTargetIsHolder ? "是" : holderKnown ? "否" : "未知";
		sb.AppendLine("【本国君主当前处境】");
		sb.AppendLine("本国统治者被俘：是；当前关押/控制方="
			+ (holderKnown ? captivity.HolderKingdomName + "（ID=" + captivity.HolderKingdomId + "）" : "未知")
			+ "；针对本篇外交对象的被俘压力=" + pressure + "。");
		if (string.IsNullOrWhiteSpace(targetId))
		{
			sb.AppendLine("君主被俘会提高本国对稳定、停战与谈判的重视程度；若本篇对象正是当前关押或控制方，则进一步提高对让步和妥协的重视。不得猜测未知关押方，也不得绕过当前合法动作。");
		}
		else if (currentTargetIsHolder)
		{
			sb.AppendLine("君主被当前外交对象关押或控制。本国处于明显不利处境，应更重视停战、和平、让步和避免战争扩大；可以接受比平时更不利但仍可执行的条件。不得因此无条件接受不存在的提议、非法条款或绕过当前合法动作。");
		}
		else if (holderKnown)
		{
			sb.AppendLine("本国君主被其他王国关押或控制。本国整体处境恶化，应减少无意义的外交升级，更重视稳定与谈判；不得因此自动接受当前对象的条件，也不得把当前对象自动认定为关押方。");
		}
		else
		{
			sb.AppendLine("本国君主被俘但当前关押/控制方无法可靠确认。本国处境恶化，应更重视稳定与谈判；不得猜测关押方，也不得把当前对象自动认定为关押方。");
		}
	}
}
