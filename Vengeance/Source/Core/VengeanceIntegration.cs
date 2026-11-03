using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Core;

/// <summary>Bounded camp preparation acknowledgement; no engine actors are created while pending.</summary>
internal enum FieldCampCeremonyReadiness
{
    Pending = 0,
    Ready = 1,
    Failed = 2
}

// Same-DLL preparation footprint, in world coordinates; not a public sub-MOD API.
internal readonly struct FieldCampCeremonyArea
{
    internal readonly Vec2 Min;
    internal readonly Vec2 Max;
    private readonly Vec2 _origin;
    private readonly Vec2 _side;
    private readonly Vec2 _forward;
    private readonly Vec2 _localMin;
    private readonly Vec2 _localMax;

    internal FieldCampCeremonyArea(Vec2 min, Vec2 max)
    {
        Min = _localMin = min;
        Max = _localMax = max;
        _origin = Vec2.Zero;
        _side = new Vec2(1f, 0f);
        _forward = new Vec2(0f, 1f);
    }

    internal FieldCampCeremonyArea(MatrixFrame frame, Vec3 localMin, Vec3 localMax)
    {
        _origin = frame.origin.AsVec2;
        var side = frame.rotation.s.AsVec2;
        if (!side.IsNonZero())
        {
            var forward = frame.rotation.f.AsVec2;
            side = forward.IsNonZero() ? new Vec2(forward.y, -forward.x) : new Vec2(1f, 0f);
        }
        _side = side.Normalized();
        _forward = new Vec2(-_side.y, _side.x);
        var min = new Vec2(float.MaxValue, float.MaxValue);
        var max = new Vec2(float.MinValue, float.MinValue);
        var projectedMin = min;
        var projectedMax = max;
        // Measured once at confirmation. Keep yaw in the narrow phase instead
        // of clearing the empty corners of a rotated world-axis bounding box.
        for (var i = 0; i < 8; i++)
        {
            var p = frame.TransformToParent(new Vec3((i & 1) == 0 ? localMin.x : localMax.x,
                (i & 2) == 0 ? localMin.y : localMax.y, (i & 4) == 0 ? localMin.z : localMax.z));
            min.x = MathF.Min(min.x, p.x); min.y = MathF.Min(min.y, p.y);
            max.x = MathF.Max(max.x, p.x); max.y = MathF.Max(max.y, p.y);
            var delta = p.AsVec2 - _origin;
            var x = delta.x * _side.x + delta.y * _side.y;
            var y = delta.x * _forward.x + delta.y * _forward.y;
            projectedMin.x = MathF.Min(projectedMin.x, x); projectedMin.y = MathF.Min(projectedMin.y, y);
            projectedMax.x = MathF.Max(projectedMax.x, x); projectedMax.y = MathF.Max(projectedMax.y, y);
        }
        Min = min; Max = max;
        _localMin = projectedMin; _localMax = projectedMax;
    }

    internal bool Contains(Vec3 point, float margin = 0.65f)
    {
        if (point.x < Min.x - margin || point.x > Max.x + margin ||
            point.y < Min.y - margin || point.y > Max.y + margin) return false;
        var delta = point.AsVec2 - _origin;
        var x = delta.x * _side.x + delta.y * _side.y;
        var y = delta.x * _forward.x + delta.y * _forward.y;
        return x >= _localMin.x - margin && x <= _localMax.x + margin &&
            y >= _localMin.y - margin && y <= _localMax.y + margin;
    }
}

/// <summary>Owns the one-active-host rule for the standalone and embedded builds.</summary>
public static class VengeanceIntegration
{
    private static readonly object Sync = new();
    private static readonly HashSet<int> ProtectedVictimAgentIndexes = new();
    // Victims past the lethal frame. Some methods keep the Agent active in a
    // frozen death pose, so hosts must not treat it as a living speaker.
    private static readonly HashSet<int> ExecutedVictimAgentIndexes = new();
    private static bool _embeddedHostActive;

    public static bool IsEnabled { get; private set; } = true;

    // Optional same-DLL host input owner; standalone has no dependency on AF UI.
    internal static Func<bool>? SceneHotkeysBlocked { get; set; }

