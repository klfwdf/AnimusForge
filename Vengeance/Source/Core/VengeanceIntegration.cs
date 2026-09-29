using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Core;

/// <summary>
/// Owns the one-active-host rule shared by the standalone module and the
/// AnimusForge-hosted build. The standalone submodule checks this before it
/// registers anything, so an AnimusForge build can take over without loading
/// a second copy of the menus, campaign behavior, or mission controller.
/// </summary>
public static class VengeanceIntegration
{
    private static readonly object Sync = new();
    private static readonly HashSet<int> ProtectedVictimAgentIndexes = new();
    private static bool _embeddedHostActive;

    public static bool IsEnabled { get; private set; } = true;

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
