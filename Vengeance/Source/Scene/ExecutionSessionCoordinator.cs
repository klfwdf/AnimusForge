using System;
using System.Linq;
using RichExecutions.Core;
using RichExecutions.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

internal sealed class PendingExecutionSession
{
    public PendingExecutionSession(ExecutionRequest request, ExecutionService service)
    {
        Request = request;
        Service = service;
    }

    public ExecutionRequest Request { get; }
    public ExecutionService Service { get; }
    public bool IsClaimed { get; set; }
}

internal static class ExecutionSessionCoordinator
{
    private static readonly object Sync = new();
    private static PendingExecutionSession? _pending;
    private static TownExecutionMissionBehavior? _activeController;

    public static bool Prepare(ExecutionRequest request, ExecutionService service)
    {
        lock (Sync)
        {
            if (_pending is not null || _activeController is not null)
            {
                return false;
            }

            _pending = new PendingExecutionSession(request, service);
        }

        RexLog.Info(
            $"Prepared session {request.SessionId} for venue '{request.Venue.StringId}' " +
            $"and prisoner '{request.Victim.StringId}'.");
        return true;
    }

    public static bool TryClaimForMission(Mission mission, out PendingExecutionSession session)
    {
        PendingExecutionSession? rejected = null;
        string? rejectionReason = null;
        lock (Sync)
        {
            if (_pending is null || _pending.IsClaimed)
            {
                session = null!;
                return false;
            }

            if (!IsExpectedTownCenterMission(mission, _pending.Request, out var mismatchReason))
            {
                rejected = _pending;
                rejectionReason = mismatchReason;
                _pending = null;
                _activeController = null;
                session = null!;
            }
            else
            {
                _pending.IsClaimed = true;
                session = _pending;
                RexLog.Info(
                    $"Claimed session {session.Request.SessionId} for town-center scene " +
                    $"'{mission.SceneName ?? "<null>"}'.");
                return true;
            }
        }

        if (rejected is not null)
        {
            RexLog.Warning(
                $"Rejected mission scene '{mission.SceneName ?? "<null>"}' for pending session " +
                $"{rejected.Request.SessionId}: {rejectionReason ?? "unknown mismatch"}");
            rejected.Service.Cancel(
                rejected.Request,
                ExecutionFailureReason.ScenePlacementFailed,
                new TextObject(
                    "{=REX_Error_Mission_Inject}The execution scene could not be initialized safely. Nothing was spent and the prisoner lives."));
        }

        return false;
    }

