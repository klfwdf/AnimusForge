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
    internal FieldCampCeremonyArea(Vec2 min, Vec2 max) { Min = min; Max = max; }
    internal bool Contains(Vec3 point, float margin = 0.65f) =>
        point.x >= Min.x - margin && point.x <= Max.x + margin &&
        point.y >= Min.y - margin && point.y <= Max.y + margin;
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
