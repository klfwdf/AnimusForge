using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.Refactor.Contracts;

public enum InteractionChannel
{
    SceneShout,
    NativeConversation,
    Courier,
    ProactiveNpc,
    Domain
}

public enum InteractionStatus
{
    Succeeded,
    SkippedByEligibility,
    DegradedWithoutProvider,
    RetryableFailure,
    NonRetryableFailure,
    CancelledAsStale,
    RejectedByValidation,
    Executed
}

public enum InteractionStage
{
    Preprocess,
    MainReply,
    Postprocess
}

public sealed class InteractionIdentity
{
    public InteractionIdentity(string sessionId, InteractionChannel channel, string subjectId)
    {
        SessionId = ContractGuard.Required(sessionId, nameof(sessionId));
        Channel = channel;
        SubjectId = ContractGuard.Required(subjectId, nameof(subjectId));
    }

    public string SessionId { get; }
    public InteractionChannel Channel { get; }
    public string SubjectId { get; }
}

public sealed class TraceContext
{
    public TraceContext(string traceId, long runtimeGeneration, long saveGeneration, string profile, string apiLine)
    {
        TraceId = ContractGuard.Required(traceId, nameof(traceId));
        RuntimeGeneration = runtimeGeneration;
        SaveGeneration = saveGeneration;
        Profile = ContractGuard.Required(profile, nameof(profile));
        ApiLine = ContractGuard.Required(apiLine, nameof(apiLine));
    }

    public string TraceId { get; }
    public long RuntimeGeneration { get; }
    public long SaveGeneration { get; }
    public string Profile { get; }
    public string ApiLine { get; }
}

public sealed class InteractionCandidate
{
    public InteractionCandidate(string stableId, string displayName, int agentIndex, bool isAlive)
    {
        StableId = ContractGuard.Required(stableId, nameof(stableId));
        DisplayName = displayName ?? string.Empty;
        AgentIndex = agentIndex;
        IsAlive = isAlive;
    }

    public string StableId { get; }
    public string DisplayName { get; }
    public int AgentIndex { get; }
    public bool IsAlive { get; }
}

public sealed class GameInteractionSnapshot
{
    public GameInteractionSnapshot(
        InteractionIdentity identity,
        TraceContext trace,
        string playerText,
        string locationId,
        int gameDay,
        int gameHour,
        IEnumerable<InteractionCandidate> candidates,
        IEnumerable<string> visibleHeroIds,
        IDictionary<string, string> detachedFacts)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Trace = trace ?? throw new ArgumentNullException(nameof(trace));
        PlayerText = playerText ?? string.Empty;
        LocationId = locationId ?? string.Empty;
        GameDay = gameDay;
        GameHour = gameHour;
        Candidates = ContractCollections.CopyList(candidates);
        VisibleHeroIds = ContractCollections.CopyStrings(visibleHeroIds);
        DetachedFacts = ContractCollections.CopyMap(detachedFacts);
    }

    public InteractionIdentity Identity { get; }
    public TraceContext Trace { get; }
    public string PlayerText { get; }
    public string LocationId { get; }
    public int GameDay { get; }
    public int GameHour { get; }
    public IReadOnlyList<InteractionCandidate> Candidates { get; }
    public IReadOnlyList<string> VisibleHeroIds { get; }
    public IReadOnlyDictionary<string, string> DetachedFacts { get; }
}

public sealed class InteractionEnvelope
{
    public InteractionEnvelope(
        GameInteractionSnapshot snapshot,
        IEnumerable<PromptMessage> history,
        DetachedPromptSections promptSections = null,
        DetachedPostprocessPromptSections postprocessPromptSections = null)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        History = ContractCollections.CopyList(history);
        PromptSections = promptSections ?? DetachedPromptSections.Empty;
        PostprocessPromptSections = postprocessPromptSections ?? DetachedPostprocessPromptSections.Empty;
    }

    public GameInteractionSnapshot Snapshot { get; }
    public IReadOnlyList<PromptMessage> History { get; }
    public DetachedPromptSections PromptSections { get; }
    public DetachedPostprocessPromptSections PostprocessPromptSections { get; }
}

