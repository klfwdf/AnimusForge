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
        "彻底不信", "极度怀疑", "强烈戒备", "不太信任", "保留态度",
        "中性观望", "基本信任", "较高信任", "高度信任", "完全信赖"
    };

    private static readonly string[] BehaviorTexts =
    {
        "彻底不信任玩家，优先回避一切经济接触。",
        "仍高度怀疑玩家，只愿进行极端保守的交易试探。",
        "明显戒备，允许有限交易但条件苛刻。",
        "偏不信任，接受交易但会要求更高风险补偿。",
        "保留态度，交易可谈但会附加明显溢价。",
        "中性观望，可做常规交易但仍以安全为先。",
        "达到基本信任，可在可控风险下提供信用交易。",
        "较高信任，可提供中大额信用交易。",
        "高度信任，可提供高额信用交易。",
        "完全信赖，可提供接近无限额的信用交易。"
    };

    private static readonly string[] ActionGuideTexts =
    {
        "L1：拒绝任何经济往来。禁止买卖、禁止赊账、禁止借款；不输出任何新增交易/欠款标签。",
        "L2：可进行即时交易，但应明显高于 guidePrice 定价以覆盖风险；禁止赊账/借款。",
        "L3：可进行即时交易，定价仍偏高；禁止赊账/借款。",
        "L4：可进行即时交易，定价可在“偏高到合理”之间浮动；禁止赊账/借款。",
        "L5：可进行即时交易，定价趋于合理但可保留风险溢价；禁止赊账/借款。",
        "L6：可进行即时交易，定价基本合理；禁止赊账/借款。",
        "L7：可赊账/借款（小到中额），额度需结合NPC财力与局势评估；默认计息。",
        "L8：可赊账/借款（中到较大额），额度需结合NPC财力与局势评估；默认计息。",
        "L9：可赊账/借款（大额），额度需结合NPC财力与局势评估；默认计息。",
        "L10：可赊账/借款（极高额度），仍需结合NPC财力、性格与风险控制；默认计息。"
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
