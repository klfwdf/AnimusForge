using System.Collections.Generic;

namespace AnimusForge;

/// <summary>
/// Retrieval results that may be computed off the game thread (network/ONNX/cache) once the
/// request and routing are known, so the game-thread section capture only reads game state.
/// Lore candidate selection runs on the worker after the game thread prepares the versioned index;
/// runtime Lore text and entity fact formatting still read game state during final section capture;
/// direct/ruler entity scoring consumes only detached candidates on the worker.
/// </summary>
internal sealed class PromptRetrievalCapture
{
	/// <summary>Complete mention set for the build: caller-supplied + router-discovered + mention store + latest.</summary>
	internal MentionedWorldEntities AuxiliaryMentions;
	internal long LoreRuleVersion;
	internal PromptLoreSettings LoreSettings;
	internal LoreCandidateRules LoreCandidates;
	internal List<GuardrailRuleHit> FallbackExtraRuleHits;
	internal WorldEntityRetrievalService.EntityCapture EntityCapture;
	internal WorldEntityRetrievalService.DetachedEntityCandidates EntityCandidates;
	internal int EntityMaxInjectedEntities;
	internal WorldEntityRetrievalService.DetachedEntityMatches EntityMatches;
}

internal sealed class PromptLoreSettings
{
	internal bool Enabled;
	internal int SemanticTopK;
	internal float SemanticMinScore;
}

// Only this half crosses to a worker; PromptRetrievalCapture keeps live entity maps on the game thread.
internal sealed class PromptKnowledgeWorkInput
{
	internal MentionedWorldEntities Mentions;
	internal PromptRuntimeTargetBinding Target;
	internal PromptRuleEligibility Eligibility;
	internal long LoreRuleVersion;
	internal PromptLoreSettings LoreSettings;
	internal bool HasPrefetchedLore;
	internal WorldEntityRetrievalService.DetachedEntityCandidates EntityCandidates;
	internal int EntityMaxInjectedEntities;
	internal string Input;
	internal string NpcLastUtterance;
	internal bool NeedsFallbackExtraRules;
	internal int ExtraRuleReturnCap;
	internal HashSet<string> ExcludedRuleIds;
	internal string GuardrailStickyTargetKey;
}

internal sealed class PromptKnowledgeWorkResult
{
	internal LoreCandidateRules LoreCandidates;
	internal WorldEntityRetrievalService.DetachedEntityMatches EntityMatches;
	internal List<GuardrailRuleHit> FallbackExtraRuleHits;
}

/// <summary>
/// Explicit three-step contract for channel schedulers. Each channel may run the steps on
/// different threads; the shared builder runs them sequentially when no scheduler is involved.
/// </summary>
internal sealed class PromptBuildPhases
{
	internal PromptBuildRequest Request;
	internal PromptRoutingResult Routing;
	internal PromptRetrievalCapture Retrieval;
	internal MentionedWorldEntities DirectPreprocessMentions;
	internal List<string> PreprocessExcludedRuleIds = new List<string>();
	internal string PreprocessExcludedRuleBlock = "";
	internal System.Diagnostics.Stopwatch TotalStopwatch;
	internal System.Diagnostics.Stopwatch StageStopwatch;
}
