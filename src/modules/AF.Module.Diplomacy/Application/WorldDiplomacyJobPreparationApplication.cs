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
        WorldDiplomacyJob job, Func<string, string, string> localize = null)
        => RebuildPendingJob(job, port.Storage, port.GenerationMaxTokens, port.AnalysisMaxTokens,
            port.CharacterRange, port.KingdomExists,
            port.ResolveRound, port.CommonContract, port.ResolveDocument,
            (source, exchange, sourceDocument, candidates) => orchestration.BuildGenerationPromptForJob(source.AuthorKingdomId,
                source.TargetKingdomId, exchange, source.IsResponse, sourceDocument, source.IsReminder, source.RoundId,
                source.AllowUntargeted, candidates, source.IsExternalResponseOnly),
            orchestration.BuildGenerationLegalActionSignature,
            source => orchestration.CaptureCanonicalHistoryForJob(source, syncSources: false),
            orchestration.BuildAnalysisPrompt, localize);

    internal static bool RefreshDiplomaticActionPresentationAndPrompt(IWorldDiplomacyJobPreparationPort port,
        IWorldDiplomacyOrchestration orchestration, WorldDiplomacyJob job, Func<string, string, string> localize = null)
	{
		if (job == null || !WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate")) return false;
		job.LlmMessages?.Clear();
		job.SemanticRepairAttempts = 0;
		job.HistoryPrefixHash = "";
		job.IsRunning = false;
		return Rebuild(port, orchestration, job, localize);
	}

    internal static bool RefreshDiplomaticThreatPresentationAndPrompt(IWorldDiplomacyJobPreparationPort port,
        IWorldDiplomacyOrchestration orchestration, WorldDiplomacyJob job, Func<string, string, string> localize = null)
	{
		if (job == null || !WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate")) return false;
		job.PresentedThreatDocumentIds = WorldDiplomacyRoundLifecycleRules.SelectPresentedThreatStageDocumentIds(WorldDiplomacyThreatScope.For(port.Storage, job.AuthorKingdomId, job.RoundId), job.AuthorKingdomId);
		job.PresentedThreatFollowThroughDocumentIds = WorldDiplomacyRoundLifecycleRules.SelectNoncompliedThreatStageDocumentIds(WorldDiplomacyThreatScope.For(port.Storage, job.AuthorKingdomId, job.RoundId), job.AuthorKingdomId);
		job.LlmMessages?.Clear();
		job.SemanticRepairAttempts = 0;
		job.HistoryPrefixHash = "";
		job.IsRunning = false;
		return Rebuild(port, orchestration, job, localize);
	}

    public static bool RebuildPendingJob(
        WorldDiplomacyJob job,
        WorldDiplomacyStorage storage,
        int generationMaxTokens,
        int analysisMaxTokens,
        Func<(int minimum, int maximum)> declarationCharRange,
        Func<string, bool> kingdomExists,
        Func<string, WorldDiplomacyRound> resolveRound,
        Func<WorldDiplomacyRound, string> getCommonContract,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Func<WorldDiplomacyJob, WorldDiplomacyExchange, WorldDiplomacyDocument, List<string>, string> buildGenerationPrompt,
        Func<WorldDiplomacyJob, string> buildLegalActionSignature,
        Action<WorldDiplomacyJob> captureCanonicalHistory,
        Func<WorldDiplomacyDocument, string> buildAnalysisPrompt,
        Func<string, string, string> localize = null)
{
	if (job == null) return false;
	if (IsJobOfKind(job, "generate"))
	{
		if (kingdomExists?.Invoke(job.AuthorKingdomId) != true
			|| (kingdomExists?.Invoke(job.TargetKingdomId) != true && !job.AllowUntargeted)) return false;
		job.PresentedThreatDocumentIds = SelectPresentedThreatStageDocumentIds(WorldDiplomacyThreatScope.For(storage, job.AuthorKingdomId, job.RoundId), job.AuthorKingdomId);
        job.PresentedThreatFollowThroughDocumentIds = SelectNoncompliedThreatStageDocumentIds(WorldDiplomacyThreatScope.For(storage, job.AuthorKingdomId, job.RoundId), job.AuthorKingdomId);
		WorldDiplomacyRound round = resolveRound?.Invoke(FirstNonEmpty(job.RoundId, job.ExchangeId));
        if (!DiplomacyEventRules.IsEvent(round) || !IsActiveRoundState(round.State)) return false;
        job.CandidateKingdomIds = round.Participants.Where(x => (x.State != "observer" || x.KingdomId == job.TargetKingdomId) && x.State != "unavailable"
            && x.KingdomId != job.AuthorKingdomId && kingdomExists(x.KingdomId))
            .Select(x => x.KingdomId).Distinct(StringComparer.Ordinal).ToList();
        job.AllowAutonomousNoAction = false;
		string commonContract = getCommonContract?.Invoke(round);
		(int minimumCharacters, int maximumCharacters) = declarationCharRange?.Invoke() ?? (0, 0);
		job.SystemPrompt = WorldDiplomacyPromptContractRules.BuildGenerationSystemPrompt(commonContract, minimumCharacters, maximumCharacters, localize);
		List<string> candidates = job.CandidateKingdomIds ?? new List<string>();
        string dynamicPrompt = buildGenerationPrompt?.Invoke(job,
            ResolveExchange(storage?.ActiveExchange, storage?.SuspendedExchanges, job.ExchangeId),
            resolveDocument?.Invoke(job.SourceDocumentId), candidates);
		job.UserPrompt = WorldDiplomacyPromptContractRules.BuildDeclareModePrompt(dynamicPrompt, localize);
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
		job.SystemPrompt = WorldDiplomacyPromptContractRules.BuildAnalysisSystemPrompt(getCommonContract?.Invoke(resolveRound?.Invoke(FirstNonEmpty(document.RoundId, document.ExchangeId))), localize);
		job.UserPrompt = buildAnalysisPrompt?.Invoke(document);
		job.CacheAffinityKey = "analyze";
		job.MaxTokens = analysisMaxTokens;
		return true;
	}
	return false;
}

// Owns analysis job composition: admission gates, prompt binding,
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
        Action<WorldDiplomacyJob> enqueueJob, Func<string, string, string> localize = null)
{
	if (document == null)
	{
		return;
	}
    if (storage?.Jobs?.Any(x => x != null && IsJobOfKind(x, "analyze") && x.DocumentId == document.DocumentId) == true) return;
	WorldDiplomacyRound owningRound = resolveRound?.Invoke(FirstNonEmpty(document.RoundId, document.ExchangeId));
	string frozenCommonContract = getCommonContract?.Invoke(owningRound);
	WorldDiplomacyJob job = new WorldDiplomacyJob
	{
		JobId = createId?.Invoke("diplomacy_analyze"),
		Kind = "analyze",
		Priority = priority,
		CreatedDay = currentDay,
		RoundId = owningRound?.RoundId ?? document.RoundId ?? document.ExchangeId ?? "",
		ExchangeId = document.ExchangeId ?? "",
		DocumentId = document.DocumentId ?? "",
		AuthorKingdomId = document.AuthorKingdomId ?? "",
		TargetKingdomId = document.TargetKingdomId ?? "",
		PresentedThreatDocumentIds = SelectPresentedThreatStageDocumentIds(storage?.DiplomaticThreats, document.AuthorKingdomId),
		IsResponse = document.IsResponse,
		SystemPrompt = WorldDiplomacyPromptContractRules.BuildAnalysisSystemPrompt(frozenCommonContract, localize),
		UserPrompt = buildAnalysisPrompt?.Invoke(document),
		CacheAffinityKey = "analyze",
		MaxTokens = analysisMaxTokens
	};
	enqueueJob?.Invoke(job);
}


}
