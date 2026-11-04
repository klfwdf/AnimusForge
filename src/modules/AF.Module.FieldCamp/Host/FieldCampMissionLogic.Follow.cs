using System;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class FieldCampMissionLogic
{
    // Agent identity is the command node. The existing native FollowAgentBehavior
    // tracks Agent.Main; camp life must never reissue a daily goal while it owns him.
    internal bool CanFollowPlayer(Agent agent) => ReferenceEquals(Mission.Current, Mission)
        && !Mission.IsMissionEnding && agent != null && agent.IsActive() && agent != Agent.Main
        && _campAgentsByAgent.TryGetValue(agent, out CampAgent entry)
        && entry.Activity != Activity.Wounded && !IsCampPrisoner(agent)
        && !SlaughterBusy && !_assembled && !IsDuelist(agent) && !IsCeremonyAgentHeld(agent)
        && !IsEnvoy(entry);

    internal bool IsFollowingPlayer(Agent agent) => agent != null
        && _campAgentsByAgent.TryGetValue(agent, out CampAgent entry) && entry.FollowingPlayer;

    internal bool BeginFollowPlayer(Agent agent)
    {
        if (!CanFollowPlayer(agent)) return false;
        CampAgent entry = _campAgentsByAgent[agent];
        if (entry.FollowingPlayer) return true;
        ReleasePose(entry);
        StopCampNavigation(agent);
        if (!TryPrepareCampNavigation(agent))
        {
            RestoreCampAttentionOwner(agent);
            return false;
        }
        entry.FollowingPlayer = true;
        entry.Returning = false;
        entry.Seated = false;
        entry.MoveBlocked = entry.RouteQueued = entry.RoutePrepared = false;
        entry.Route = Array.Empty<TaleWorlds.Library.Vec3>();
        agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
        agent.SetIsAIPaused(false);
        return true;
    }

    internal bool EndFollowPlayer(Agent agent)
    {
        if (!IsFollowingPlayer(agent)) return false;
        CampAgent entry = _campAgentsByAgent[agent];
        entry.FollowingPlayer = false;
        // The ordinary conversation release will return the station after its owner exits.
        if (!IsSceneConversationHeld(agent)) RestoreCampAttentionOwner(agent);
        return true;
    }
}
