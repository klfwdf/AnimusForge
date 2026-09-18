using System.Collections.Generic;

namespace AnimusForge;

/// <summary>
/// Retrieval results that may be computed off the game thread (network/ONNX/cache) once the
/// request and routing are known, so the game-thread section capture only reads game state.
/// Null fields mean "not prefetched": the capture phase falls back to its legacy inline call.
/// </summary>
internal sealed class PromptRetrievalCapture
{
	/// <summary>Auxiliary mentions merged from the router and the mention store for this input.</summary>
	internal MentionedWorldEntities AuxiliaryMentions;
	/// <summary>Lore text already resolved for the selected source (host still logs the source label).</summary>
	internal string LoreContext;
	internal bool LoreResolved;
	/// <summary>Matched extra-rule instruction text from semantic retrieval when routing produced no authoritative id set.</summary>
	internal string MatchedExtraRuleInstructions;
	internal bool MatchedExtraRuleInstructionsResolved;
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