    // Optional camp owner. Preparation is polled at most every 0.5 s, keyed by
    // session; staging and release remain once-only notifications. No AF types
    // enter the shared standalone source. Missing preparation keeps legacy flow.
    public static Func<Mission?, bool>? FieldCampMissionProbe { get; set; }
    public static Action<Hero, Vec3, Vec2>? FieldCampCeremonyStaged { get; set; }
    public static Action? FieldCampCeremonyReleased { get; set; }
    internal static Func<Guid, Hero, Vec3, Vec2, float, IReadOnlyList<FieldCampCeremonyArea>, FieldCampCeremonyReadiness>? FieldCampCeremonyPreparation { get; set; }
    internal static Func<Guid, IReadOnlyList<Agent>>? FieldCampCeremonyBorrowedCrowd { get; set; }
    internal static Action<Guid>? FieldCampCeremonyPreparationReleased { get; set; }
    internal static Action<Guid, IReadOnlyList<FieldCampCeremonyArea>, bool>? FieldCampCeremonyGeometryChanged { get; set; }
    internal static Func<Guid, string, Hero?, Agent?>? FieldCampAcquireActor { get; set; }
    internal static Func<Agent, bool>? FieldCampOwnsVictimOrigin { get; set; }
    internal static Func<Guid, Agent, Vec3, Vec2, IReadOnlyList<Vec3>, bool>? FieldCampWalkActor { get; set; }
    internal static Func<Guid, FieldCampCeremonyReadiness>? FieldCampActorReadiness { get; set; }
    internal static Action? FieldCampReleaseActors { get; set; }
    internal static Func<Hero, bool>? FieldCampPrisonerPresent { get; set; }
    internal static Action<Guid>? FieldCampArrangeSpectators { get; set; }

    // Read the camp owner's activity state only before starting preparation.
    internal static Func<Mission?, bool>? FieldCampCeremonyAdmissionProbe { get; set; }

    internal static bool CanStartFieldCampCeremony()
    {
        try
        {
            return IsEnabled && FieldCampCeremonyAdmissionProbe?.Invoke(Mission.Current) == true;
        }
        catch
        {
            return false;
        }
    }

    internal static Action<Mission, RichExecutions.Scene.TownExecutionMissionBehavior>? FieldCampControllerChanged { get; set; }

    public static bool IsFieldCampAvailable()
    {
        try
        {
            return FieldCampMissionProbe?.Invoke(Mission.Current) == true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsEmbeddedHostActive
    {
        get
        {
            lock (Sync)
            {
                return _embeddedHostActive;
            }
        }
    }

    public static void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
    }

    public static bool TryClaimEmbeddedHost()
    {
        lock (Sync)
        {
            if (!IsEnabled || _embeddedHostActive)
            {
                return false;
            }

            _embeddedHostActive = true;
            return true;
        }
    }

    public static void ReleaseEmbeddedHost()
    {
        lock (Sync)
        {
            _embeddedHostActive = false;
            SceneHotkeysBlocked = null;
            ProtectedVictimAgentIndexes.Clear();
            ExecutedVictimAgentIndexes.Clear();
        }
    }

    public static void MarkExecutedVictim(Agent? victim)
    {
        if (victim is null)
        {
            return;
        }

        lock (Sync)
        {
            ExecutedVictimAgentIndexes.Add(victim.Index);
        }
    }

    public static bool IsExecutedVictim(Agent? victim)
    {
        if (victim is null)
        {
            return false;
        }

        lock (Sync)
        {
            return ExecutedVictimAgentIndexes.Count > 0 && ExecutedVictimAgentIndexes.Contains(victim.Index);
        }
    }

    public static void RegisterProtectedVictim(Agent? victim)
    {
        if (!IsEnabled || victim is null)
        {
            return;
        }

        lock (Sync)
        {
            ProtectedVictimAgentIndexes.Add(victim.Index);
        }
    }

    public static void ClearProtectedVictim(Agent? victim)
    {
        if (victim is null)
        {
            return;
        }

        lock (Sync)
        {
            ProtectedVictimAgentIndexes.Remove(victim.Index);
            ExecutedVictimAgentIndexes.Remove(victim.Index);
        }
    }

    public static bool IsProtectedVictim(Agent? victim)
    {
        if (!IsEnabled || victim is null)
        {
            return false;
        }

        lock (Sync)
        {
            return ProtectedVictimAgentIndexes.Contains(victim.Index);
        }
    }
}
