using System.Collections.Generic;

namespace AnimusForge;

/// <summary>
/// Retrieval results that may be computed off the game thread (network/ONNX/cache) once the
/// request and routing are known, so the game-thread section capture only reads game state.
/// Lore and entity retrieval stay in the game-thread step because their owners read Hero state (J06).
/// </summary>
internal sealed class PromptRetrievalCapture
{
	/// <summary>Complete mention set for the build: caller-supplied + router-discovered + mention store + latest.</summary>
	internal MentionedWorldEntities AuxiliaryMentions;
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
