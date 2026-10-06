using System;
using System.Collections.Generic;
using SandBox.Missions.MissionLogics;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

internal sealed class ExecutionCrowdRoute
{
    private readonly Agent _agent;
    private readonly Vec2 _center;
    private readonly float _radius;
    private readonly MissionAgentHandler _handler;
    private readonly List<UsableMachine> _targets = new();
    private readonly NavigationPath _path = new();
    private int _cursor;
    private int _captures;
    internal ExecutionCrowdRoute(Agent agent, MissionAgentHandler handler, Vec2 center, float radius)
    {
        _agent = agent;
        _center = center;
        _radius = radius;
        _handler = handler;
        // Capture real available standing points, including paired native
        // objects and the limited pool. At most three captures per spectator.
        CaptureTargets();
    }

    private void CaptureTargets()
    {
        _captures++;
        var common = _handler.FindAllUnusedPoints(_agent, "npc_common") ?? new List<UsableMachine>();
        var limited = _handler.FindAllUnusedPoints(_agent, "npc_common_limited") ?? new List<UsableMachine>();
        // Give the fallback pool a chance even in scenes with many unusable
        // common routes. Deduplicate only during bounded capture, not per Tick.
        var seen = new HashSet<UsableMachine>(_targets);
        for (int i = 0; i < Math.Max(common.Count, limited.Count); i++)
        {
            if (i < common.Count && seen.Add(common[i])) _targets.Add(common[i]);
            if (i < limited.Count && seen.Add(limited[i])) _targets.Add(limited[i]);
        }
    }

    internal bool IsAvailable(UsableMachine target) => target != null && !target.IsDisabled
        && !target.IsDestroyed && target.IsStandingPointAvailableForAgent(_agent);

    internal UsableMachine? FindNext()
    {
        if (_captures < ExecutionCrowdDispersalSchedule.MaximumAttempts
            && (_targets.Count == 0 || _cursor >= _targets.Count)) CaptureTargets();
        // At most 24 cheap candidate checks and four native path requests per
        // attempt. Cursor persists; a failed frame never repeats a full scan.
        int paths = 0;
        int checks = Math.Min(24, _targets.Count);
        for (int i = 0; i < checks; i++)
        {
            var target = _targets[_cursor++ % _targets.Count];
            if (!IsAvailable(target)) continue;
            var point = target.GetVacantStandingPointForAI(_agent);
            if (point == null || point.IsDisabledForAgent(_agent)) continue;
            var end = point.GetUserFrameForAgent(_agent).Origin;
            var start = _agent.GetWorldPosition();
            if (!ExecutionCrowdRoutePolicy.IsOutwardTarget(start.AsVec2, end.AsVec2, _center, _radius)) continue;
            var first = start.GetNavMesh();
            var last = end.GetNavMesh();
            if (first == UIntPtr.Zero || last == UIntPtr.Zero) continue;
            paths++;
            _path.Size = 0;
            if (_agent.Mission.Scene.GetPathBetweenAIFaces(first, last, start.AsVec2, end.AsVec2,
                    0.5f, _path, null) && IsSafePath(start.AsVec2, end.AsVec2)) return target;
            if (paths >= 4) break;
        }
        return null;
    }

    private bool IsSafePath(Vec2 start, Vec2 end)
    {
        if (_path.Size < 0 || _path.Size >= _path.PathPoints.Length) return false;
        var previous = start;
        for (int i = 0; i < _path.Size; i++)
        {
            var next = _path[i];
            if (!ExecutionCrowdRoutePolicy.IsSafeSegment(previous, next, _center, _radius)) return false;
            previous = next;
        }
        return ExecutionCrowdRoutePolicy.IsSafeSegment(previous, end, _center, _radius);
    }
}
