using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free limits for how many NPCs may speak in response to a GCCZ event.
/// AF adapters own MCM reads, live agent selection, and speech queue side effects.
/// </summary>
public static class SiegeNpcResponseLimitProfile
{
    public const int MinResponseLimit = 1;

    public const int MaxResponseLimit = 10;

    public const int DefaultResponseLimit = MaxResponseLimit;

    public const bool DefaultUnlimited = false;

    public const string McmGroupName = "14. 攻城处置&内部暴乱";

    public const string DiagnosticLogFileName = "GCCZ_Debug.log";

    public static int ClampResponseLimit(int value)
    {
        return Math.Max(MinResponseLimit, Math.Min(MaxResponseLimit, value));
    }

    public static int ResolveAllowedResponseCount(bool unlimited, int configuredLimit, int availableCount)
    {
        int safeAvailableCount = Math.Max(0, availableCount);
        if (unlimited)
        {
            return safeAvailableCount;
        }

        return Math.Min(safeAvailableCount, ClampResponseLimit(configuredLimit));
    }
}
