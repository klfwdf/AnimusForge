using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Synchronous draft rejection, bounded semantic correction, source binding and enqueue.
// No rejected draft is published or recorded as an executed fact by this owner.
internal static class WorldDiplomacyDraftRepairApplication
{
	internal static void RejectGeneratedDraftBeforePublication(IWorldDiplomacyDraftRepairWorld world,
		IWorldDiplomacyOrchestration orchestration,
		WorldDiplomacyJob job,
		string rejectedRaw,
		string author,
		string target,
		string reason,
		JObject parsedJson)
	{
		if (job == null) return;
		if (author == null)
		{
			orchestration.AbandonRejectedGeneration(job, null, target, string.IsNullOrWhiteSpace(reason) ? "generated_party_missing" : reason);
			return;
		}
		string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "generated_draft_invalid" : reason.Trim();
		string logReason = WorldDiplomacyTextRules.StripGeneratedActionReasonPrefix(normalizedReason, out int rejectedActionIndex);
		JObject rejectedAction = rejectedActionIndex >= 0
			&& parsedJson?["actions"] is JArray rejectedActions
			&& rejectedActionIndex < rejectedActions.Count
			? rejectedActions[rejectedActionIndex] as JObject
			: null;
		world.Log("generated declaration rejected before publication job=" + job.JobId
			+ " scope=draft_validation"
			+ " action_index=" + rejectedActionIndex.ToString(CultureInfo.InvariantCulture)
			+ " intent=" + WorldDiplomacyIntentVocabulary.NormalizeIntent(WorldDiplomacyEnvelopeJsonRules.ReadString(rejectedAction ?? parsedJson, "intent", "author_intent.intent"))
			+ " author=" + author
			+ " target=" + (target ?? "")
			+ " reason=" + logReason
			+ " repair_attempt=" + Math.Max(0, job.SemanticRepairAttempts).ToString(CultureInfo.InvariantCulture));
		if (job.SemanticRepairAttempts < WorldDiplomacyPromptContractRules.MaxGeneratedDraftRepairAttempts
			&& EnqueueGeneratedDeclarationRepair(world, orchestration, job, rejectedRaw, author, target, normalizedReason, parsedJson))
		{
			return;
		}
		orchestration.AbandonRejectedGeneration(job, author, target, normalizedReason);
	}

	internal static bool EnqueueGeneratedDeclarationRepair(IWorldDiplomacyDraftRepairWorld world,
		IWorldDiplomacyOrchestration orchestration,
		WorldDiplomacyJob source,
		string rejectedRaw,
		string author,
		string target,
		string reason,
		JObject rejectedJson)
	{
		if (source == null || author == null) return false;
		reason = WorldDiplomacyTextRules.StripGeneratedActionReasonPrefix(reason, out int rejectedActionIndex);
		WorldDiplomacyRound repairRound = world.ResolveRound(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(source.RoundId, source.ExchangeId));
		List<string> authorizedTargetIds = orchestration.GetAuthorizedGenerationTargetIds(source, repairRound, author);
		if (authorizedTargetIds.Count == 0)
		{
			world.Log("generated declaration repair skipped because no legal action remains sourceJob=" + source.JobId
				+ " author=" + author + " reason=" + (reason ?? ""));
			return false;
		}
		string repairTarget = target != null && authorizedTargetIds.Contains(target, StringComparer.OrdinalIgnoreCase)
			? target
			: authorizedTargetIds.Count == 1 ? world.ResolveKingdom(authorizedTargetIds[0]) : null;
		StringBuilder correctionBuilder = new StringBuilder();
        correctionBuilder.AppendLine(new JObject { ["validation_error"] = reason,
            ["action_index"] = rejectedActionIndex }.ToString());
        if (reason == "intent_commitment_mismatch")
        {
            var action = rejectedActionIndex >= 0 && rejectedJson?["actions"] is JArray actions && rejectedActionIndex < actions.Count
                ? actions[rejectedActionIndex] as JObject : rejectedJson;
            string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(WorldDiplomacyEnvelopeJsonRules.ReadString(action, "intent", "author_intent.intent"));
            correctionBuilder.AppendLine(new JObject { ["validation_error"] = reason, ["action_index"] = rejectedActionIndex,
                ["intent"] = intent, ["required_commitment"] = WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent(intent) }.ToString());
        }
		WorldDiplomacyPromptContractRules.AppendGeneratedRepairCorrection(
			correctionBuilder,
			reason,
			rejectedActionIndex,
			rejectedJson,
			author,
			world.KingdomName(author),
			repairTarget,
			repairTarget == null ? "" : world.KingdomName(repairTarget),
			repairTarget == null ? "" : WorldDiplomacyPromptComposer.BilateralStateLabel(world, author, repairTarget),
			() => world.BuildGovernmentHardFact(author), world.LocalizePrompt);
		correctionBuilder.AppendLine(orchestration.BuildCurrentLegalDiplomaticOptions(
			repairRound,
			author,
			authorizedTargetIds,
			source.IsRelayTurn,
			source.ResultSettlementSlotId,
			source.IsExternalResponseOnly,
			world.ResolveDocument(source.SourceDocumentId)));
		string correction = WorldDiplomacyPromptContractRules.BuildDeclareModePrompt(correctionBuilder.ToString(), world.LocalizePrompt);
		List<WorldDiplomacyLlmMessage> messages = WorldDiplomacyPromptContractRules.CloneLlmMessages(WorldDiplomacyLlmMessageApplication.BuildLlmMessagesForJob(source, orchestration.BuildCanonicalHistoryBlock));
		messages.Add(new WorldDiplomacyLlmMessage { Role = "assistant", Content = rejectedRaw ?? "" });
		messages.Add(new WorldDiplomacyLlmMessage { Role = "user", Content = correction });
		WorldDiplomacyJob repair = WorldDiplomacyRoundLifecycleRules.BuildGeneratedDeclarationRepairJob(
			source,
			repairRound,
			correction,
			messages,
			authorizedTargetIds,
			world.NewId("diplomacy_generate_repair"));
		repair.PresentedLegalActionSignature = orchestration.BuildGenerationLegalActionSignature(repair);
        repair.PrivacySourceMaterial = source.PrivacySourceMaterial;
        repair.PrivacyPublicContext = source.PrivacyPublicContext;
        repair.PrivacyRevision = source.PrivacyRevision;
        repair.PrivacyKnowledgeRevision = source.PrivacyKnowledgeRevision;
        repair.CourtKnowledgeRevision = source.CourtKnowledgeRevision;
        repair.PrivacyRewrites = source.PrivacyRewrites;
        repair.PrivacyDeclarationSystem = source.PrivacyDeclarationSystem;
        repair.SilenceSourceDocumentId = source.SilenceSourceDocumentId;
        repair.RoundConversationRevision = source.RoundConversationRevision;
        repair.PlayerResponseSourceIds = new List<string>(source.PlayerResponseSourceIds ?? new());
        repair.PersonalMemoryRulerId = source.PersonalMemoryRulerId;
        repair.TechnicalRecoveryAttempts = source.TechnicalRecoveryAttempts;
        repair.RecoveryConfiguration = source.RecoveryConfiguration;
        repair.RecoveryServiceEpoch = source.RecoveryServiceEpoch;
        repair.RecoverOnService = source.RecoverOnService;
		orchestration.EnqueueJob(repair);
		world.Log("generated declaration repair queued sourceJob=" + source.JobId + " repairJob=" + repair.JobId + " reason=" + reason);
		return true;
	}
}