/// <summary>
/// String-only prompt fragments produced at the interaction boundary. The
/// fragments are deliberately not generated here: the existing channel owner
/// remains authoritative for Persona, history/AFEF, knowledge/RAG, rules and
/// channel-specific eligibility. This type only freezes their order and keeps
/// live game objects out of the async prompt composer.
/// </summary>
public sealed class DetachedPromptSections
{
    public static readonly DetachedPromptSections Empty = new DetachedPromptSections(
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        true);

    public DetachedPromptSections(
        IEnumerable<string> systemSections,
        IEnumerable<string> prefixUserSections,
        IEnumerable<string> suffixUserSections,
        bool appendCurrentPlayerInput = true)
    {
        SystemSections = ContractCollections.CopyStrings(systemSections);
        PrefixUserSections = ContractCollections.CopyStrings(prefixUserSections);
        SuffixUserSections = ContractCollections.CopyStrings(suffixUserSections);
        AppendCurrentPlayerInput = appendCurrentPlayerInput;
    }

    public IReadOnlyList<string> SystemSections { get; }
    public IReadOnlyList<string> PrefixUserSections { get; }
    public IReadOnlyList<string> SuffixUserSections { get; }
    public bool AppendCurrentPlayerInput { get; }
}

/// <summary>
/// String-only sections for the action postprocess stage. The channel owner
/// must build tag rules, history/AFEF and runtime target facts before the async
/// boundary. The composer only freezes their order and appends the current
/// visible reply when requested; it never invents an action rule.
/// </summary>
public sealed class DetachedPostprocessPromptSections
{
    public static readonly DetachedPostprocessPromptSections Empty = new DetachedPostprocessPromptSections(
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        true);

    public DetachedPostprocessPromptSections(
        IEnumerable<string> systemSections,
        IEnumerable<string> prefixUserSections,
        IEnumerable<string> suffixUserSections,
        bool appendLatestVisibleReply = true)
    {
        SystemSections = ContractCollections.CopyStrings(systemSections);
        PrefixUserSections = ContractCollections.CopyStrings(prefixUserSections);
        SuffixUserSections = ContractCollections.CopyStrings(suffixUserSections);
        AppendLatestVisibleReply = appendLatestVisibleReply;
    }

    public IReadOnlyList<string> SystemSections { get; }
    public IReadOnlyList<string> PrefixUserSections { get; }
    public IReadOnlyList<string> SuffixUserSections { get; }
    public bool AppendLatestVisibleReply { get; }
}

/// <summary>
/// Atomic main/postprocess Prompt sections captured for one interaction turn.
/// Keeping both halves together prevents a reload or a second capture from
/// pairing one turn's main rules with another turn's action rules.
/// </summary>
public sealed class DetachedInteractionPromptSections
{
    public static readonly DetachedInteractionPromptSections Empty = new DetachedInteractionPromptSections(
        DetachedPromptSections.Empty,
        DetachedPostprocessPromptSections.Empty);

    public DetachedInteractionPromptSections(
        DetachedPromptSections main,
        DetachedPostprocessPromptSections postprocess)
    {
        Main = main ?? DetachedPromptSections.Empty;
        Postprocess = postprocess ?? DetachedPostprocessPromptSections.Empty;
    }

    public DetachedPromptSections Main { get; }
    public DetachedPostprocessPromptSections Postprocess { get; }
}

public sealed class RuleSelection
{
    public RuleSelection(IEnumerable<string> ruleIds, IEnumerable<string> exclusionReasons)
    {
        RuleIds = ContractCollections.CopyStrings(ruleIds);
        ExclusionReasons = ContractCollections.CopyStrings(exclusionReasons);
    }

    public IReadOnlyList<string> RuleIds { get; }
    public IReadOnlyList<string> ExclusionReasons { get; }
}

