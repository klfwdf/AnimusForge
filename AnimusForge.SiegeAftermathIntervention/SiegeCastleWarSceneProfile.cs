using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free contract for opening castle aftermath in the vanilla siege scene layer.
/// Bannerlord scene lookup and mission preparation remain in the fused runtime bridge.
/// </summary>
public static class SiegeCastleWarSceneProfile
{
    public const string CenterLocationId = "center";

    public const string SiegeSceneLevelTag = "siege";

    public const string RequiredMissionHostName = "Battle";

    public static string BuildSceneLevels(string upgradeLevelTag)
    {
        string normalized = upgradeLevelTag?.Trim() ?? string.Empty;
        return normalized.Length == 0
            ? SiegeSceneLevelTag
            : normalized + " " + SiegeSceneLevelTag;
    }

    public static float[] NormalizeWallHitPointRatios(IEnumerable<float> ratios)
    {
        if (ratios == null)
        {
            return Array.Empty<float>();
        }

        List<float> normalized = new List<float>();
        foreach (float ratio in ratios)
        {
            if (float.IsNaN(ratio) || float.IsInfinity(ratio))
            {
                normalized.Add(1f);
            }
            else if (ratio <= 0f)
            {
                normalized.Add(0f);
            }
            else
            {
                normalized.Add(ratio >= 1f ? 1f : ratio);
            }
        }

        return normalized.ToArray();
    }

    public static int CountBreachedWallSections(IEnumerable<float> ratios)
    {
        if (ratios == null)
        {
            return 0;
        }

        int count = 0;
        foreach (float ratio in ratios)
        {
            if (!float.IsNaN(ratio) && ratio <= 0.00001f)
            {
                count++;
            }
        }

        return count;
    }
}
