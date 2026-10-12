using System;

namespace AnimusForge.Modules.Economy;

/// <summary>
/// Stateless trust scale and prompt semantics. Storage, carry accumulation,
/// public-pool fan-out, and relationship mutation remain in the game owner.
/// </summary>
internal static class EconomyTrustPolicy
{
    private static readonly string[] LevelTexts =
    {
        EconomyPromptTexts.Get("S694"), EconomyPromptTexts.Get("S695"), EconomyPromptTexts.Get("S696"), EconomyPromptTexts.Get("S697"), EconomyPromptTexts.Get("S698"),
        EconomyPromptTexts.Get("S699"), EconomyPromptTexts.Get("S700"), EconomyPromptTexts.Get("S701"), EconomyPromptTexts.Get("S702"), EconomyPromptTexts.Get("S703")
    };

    private static readonly string[] BehaviorTexts =
    {
        EconomyPromptTexts.Get("S704"),
        EconomyPromptTexts.Get("S705"),
        EconomyPromptTexts.Get("S706"),
        EconomyPromptTexts.Get("S707"),
        EconomyPromptTexts.Get("S708"),
        EconomyPromptTexts.Get("S709"),
        EconomyPromptTexts.Get("S710"),
        EconomyPromptTexts.Get("S711"),
        EconomyPromptTexts.Get("S712"),
        EconomyPromptTexts.Get("S713")
    };

    private static readonly string[] ActionGuideTexts =
    {
        EconomyPromptTexts.Get("S714"),
        EconomyPromptTexts.Get("S715"),
        EconomyPromptTexts.Get("S716"),
        EconomyPromptTexts.Get("S717"),
        EconomyPromptTexts.Get("S718"),
        EconomyPromptTexts.Get("S719"),
        EconomyPromptTexts.Get("S720"),
        EconomyPromptTexts.Get("S721"),
        EconomyPromptTexts.Get("S722"),
        EconomyPromptTexts.Get("S723")
    };

    internal static int Clamp(int value)
    {
        return Math.Max(-100, Math.Min(100, value));
    }

    internal static int GetLevelIndex(int trust)
    {
        double normalized = ((double)Clamp(trust) + 100.0) / 200.0;
        int level = (int)Math.Floor(normalized * 10.0) + 1;
        return Math.Max(1, Math.Min(10, level));
    }

    internal static string GetLevelText(int trust)
    {
        return LevelTexts[GetLevelIndex(trust) - 1];
    }

    internal static string GetBehaviorText(int trust)
    {
        return BehaviorTexts[GetLevelIndex(trust) - 1];
    }

    internal static string GetActionGuideText(int trust)
    {
        return ActionGuideTexts[GetLevelIndex(trust) - 1];
    }
}