public sealed class CapabilitySet
{
    public CapabilitySet(IEnumerable<string> capabilityIds)
    {
        CapabilityIds = ContractCollections.CopyStrings(capabilityIds);
    }

    public IReadOnlyList<string> CapabilityIds { get; }
}

public sealed class PromptMessage
{
    public PromptMessage(string role, string content)
    {
        Role = ContractGuard.Required(role, nameof(role));
        Content = content ?? string.Empty;
    }

    public string Role { get; }
    public string Content { get; }
}

public sealed class PromptPackage
{
    public PromptPackage(IEnumerable<PromptMessage> messages, int maxTokens, string model)
    {
        Messages = ContractCollections.CopyList(messages);
        MaxTokens = maxTokens;
        Model = ContractGuard.Required(model, nameof(model));
    }

    public IReadOnlyList<PromptMessage> Messages { get; }
    public int MaxTokens { get; }
    public string Model { get; }
}

public sealed class PostprocessContext
{
    public PostprocessContext(IEnumerable<string> allowedRuleIds, IEnumerable<string> allowedTagFamilies, CapabilitySet capabilities)
    {
        AllowedRuleIds = ContractCollections.CopyStrings(allowedRuleIds);
        AllowedTagFamilies = ContractCollections.CopyStrings(allowedTagFamilies);
        Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    }

    public IReadOnlyList<string> AllowedRuleIds { get; }
    public IReadOnlyList<string> AllowedTagFamilies { get; }
    public CapabilitySet Capabilities { get; }
}

public sealed class ActionRequest
{
    public ActionRequest(string tag, string targetId, IDictionary<string, string> parameters)
    {
        Tag = ContractGuard.Required(tag, nameof(tag));
        TargetId = targetId ?? string.Empty;
        Parameters = ContractCollections.CopyMap(parameters);
    }

    public string Tag { get; }
    public string TargetId { get; }
    public IReadOnlyDictionary<string, string> Parameters { get; }
}

public sealed class ActionPlan
{
    public ActionPlan(IEnumerable<ActionRequest> actions, string rawPostprocessId)
    {
        Actions = ContractCollections.CopyList(actions);
        RawPostprocessId = rawPostprocessId ?? string.Empty;
    }

    public IReadOnlyList<ActionRequest> Actions { get; }
    public string RawPostprocessId { get; }
}

public sealed class FactRecord
{
    public FactRecord(string factType, string subjectId, string text)
    {
        FactType = ContractGuard.Required(factType, nameof(factType));
        SubjectId = ContractGuard.Required(subjectId, nameof(subjectId));
        Text = text ?? string.Empty;
    }

    public string FactType { get; }
    public string SubjectId { get; }
    public string Text { get; }
}

/// <summary>
/// One interaction-boundary memory transaction. The commit id is an
/// ephemeral runtime id used only for detached duplicate suppression; it is
/// deliberately not serialized into a save or exposed to the LLM.
/// </summary>
public sealed class InteractionMemoryCommit
{
    public InteractionMemoryCommit(
        string commitId,
        InteractionChannel channel,
        string sessionId,
        string subjectId,
        string userText,
        string assistantText,
        IEnumerable<FactRecord> confirmedFacts)
    {
        CommitId = ContractGuard.Required(commitId, nameof(commitId));
        Channel = channel;
        SessionId = ContractGuard.Required(sessionId, nameof(sessionId));
        SubjectId = ContractGuard.Required(subjectId, nameof(subjectId));
        UserText = userText ?? string.Empty;
        AssistantText = assistantText ?? string.Empty;
        ConfirmedFacts = ContractCollections.CopyList(confirmedFacts);
    }

    public string CommitId { get; }
    public InteractionChannel Channel { get; }
    public string SessionId { get; }
    public string SubjectId { get; }
    public string UserText { get; }
    public string AssistantText { get; }
    public IReadOnlyList<FactRecord> ConfirmedFacts { get; }
}