    private static bool IsExpectedTownCenterMission(
        Mission mission,
        ExecutionRequest request,
        out string mismatchReason)
    {
        mismatchReason = string.Empty;
        if (mission is null)
        {
            mismatchReason = "mission was null";
            return false;
        }

        if (!request.Venue.IsTown || request.Venue.Town is null)
        {
            mismatchReason = "the requested venue is no longer a valid town";
            return false;
        }

        // Settlement.CurrentSettlement can be transiently null while a
        // location mission is opened. The encounter owns the same stable
        // settlement identity and remains available through that hand-off.
        var encounterSettlement = PlayerEncounter.LocationEncounter?.Settlement;
        var settlementMatches = ReferenceEquals(Settlement.CurrentSettlement, request.Venue) ||
                               ReferenceEquals(encounterSettlement, request.Venue);
        if (!settlementMatches)
        {
            mismatchReason =
                $"neither CurrentSettlement nor LocationEncounter matched venue '{request.Venue.StringId}'";
            return false;
        }

        // Location identity is more reliable than SceneName for town-overhaul
        // modules, which can replace a center scene without changing the town
        // encounter that opened it.
        var currentLocation = CampaignMission.Current?.Location;
        if (string.Equals(currentLocation?.StringId, "center", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var center = request.Venue.LocationComplex?.GetLocationWithId("center");
        if (center is null)
        {
            mismatchReason = "the town center location was unavailable";
            return false;
        }

        var expectedScene = center.GetSceneName(request.Venue.Town.GetWallLevel());
        if (!string.IsNullOrWhiteSpace(expectedScene) &&
            string.Equals(mission.SceneName, expectedScene, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        mismatchReason =
            $"location was '{currentLocation?.StringId ?? "<unavailable>"}', " +
            $"scene was '{mission.SceneName ?? "<null>"}', expected '{expectedScene ?? "<null>"}'";
        return false;
    }

    public static void InjectClaimedSession(Mission mission, PendingExecutionSession session)
    {
        RexLog.Info(
            $"Injecting session {session.Request.SessionId} into mission scene " +
            $"'{mission.SceneName ?? "<null>"}'.");
        var downstreamDecider = mission.MissionBehaviors
            .OfType<IAgentStateDecider>()
            .FirstOrDefault();
        var deathLogic = new ExecutionVictimDeathMissionLogic(
            session.Request,
            downstreamDecider);
        var controller = new TownExecutionMissionBehavior(
            session.Request,
            session.Service,
            RichExecutionApi.SceneActs,
            deathLogic);
        AddExecutionMissionBehaviorsAtomically(
            mission,
            deathLogic,
            controller,
            downstreamDecider);
    }

    private static void AddExecutionMissionBehaviorsAtomically(
        Mission mission,
        ExecutionVictimDeathMissionLogic deathLogic,
        TownExecutionMissionBehavior controller,
        IAgentStateDecider downstreamDecider)
    {
        try
        {
            mission.AddMissionBehavior(deathLogic);
            mission.AddMissionBehavior(controller);
            PlaceDeathLogicBeforeDownstreamDecider(mission, deathLogic, downstreamDecider);
            var firstDecider = mission.MissionBehaviors.OfType<IAgentStateDecider>().FirstOrDefault();
            if (!ReferenceEquals(firstDecider, deathLogic))
            {
                throw new InvalidOperationException(
                    "Execution death logic was not the first IAgentStateDecider after insertion.");
            }

            ExecutionSessionCoordinator.RegisterController(controller);
        }
        catch
        {
            TryRemoveInjectedBehavior(mission, controller);
            TryRemoveInjectedBehavior(mission, deathLogic);
            throw;
        }
    }

    private static void PlaceDeathLogicBeforeDownstreamDecider(
        Mission mission,
        ExecutionVictimDeathMissionLogic deathLogic,
        IAgentStateDecider downstreamDecider)
    {
        var deathLogicIndex = mission.MissionBehaviors.IndexOf(deathLogic);
        if (deathLogicIndex < 0)
        {
            throw new InvalidOperationException("Execution death logic was absent after Mission.AddMissionBehavior.");
        }

        if (downstreamDecider is not MissionBehavior downstreamBehavior) return;
        var downstreamIndex = mission.MissionBehaviors.IndexOf(downstreamBehavior);
        if (downstreamIndex < 0)
        {
            throw new InvalidOperationException(
                "The captured downstream IAgentStateDecider disappeared during execution injection.");
        }

        if (deathLogicIndex < downstreamIndex) return;
        mission.MissionBehaviors.RemoveAt(deathLogicIndex);
        mission.MissionBehaviors.Insert(downstreamIndex, deathLogic);
    }

    private static void TryRemoveInjectedBehavior(Mission mission, MissionBehavior behavior)
    {
        try
        {
            if (mission.MissionBehaviors.Contains(behavior)) mission.RemoveMissionBehavior(behavior);
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not remove a partially injected execution behavior.", exception);
        }
    }

    public static void RegisterController(TownExecutionMissionBehavior controller)
    {
        lock (Sync)
        {
            _activeController = controller;
        }
    }

    public static void Release(Guid sessionId)
    {
        lock (Sync)
        {
            if (_pending?.Request.SessionId == sessionId)
            {
                _pending = null;
            }

            if (_activeController?.Request.SessionId == sessionId)
            {
                _activeController = null;
            }
        }
    }

    public static void CancelPending(
        ExecutionFailureReason reason,
        TextObject message)
    {
        lock (Sync)
        {
            if (_pending is null)
            {
                return;
            }

            _pending.Service.Cancel(_pending.Request, reason, message);
            _pending = null;
            _activeController = null;
        }
    }

    public static bool IsConversationWithExecutioner()
    {
        lock (Sync)
        {
            return _activeController?.IsConversationWithExecutioner() == true;
        }
    }

    public static bool IsCrossbowConversationWithExecutioner()
    {
        lock (Sync)
        {
            return _activeController?.IsCrossbowConversationWithExecutioner() == true;
        }
    }

    public static void RequestExecutionerStart()
    {
        lock (Sync)
        {
            _activeController?.TryBeginExecution(ExecutionActor.Executioner);
        }
    }

    public static void RequestPlayerStart()
    {
        lock (Sync)
        {
            _activeController?.TryBeginExecution(ExecutionActor.Player);
        }
    }
}
