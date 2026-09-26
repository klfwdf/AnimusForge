using System;
using System.Collections.Generic;
using System.Threading;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class ShoutBehavior
{
    internal static string IssueModuleSceneTicket(string clientId)
    {
        ShoutBehavior owner = CurrentInstance;
        if (owner == null || !IsBannerlordMainThreadForNativeActions()) return null;
        ScenePlayerShoutContext context = owner.CaptureModuleSceneContext();
        return context == null ? null : owner._scenePlayerShoutRequestOwner.IssueModuleTicket(clientId, context);
    }

    internal static bool TryClaimModuleSceneTicket(string clientId, string ticketId, out ScenePlayerShoutRequest request)
    {
        request = null;
        ShoutBehavior owner = CurrentInstance;
        return owner != null && IsBannerlordMainThreadForNativeActions()
            && owner._scenePlayerShoutRequestOwner.TryTakeModuleTicket(clientId, ticketId, out ScenePlayerShoutContext context)
            && owner.TryClaimModuleSceneContext(context, out request);
    }

    internal static void RevokeModuleSceneTickets(string clientId)
    {
        CurrentInstance?._scenePlayerShoutRequestOwner.RevokeClientModuleTickets(clientId);
    }

    // Main-thread only. This is a preview, not an input claim or a dialogue dispatch.
    internal ScenePlayerShoutContext CaptureModuleSceneContext()
    {
        if (!IsBannerlordMainThreadForNativeActions() || !ReferenceEquals(CurrentInstance, this)
            || !_isProcessingShout || _activeShoutTargetingContext == null
            || Mission.Current == null || Agent.Main == null)
            return null;

        ShoutTargetingContext source = _activeShoutTargetingContext;
        if (source.CandidateAgentIndices == null || source.PreviewCandidateAgents == null
            || source.CandidatePlayerDistancesMeters == null)
            return null;
        List<Agent> framed = GetAgentsForShoutTargetingContext(source);
        Agent primary = ResolvePrimaryAgentForShoutTargetingContext(source, framed);
        if (framed.Count == 0 || framed.Count != source.CandidateAgentIndices.Count || primary == null)
            return null;

        ShoutTargetingContext frozen = CloneModuleSceneTargetingContext(source);
        return _scenePlayerShoutRequestOwner.CaptureContext(this, Mission.Current, Agent.Main,
            SaveRuntimeGuard.CaptureGeneration(), Volatile.Read(ref _sceneHistorySessionId),
            Volatile.Read(ref _sceneConversationEpoch), source, frozen);
    }

    // Claim precedes any UI/API work. A changed UI selection or reused Agent index cannot retarget it.
    internal bool TryClaimModuleSceneContext(ScenePlayerShoutContext context, out ScenePlayerShoutRequest request)
    {
        request = null;
        if (!IsBannerlordMainThreadForNativeActions() || !ReferenceEquals(CurrentInstance, this)
            || !_isProcessingShout || !IsModuleSceneTargetingSourceCurrent(context))
            return false;
        return _scenePlayerShoutRequestOwner.TryClaimContext(context, this, Mission.Current, Agent.Main,
            SaveRuntimeGuard.IsCurrentGeneration(context.RuntimeGeneration),
            Volatile.Read(ref _sceneHistorySessionId), Volatile.Read(ref _sceneConversationEpoch), out request);
    }

    private bool IsModuleSceneTargetingSourceCurrent(ScenePlayerShoutContext context)
    {
        if (context == null || !ReferenceEquals(context.SourceTargetingContext, _activeShoutTargetingContext))
            return false;
        ShoutTargetingContext source = context.SourceTargetingContext;
        ShoutTargetingContext frozen = context.TargetingContext;
        if (source == null || frozen == null || source.PrimaryAgentIndex != frozen.PrimaryAgentIndex
            || source.RangeMeters != frozen.RangeMeters || source.HalfAngleRadians != frozen.HalfAngleRadians
            || source.CandidateAgentIndices == null || frozen.CandidateAgentIndices == null
            || source.CandidateAgentIndices.Count != frozen.CandidateAgentIndices.Count
            || source.PreviewCandidateAgents == null || frozen.PreviewCandidateAgents == null
            || source.PreviewCandidateAgents.Count != frozen.PreviewCandidateAgents.Count
            || source.CandidatePlayerDistancesMeters == null || frozen.CandidatePlayerDistancesMeters == null
            || source.CandidatePlayerDistancesMeters.Count != frozen.CandidatePlayerDistancesMeters.Count)
            return false;
        for (int i = 0; i < source.CandidateAgentIndices.Count; i++)
            if (source.CandidateAgentIndices[i] != frozen.CandidateAgentIndices[i]) return false;
        for (int i = 0; i < source.PreviewCandidateAgents.Count; i++)
            if (!ReferenceEquals(source.PreviewCandidateAgents[i], frozen.PreviewCandidateAgents[i])) return false;
        foreach (KeyValuePair<int, float> distance in frozen.CandidatePlayerDistancesMeters)
            if (!source.CandidatePlayerDistancesMeters.TryGetValue(distance.Key, out float current)
                || current != distance.Value) return false;

        List<Agent> currentFramed = GetAgentsForShoutTargetingContext(frozen);
        if (currentFramed.Count != frozen.CandidateAgentIndices.Count
            || ResolvePrimaryAgentForShoutTargetingContext(frozen, currentFramed) == null)
            return false;
        Dictionary<int, Agent> previewByIndex = new Dictionary<int, Agent>(frozen.PreviewCandidateAgents.Count);
        for (int i = 0; i < frozen.PreviewCandidateAgents.Count; i++)
        {
            Agent preview = frozen.PreviewCandidateAgents[i];
            if (preview != null && !previewByIndex.ContainsKey(preview.Index))
                previewByIndex.Add(preview.Index, preview);
        }
        for (int i = 0; i < currentFramed.Count; i++)
            if (!previewByIndex.TryGetValue(currentFramed[i].Index, out Agent expected)
                || !ReferenceEquals(currentFramed[i], expected)) return false;
        return true;
    }

    private static ShoutTargetingContext CloneModuleSceneTargetingContext(ShoutTargetingContext source)
    {
        return new ShoutTargetingContext
        {
            RangeMeters = source.RangeMeters,
            HalfAngleRadians = source.HalfAngleRadians,
            PrimaryAgentIndex = source.PrimaryAgentIndex,
            CandidateAgentIndices = new List<int>(source.CandidateAgentIndices),
            CandidatePlayerDistancesMeters = new Dictionary<int, float>(source.CandidatePlayerDistancesMeters),
            PreviewCandidateAgents = new List<Agent>(source.PreviewCandidateAgents)
        };
    }
}