public enum MemoryCommitStatus
{
    Applied,
    Duplicate,
    Rejected,
    Failed
}

public sealed class MemoryCommitResult
{
    public MemoryCommitResult(MemoryCommitStatus status, string errorCode = null)
    {
        Status = status;
        ErrorCode = errorCode ?? string.Empty;
    }

    public MemoryCommitStatus Status { get; }
    public string ErrorCode { get; }
    public bool HistoryWritten => Status == MemoryCommitStatus.Applied || Status == MemoryCommitStatus.Duplicate;
}

public sealed class InteractionResult
{
    public InteractionResult(
        InteractionStatus status,
        string visibleReply,
        ActionPlan actionPlan,
        IEnumerable<FactRecord> confirmedFacts,
        string errorCode,
        string rawReply = null,
        string rawPostprocessReply = null)
    {
        Status = status;
        VisibleReply = visibleReply ?? string.Empty;
        ActionPlan = actionPlan ?? new ActionPlan(Array.Empty<ActionRequest>(), string.Empty);
        ConfirmedFacts = ContractCollections.CopyList(confirmedFacts);
        ErrorCode = errorCode ?? string.Empty;
        RawReply = rawReply ?? string.Empty;
        RawPostprocessReply = rawPostprocessReply ?? string.Empty;
    }

    public InteractionStatus Status { get; }
    public string VisibleReply { get; }
    public ActionPlan ActionPlan { get; }
    public IReadOnlyList<FactRecord> ConfirmedFacts { get; }
    public string ErrorCode { get; }
    public string RawReply { get; }
    public string RawPostprocessReply { get; }
}

public interface IInteractionMemory
{
    IReadOnlyList<PromptMessage> Read(string subjectId, int maxItems);
    void Append(string subjectId, PromptMessage message, IEnumerable<FactRecord> confirmedFacts);
}

/// <summary>
/// Optional atomic boundary for the new detached pipeline. Legacy memory
/// implementations may keep using Append; new channel facades should prefer
/// this interface so user/assistant/facts are committed once and can be
/// deduplicated without changing save compatibility.
/// </summary>
public interface IInteractionMemoryBatchCommitter
{
    MemoryCommitResult Commit(InteractionMemoryCommit commit);
}

public interface IActionPlanExecutor
{
    InteractionStatus ValidateAndExecute(ActionPlan actionPlan, GameInteractionSnapshot currentSnapshot);
}

public interface IRuleSelector
{
    RuleSelection Select(GameInteractionSnapshot snapshot);
}

public interface IPromptPackageComposer
{
    PromptPackage Compose(InteractionEnvelope envelope, RuleSelection selection, CapabilitySet capabilities);
}

public interface IPostprocessContextBuilder
{
    PostprocessContext Build(GameInteractionSnapshot snapshot, RuleSelection selection, CapabilitySet capabilities);
}

public interface IActionPostprocessor
{
    ActionPlan Parse(string rawText, PostprocessContext context);
}

public interface IInteractionPipeline
{
    Task<InteractionResult> GenerateAsync(
        InteractionEnvelope envelope,
        LlmProviderSnapshot provider,
        CancellationToken cancellationToken);
}

internal static class ContractGuard
{
    public static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}

internal static class ContractCollections
{
    public static IReadOnlyList<T> CopyList<T>(IEnumerable<T> values)
    {
        return new List<T>(values ?? Enumerable.Empty<T>()).AsReadOnly();
    }

    public static IReadOnlyList<string> CopyStrings(IEnumerable<string> values)
    {
        return new List<string>((values ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value))).AsReadOnly();
    }

    public static IReadOnlyDictionary<string, string> CopyMap(IDictionary<string, string> values)
    {
        Dictionary<string, string> copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (values != null)
        {
            foreach (KeyValuePair<string, string> pair in values)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key))
                {
                    copy[pair.Key.Trim()] = pair.Value ?? string.Empty;
                }
            }
        }

        return new ReadOnlyDictionary<string, string>(copy);
    }
}
