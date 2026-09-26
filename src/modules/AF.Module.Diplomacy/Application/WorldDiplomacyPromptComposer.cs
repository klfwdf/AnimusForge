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
	internal static string BuildRoundPlanSystemPrompt(IWorldDiplomacyPromptWorld world, WorldDiplomacyRound round)
	{
		StringBuilder sb = WorldDiplomacyPromptContractRules.CreateSystemPromptBuilder(world.GetCommonDiplomacyContract(round));
		sb.AppendLine(WorldDiplomacyPromptContractRules.RoundPlanTaskMarker + "最后一条消息的 MODE=ROUND_PLAN 决定本次任务和输出结构。");
		return sb.ToString().TrimEnd();
	}

	internal static string BuildRoundPlanPrompt(IWorldDiplomacyPromptWorld world, WorldDiplomacyDocument root, List<string> candidateIds)
	{
		StringBuilder sb = new StringBuilder();
		string vassalageSnapshot = world.BuildWorldDiplomacyVassalageSnapshot();
		if (!string.IsNullOrWhiteSpace(vassalageSnapshot))
		{
			sb.AppendLine(vassalageSnapshot);
		}
		sb.AppendLine("开场宣言：");
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
			sb.AppendLine(world.BuildCompactRoundPlanCandidateLine(world.ResolveKingdom(root.AuthorKingdomId), kingdom, world.ResolveRound(root.RoundId)));
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
		WorldDiplomacyRound round,
		string author,
		string previous,
		WorldDiplomacyDocument prioritySource = null,
		bool priorityResponseOnly = false)
	{
		world.PruneInvalidOffers(round);
		StringBuilder sb = new StringBuilder();
		List<string> legalTargetIds = round?.ResultSettlementPending == true
			? world.GetResultSettlementActionableTargets(round, author)
			: (round?.RelayRouteKingdomIds ?? new List<string>())
				.Where(x => !string.Equals(x, author, StringComparison.OrdinalIgnoreCase)).ToList();
		Dictionary<string, List<string>> legalActionsByTarget = world.BuildLegalDiplomaticDeclarationIntentMap(
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
		sb.AppendLine("本篇发布国=" + author + "=" + world.KingdomName(author) + "，授权统治者=" + world.RulerName(author));
		if (priorityResponseOnly && prioritySource != null)
		{
			string priorityActionFact = WorldDiplomacyDocumentFactRules.BuildSourceActionFactForTarget(prioritySource, author);
			sb.AppendLine("【本篇优先任务：回应玩家王国宣言】");
			sb.AppendLine("玩家王国的下列宣言已经送达本国王庭并直接指向本国，本篇必须正面回应它，而不是沿原定公文次序改谈其他国家：来源="
				+ prioritySource.DocumentId + "|发文国=" + prioritySource.AuthorKingdomId + "|标题=" + prioritySource.Title
				+ (string.IsNullOrWhiteSpace(priorityActionFact) ? "" : "|与本国相关动作=" + priorityActionFact));
			sb.AppendLine("必须选择当前可选动作。若玩家发出谴责或最后通牒，只有无条件退让才使用comply_ultimatum；任何其他实际动作都按不退让结算且威慑来源字段留空。");
		}
		world.AppendDiplomaticAuthorDecisionContext(sb, author, round.RoundId);
		world.AppendOtherKingdomRelationshipContext(sb, author, legalTargetIds);
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
		if (world.HasCessionBoundMultiplePeaceAcceptanceOptions(round, author, legalActionsByTarget))
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
		world.AppendRelayResponseSourceContext(
			sb,
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
			+ "天；距离预计收束=" + remainingDays.ToString(CultureInfo.InvariantCulture) + "天；当前公文往来阶段=" + round.RelayPassNumber.ToString(CultureInfo.InvariantCulture));
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
			world.AppendDiplomaticTargetDecisionContext(
				sb,
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

	internal static string BuildAutonomousOpeningPrompt(IWorldDiplomacyPromptWorld world, string author, string roundId, List<string> candidateIds)
	{
		if (author == null) return "";
		StringBuilder sb = new StringBuilder();
		world.AppendDiplomaticAuthorDecisionContext(sb, author, roundId);
		WorldDiplomacyRound round = world.ResolveRound(roundId);
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
			sb.AppendLine(world.BuildCompactRoundPlanCandidateLine(author, candidate, round));
			if (world.IsAtWar(author, candidate))
			{
				sb.AppendLine("  战争判断=" + WorldDiplomacyTextRules.CompactPromptFact(world.BuildWarDecisionContext(author, candidate, true), 900));
			}
		}
		int activity = world.GetActivityLevel();
		sb.AppendLine(activity switch
		{
			0 => "外交活跃程度为低：优先选择代价较低的提案或答复，但仍必须采取至少一项实际动作。",
			2 => "外交活跃程度为高：更积极寻找推进国家目标的外交机会，但不得无理由发动战争。",
			_ => "外交活跃程度为标准：根据国家目标和局势，自主选择至少一项合作、施压、冲突或关系变更动作。"
		});
		return sb.ToString();
	}

	internal static string BuildGenerationPrompt(IWorldDiplomacyPromptWorld world,
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
			return BuildAutonomousOpeningPrompt(world, author, roundId, roundPlanCandidateIds);
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
		world.AppendDiplomaticAuthorDecisionContext(sb, author, resolvedRoundId);
		sb.AppendLine("【本篇对象与合法动作】");
		sb.AppendLine("主要对象国：" + world.KingdomName(target) + "（ID=" + targetId + "），统治者：" + world.RulerName(target));
		List<string> legalActions = world.BuildLegalDiplomaticDeclarationIntents(
			activeRound,
			author,
			target,
			isRelayTurn: false,
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
		world.AppendDiplomaticTargetDecisionContext(
			sb,
			activeRound,
			author,
			target,
			includePeaceNegotiationTerms: canProposePeace,
			legalActions: legalActions);
		world.AppendRulerCaptivityDecisionContext(sb, author, target);
		if (isResponse || isExternalResponseOnly || sourceDocument != null)
		{
			world.AppendOtherKingdomRelationshipContext(sb, author, new[] { targetId });
		}
		if (roundPlanCandidateIds != null && roundPlanCandidateIds.Count > 0)
		{
			sb.AppendLine("【同次确定本次外交事件参与国】");
			sb.AppendLine("在起草开场宣言的同时填写round_plan。本次参与国总数上限（包括发起国）=" + world.GetRoundParticipantLimit().ToString(CultureInfo.InvariantCulture) + "。宣言明确指向的王国必须优先入选；其余只选确有战争、同盟、贸易、安全或政治利益且能够采取外交行为者，不要为了热闹选满。候选简表：");
			foreach (string candidateId in roundPlanCandidateIds)
			{
				string candidate = world.ResolveKingdom(candidateId);
				if (candidate == null) continue;
				sb.AppendLine(world.BuildCompactRoundPlanCandidateLine(author, candidate, activeRound));
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
			sb.AppendLine("下列公开外交宣言已经送达；必须从本篇当前合法动作中选择回应：");
			string sourceActionFact = WorldDiplomacyDocumentFactRules.BuildSourceActionFactForTarget(sourceDocument, author);
			string sourcePeaceTerms = WorldDiplomacyDocumentFactRules.BuildPeaceOfferTermsFact(sourceDocument, author);
			sb.AppendLine("来源公文ID：" + sourceDocument.DocumentId + "；提出国=" + sourceDocument.AuthorKingdomId
				+ (string.IsNullOrWhiteSpace(sourceActionFact) ? "" : "；与本国相关动作=" + sourceActionFact));
			if (!string.IsNullOrWhiteSpace(sourcePeaceTerms)) sb.AppendLine("和平原案条款：" + sourcePeaceTerms);
			sb.AppendLine("标题：" + sourceDocument.Title);
			sb.AppendLine("正文：" + WorldDiplomacyTextRules.Limit(sourceDocument.Body, 2200));
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
			_ => "外交活跃程度为标准：在合作、冲突和关系变更动作之间按局势自然选择。"
		});
		return sb.ToString();
	}

	internal static string BuildAnalysisPrompt(IWorldDiplomacyPromptWorld world, WorldDiplomacyDocument document)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("发文国：" + document.AuthorKingdomName + "（ID=" + document.AuthorKingdomId + "）");
		string documentAuthor = world.ResolveKingdom(document.AuthorKingdomId);
		WorldDiplomacyRound analysisRound = world.ResolveRound(document.RoundId);
		if (document.IsPlayerAuthored) world.PruneInvalidOffers(analysisRound);
		if (documentAuthor != null) world.AppendDiplomaticThreatAnalysisContext(sb, documentAuthor);
		string vassalageSnapshot = world.BuildWorldDiplomacyVassalageSnapshot();
		if (!string.IsNullOrWhiteSpace(vassalageSnapshot)) sb.AppendLine(vassalageSnapshot);
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
				bool canProposePeace = world.BuildLegalDiplomaticActionIntents(analysisRound, author, candidateTarget)
					.Any(x => string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(x), "propose_peace", StringComparison.OrdinalIgnoreCase));
				sb.AppendLine(world.BuildWarDecisionContext(author, candidateTarget, canProposePeace));
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
			List<WorldDiplomacyRoundOffer> openOffers = (analysisRound?.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
				.Where(x => WorldDiplomacyRoundLifecycleRules.IsOpenOfferToTarget(x, document.AuthorKingdomId))
				.OrderByDescending(x => requiredPlayerPeaceOffer != null
					&& WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(x.SourceDocumentId, requiredPlayerPeaceOffer.SourceDocumentId)
					&& string.Equals(x.SourceActionId ?? "", requiredPlayerPeaceOffer.SourceActionId ?? "", StringComparison.OrdinalIgnoreCase))
				.ThenByDescending(x => x.CreatedDay)
				.Take(4)
				.ToList();
			if (openOffers.Count > 0)
			{
				sb.AppendLine("当前待本国正式答复的提案：");
				foreach (WorldDiplomacyRoundOffer offer in openOffers)
				{
					WorldDiplomacyDocument source = world.ResolveDocument(offer.SourceDocumentId);
					bool isPeaceOffer = string.Equals(
						WorldDiplomacyIntentVocabulary.NormalizeIntent(offer.Intent),
						"propose_peace",
						StringComparison.OrdinalIgnoreCase);
					sb.AppendLine("- 来源=" + offer.SourceDocumentId + "|类型=" + offer.Intent
						+ "|提出国=" + offer.ProposerKingdomId + "=" + world.KingdomName(world.ResolveKingdom(offer.ProposerKingdomId))
						+ "|标题=" + WorldDiplomacyTextRules.Limit(source?.Title, 80) + "|要点=" + WorldDiplomacyTextRules.Limit(source?.Body, 240)
						+ (isPeaceOffer
							? "|原案条款=" + WorldDiplomacyOfferContractRules.FormatPeaceTermsForPrompt(WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(source, offer.SourceActionId))
								+ "|答复=原样接受或明确拒绝"
							: ""));
				}
				sb.AppendLine("接受或拒绝必须绑定对应来源；和平原案不得改写或另提方案，其他动作以当前合法状态为准。只有评论且没有实际动作时按其语义返回statement或condemn，公文仍然有效。");
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
		sb.AppendLine("公文正文：" + WorldDiplomacyTextRules.Limit(document.Body, 3000));
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
}
