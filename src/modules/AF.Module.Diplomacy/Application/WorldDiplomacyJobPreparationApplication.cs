using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyRoundLifecycleRules;
namespace AnimusForge;
internal interface IWorldDiplomacyJobPreparationPort
{
    WorldDiplomacyStorage Storage { get; }
    int GenerationMaxTokens { get; }
    int AnalysisMaxTokens { get; }
    (int minimum, int maximum) CharacterRange();
    bool KingdomExists(string id);
    WorldDiplomacyRound ResolveRound(string id);
    string CommonContract(WorldDiplomacyRound round);
    WorldDiplomacyDocument ResolveDocument(string id);
    bool TryBuildProfile(string authorId, string marker, out string prompt);
    void LogProfile(WorldDiplomacyJob job, string prompt);
}
internal static class WorldDiplomacyJobPreparationApplication
{
public static bool HasStaleDiplomaticActionPresentation(
        WorldDiplomacyJob job, Func<WorldDiplomacyJob, string> buildLegalActionSignature)
    {
        if (job == null || !WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate")) return false;
        return !string.Equals(
            job.PresentedLegalActionSignature ?? "",
            buildLegalActionSignature?.Invoke(job),
            StringComparison.Ordinal);
    }

    internal static bool Rebuild(IWorldDiplomacyJobPreparationPort port, IWorldDiplomacyOrchestration orchestration,
        WorldDiplomacyJob job)
        => RebuildPendingJob(job, port.Storage, port.GenerationMaxTokens, port.AnalysisMaxTokens,
            port.CharacterRange, port.KingdomExists, orchestration.GetResultSettlementActionableTargetIds,
            port.ResolveRound, port.CommonContract, port.ResolveDocument,
            (round, source, sourceDocument) => orchestration.BuildRelayTurnGenerationPrompt(round, source.AuthorKingdomId,
                source.TargetKingdomId, sourceDocument, source.IsExternalResponseOnly),
            (source, exchange, sourceDocument, candidates) => orchestration.BuildGenerationPromptForJob(source.AuthorKingdomId,
                source.TargetKingdomId, exchange, source.IsResponse, sourceDocument, source.IsReminder, source.RoundId,
                source.AllowUntargeted, candidates, source.IsExternalResponseOnly),
            orchestration.BuildGenerationLegalActionSignature,
            source => orchestration.CaptureCanonicalHistoryForJob(source, syncSources: false),
            orchestration.BuildAnalysisPrompt, orchestration.BuildRoundPlanSystemPrompt, orchestration.BuildRoundPlanPrompt);

    internal static bool EnsureGenerationJobHasKingdomStrategicProfile(IWorldDiplomacyJobPreparationPort port, WorldDiplomacyJob job)
	{
		if (job == null || !WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate")) return true;
		string authorId = (job.AuthorKingdomId ?? "").Trim();
		if (string.IsNullOrEmpty(authorId)) return false;
		string marker = WorldDiplomacyPromptContractRules.BuildKingdomStrategicProfileMarker(authorId);
		if (!port.TryBuildProfile(authorId, marker, out string profilePrompt)) return false;
		if (string.Equals(job.StrategicProfileKingdomId, authorId, StringComparison.OrdinalIgnoreCase)
			&& WorldDiplomacyPromptContractRules.GenerationJobContainsKingdomStrategicProfile(job, authorId, marker, profilePrompt)) return true;
		job.StrategicProfileKingdomId = "";
		if (WorldDiplomacyPromptContractRules.GenerationJobContainsKingdomStrategicProfile(job, authorId, marker, profilePrompt))
		{
			job.StrategicProfileKingdomId = authorId;
			return true;
		}
		if (job.LlmMessages?.Count > 0)
		{
			for (int index = job.LlmMessages.Count - 1; index >= 0; index--)
			{
				WorldDiplomacyLlmMessage message = job.LlmMessages[index];
				if (message == null || !string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase)) continue;
				message.Content = WorldDiplomacyPromptContractRules.UpsertKingdomStrategicProfilePrompt(message.Content, profilePrompt, authorId);
				message.StrategicProfileKingdomId = authorId;
				job.UserPrompt = message.Content;
				job.StrategicProfileKingdomId = authorId;
				port.LogProfile(job, profilePrompt);
				return true;
			}
			job.LlmMessages.Add(new WorldDiplomacyLlmMessage { Role = "user", Content = profilePrompt, StrategicProfileKingdomId = authorId });
			job.UserPrompt = profilePrompt;
			job.StrategicProfileKingdomId = authorId;
			port.LogProfile(job, profilePrompt);
			return true;
		}
		job.UserPrompt = WorldDiplomacyPromptContractRules.UpsertKingdomStrategicProfilePrompt(job.UserPrompt, profilePrompt, authorId);
		job.StrategicProfileKingdomId = authorId;
		port.LogProfile(job, profilePrompt);
		return true;
	}

    internal static bool RefreshDiplomaticActionPresentationAndPrompt(IWorldDiplomacyJobPreparationPort port,
        IWorldDiplomacyOrchestration orchestration, WorldDiplomacyJob job)
	{
		if (job == null || !WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate")) return false;
		job.LlmMessages?.Clear();
		job.SemanticRepairAttempts = 0;
		job.HistoryPrefixHash = "";
		job.IsRunning = false;
		return Rebuild(port, orchestration, job);
	}

    internal static bool RefreshDiplomaticThreatPresentationAndPrompt(IWorldDiplomacyJobPreparationPort port,
        IWorldDiplomacyOrchestration orchestration, WorldDiplomacyJob job)
	{
		if (job == null || !WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate")) return false;
		job.PresentedThreatDocumentIds = WorldDiplomacyRoundLifecycleRules.SelectPresentedThreatStageDocumentIds(port.Storage?.DiplomaticThreats, job.AuthorKingdomId);
		job.PresentedThreatFollowThroughDocumentIds = WorldDiplomacyRoundLifecycleRules.SelectNoncompliedThreatStageDocumentIds(port.Storage?.DiplomaticThreats, job.AuthorKingdomId);
		job.LlmMessages?.Clear();
		job.SemanticRepairAttempts = 0;
		job.HistoryPrefixHash = "";
		job.IsRunning = false;
		return Rebuild(port, orchestration, job);
	}

    public static bool RebuildPendingJob(
        WorldDiplomacyJob job,
        WorldDiplomacyStorage storage,
        int generationMaxTokens,
        int analysisMaxTokens,
        Func<(int minimum, int maximum)> declarationCharRange,
        Func<string, bool> kingdomExists,
        Func<WorldDiplomacyRound, string, List<string>> getSettlementTargets,
        Func<string, WorldDiplomacyRound> resolveRound,
        Func<WorldDiplomacyRound, string> getCommonContract,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Func<WorldDiplomacyRound, WorldDiplomacyJob, WorldDiplomacyDocument, string> buildRelayTurnPrompt,
        Func<WorldDiplomacyJob, WorldDiplomacyExchange, WorldDiplomacyDocument, List<string>, string> buildGenerationPrompt,
        Func<WorldDiplomacyJob, string> buildLegalActionSignature,
        Action<WorldDiplomacyJob> captureCanonicalHistory,
        Func<WorldDiplomacyDocument, string> buildAnalysisPrompt,
        Func<WorldDiplomacyRound, string> buildRoundPlanSystemPrompt,
        Func<WorldDiplomacyDocument, List<string>, string> buildRoundPlanUserPrompt)
{
	if (job == null) return false;
	if (IsJobOfKind(job, "generate"))
	{
		if (kingdomExists?.Invoke(job.AuthorKingdomId) != true
			|| (kingdomExists?.Invoke(job.TargetKingdomId) != true && !job.AllowUntargeted)) return false;
		job.PresentedThreatDocumentIds = SelectPresentedThreatStageDocumentIds(storage?.DiplomaticThreats, job.AuthorKingdomId);
		job.PresentedThreatFollowThroughDocumentIds = SelectNoncompliedThreatStageDocumentIds(storage?.DiplomaticThreats, job.AuthorKingdomId);
		WorldDiplomacyRound round = resolveRound?.Invoke(FirstNonEmpty(job.RoundId, job.ExchangeId));
		if (job.IsRelayTurn && round?.ResultSettlementPending == true
			&& !string.IsNullOrWhiteSpace(job.ResultSettlementSlotId))
		{
			job.CandidateKingdomIds = getSettlementTargets?.Invoke(round, job.AuthorKingdomId) ?? new List<string>();
		}
		// Legacy persisted flag is never authoritative for rebuilt jobs. Opening
		// documents stay action-only; later relay eligibility is derived from live state.
		job.AllowAutonomousNoAction = false;
		string commonContract = getCommonContract?.Invoke(round);
		(int minimumCharacters, int maximumCharacters) = declarationCharRange?.Invoke() ?? (0, 0);
		job.SystemPrompt = job.IsRelayTurn ? WorldDiplomacyPromptContractRules.BuildRelayGenerationSystemPrompt(commonContract, minimumCharacters, maximumCharacters) : WorldDiplomacyPromptContractRules.BuildGenerationSystemPrompt(commonContract, minimumCharacters, maximumCharacters);
		List<string> candidates = job.CandidateKingdomIds ?? new List<string>();
		if (job.IsRelayTurn && round == null) return false;
		string dynamicPrompt = job.IsRelayTurn
			? buildRelayTurnPrompt?.Invoke(round, job, resolveDocument?.Invoke(job.SourceDocumentId))
			: buildGenerationPrompt?.Invoke(job, ResolveExchange(storage?.ActiveExchange, storage?.SuspendedExchanges, job.ExchangeId),
				resolveDocument?.Invoke(job.SourceDocumentId), candidates);
		job.UserPrompt = WorldDiplomacyPromptContractRules.BuildDeclareModePrompt(dynamicPrompt);
		job.CacheAffinityKey = WorldDiplomacyPromptContractRules.CanonicalHistoryCacheAffinityKey;
		job.ProfiledKingdomId = "";
		job.StrategicProfileKingdomId = job.AuthorKingdomId;
		job.MaxTokens = generationMaxTokens;
		job.PresentedLegalActionSignature = buildLegalActionSignature?.Invoke(job);
		captureCanonicalHistory?.Invoke(job);
		return !string.IsNullOrWhiteSpace(job.UserPrompt);
	}
	if (IsJobOfKind(job, "analyze"))
	{
		WorldDiplomacyDocument document = resolveDocument?.Invoke(job.DocumentId);
		if (document == null) return false;
		job.PresentedThreatDocumentIds = SelectPresentedThreatStageDocumentIds(storage?.DiplomaticThreats, document.AuthorKingdomId);
		job.PresentedThreatFollowThroughDocumentIds = SelectNoncompliedThreatStageDocumentIds(storage?.DiplomaticThreats, document.AuthorKingdomId);
		job.SystemPrompt = WorldDiplomacyPromptContractRules.BuildAnalysisSystemPrompt(getCommonContract?.Invoke(resolveRound?.Invoke(FirstNonEmpty(document.RoundId, document.ExchangeId))));
		job.UserPrompt = buildAnalysisPrompt?.Invoke(document);
		job.CacheAffinityKey = "analyze";
		job.MaxTokens = analysisMaxTokens;
		return true;
	}
	if (IsJobOfKind(job, "round_plan"))
	{
		WorldDiplomacyRound round = resolveRound?.Invoke(job.RoundId);
		WorldDiplomacyDocument root = resolveDocument?.Invoke(job.DocumentId);
		if (round == null || root == null) return false;
		job.SystemPrompt = buildRoundPlanSystemPrompt?.Invoke(round);
		job.UserPrompt = buildRoundPlanUserPrompt?.Invoke(root, job.CandidateKingdomIds ?? new List<string>());
		job.MaxTokens = analysisMaxTokens;
		return true;
	}
	return false;
}

// Owns analysis and round-plan job composition: admission gates, prompt binding,
// cache affinity, and queue admission stay behind the supplied ports.
public static void PrepareAnalysisJob(
        WorldDiplomacyDocument document,
        int priority,
        WorldDiplomacyStorage storage,
        int currentDay,
        int analysisMaxTokens,
        Func<string, string> createId,
        Func<string, WorldDiplomacyRound> resolveRound,
        Func<WorldDiplomacyRound, string> getCommonContract,
        Func<WorldDiplomacyDocument, string> buildAnalysisPrompt,
        Action<WorldDiplomacyJob> enqueueJob)
{
	if (document == null)
	{
		return;
	}
	WorldDiplomacyRound owningRound = resolveRound?.Invoke(FirstNonEmpty(document.RoundId, document.ExchangeId));
	string frozenCommonContract = getCommonContract?.Invoke(owningRound);
	WorldDiplomacyJob job = new WorldDiplomacyJob
	{
		JobId = createId?.Invoke("diplomacy_analyze"),
		Kind = "analyze",
		Priority = priority,
		CreatedDay = currentDay,
		ExchangeId = document.ExchangeId ?? "",
		DocumentId = document.DocumentId ?? "",
		AuthorKingdomId = document.AuthorKingdomId ?? "",
		TargetKingdomId = document.TargetKingdomId ?? "",
		PresentedThreatDocumentIds = SelectPresentedThreatStageDocumentIds(storage?.DiplomaticThreats, document.AuthorKingdomId),
		PresentedThreatFollowThroughDocumentIds = SelectNoncompliedThreatStageDocumentIds(storage?.DiplomaticThreats, document.AuthorKingdomId),
		IsResponse = document.IsResponse,
		SystemPrompt = WorldDiplomacyPromptContractRules.BuildAnalysisSystemPrompt(frozenCommonContract),
		UserPrompt = buildAnalysisPrompt?.Invoke(document),
		CacheAffinityKey = "analyze",
		MaxTokens = analysisMaxTokens
	};
	enqueueJob?.Invoke(job);
}

public static void PrepareRoundPlanJob(
        WorldDiplomacyRound round,
        WorldDiplomacyDocument root,
        WorldDiplomacyStorage storage,
        int currentDay,
        int analysisMaxTokens,
        Func<string, string> createId,
        Func<WorldDiplomacyRound, string, List<string>> getPlanCandidates,
        Func<WorldDiplomacyRound, string> buildSystemPrompt,
        Func<WorldDiplomacyDocument, List<string>, string> buildUserPrompt,
        Action<WorldDiplomacyJob> enqueueJob,
        Action<string> closeActiveRound)
{
	if (round == null || root == null || round.RelayPlanned
		|| !ReferenceEquals(storage?.ActiveRound, round)
		|| !IsActiveRoundState(round.State)
		|| storage.Jobs.Any(x => IsJobOfKind(x, "round_plan")
			&& IsRecordInRound(x.RoundId, round.RoundId))) return;
	List<string> candidates = getPlanCandidates?.Invoke(round, root.AuthorKingdomId) ?? new List<string>();
	if (candidates.Count == 0)
	{
		closeActiveRound?.Invoke("round_plan_no_actionable_participants");
		return;
	}
	WorldDiplomacyJob job = new WorldDiplomacyJob
	{
		JobId = createId?.Invoke("diplomacy_round_plan"),
		Kind = "round_plan",
		Priority = 85,
		CreatedDay = currentDay,
		RoundId = round.RoundId,
		DocumentId = root.DocumentId,
		AuthorKingdomId = root.AuthorKingdomId,
		CandidateKingdomIds = candidates,
		SystemPrompt = buildSystemPrompt?.Invoke(round),
		UserPrompt = buildUserPrompt?.Invoke(root, candidates),
		CacheAffinityKey = "diplomacy-round-plan:v6",
		MaxTokens = analysisMaxTokens
	};
	enqueueJob?.Invoke(job);
}
}
