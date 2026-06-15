namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free UI wording for final GCCZ completion and encounter-exit notifications.
/// AF adapters still own live aftermath labels, loot totals, and Bannerlord display side effects.
/// </summary>
public static class SiegeInterventionCompletionUiProfile
{
    public const uint CompletionMessageColor = 0xFFB6F7A8u;

    public const uint MassacreVictoryMessageColor = 0xFFFF7777u;

    public const string DoneContinueMenuOptionText = "继续...";

    public const string DoneMenuFallbackText = "攻城后的入城处置已经完成。按继续结束本次攻城遭遇。";

    public const string CompletedSummaryFallbackText = "攻城后的入城处置已经完成，正在结束本次攻城遭遇。";

    public const string MassacreVictoryMessage = "【攻城处置】血洗完成：城内残余抵抗已经肃清。离场后将结算战利品和第纳尔。";

    public const string MassacreVictoryQuickText = "血洗完成，离场后结算战利品。";

    public const string LeaveEncounterQuickText = "攻城后处置已完成，正在离开攻城遭遇。";

    public const string CulturalRepopulationCompletedLabel = "屠民迁殖";

    public const string DevastateCompletedLabel = "血洗/毁坏";

    public const string PlunderCompletedLabel = "搜掠";

    public const string MercyCompletedLabel = "安抚";

    public const string DefaultCompletedLabel = "处置";

    public static string BuildCompletedEncounterMessage(SiegeAftermathResolutionKind aftermathKind, bool culturalRepopulationApplied)
    {
        return BuildCompletedEncounterMessage(GetCompletedEncounterLabel(aftermathKind, culturalRepopulationApplied));
    }

    public static string BuildCompletedEncounterMessage(string actionLabel)
    {
        return "【攻城处置】攻城后" + NormalizeActionLabel(actionLabel) + "已经结算完成，正在结束攻城遭遇。";
    }

    public static string GetCompletedEncounterLabel(SiegeAftermathResolutionKind aftermathKind, bool culturalRepopulationApplied)
    {
        switch (aftermathKind)
        {
            case SiegeAftermathResolutionKind.Devastate:
                return culturalRepopulationApplied ? CulturalRepopulationCompletedLabel : DevastateCompletedLabel;
            case SiegeAftermathResolutionKind.Pillage:
                return PlunderCompletedLabel;
            case SiegeAftermathResolutionKind.ShowMercy:
                return MercyCompletedLabel;
            default:
                return DefaultCompletedLabel;
        }
    }

    public static string BuildLootSettlementSummaryMessage(int marketItemTotal, int marketStackKinds, int marketGold, int civilianGold)
    {
        return "【战利清点】结算：市场物资 " + ClampNonNegative(marketItemTotal) + " 件 / " + ClampNonNegative(marketStackKinds) + " 类，市场金库 " + ClampNonNegative(marketGold) + "，民众第纳尔 " + ClampNonNegative(civilianGold) + "。";
    }

    private static string NormalizeActionLabel(string actionLabel)
    {
        return string.IsNullOrWhiteSpace(actionLabel) ? "处置" : actionLabel.Trim();
    }

    private static int ClampNonNegative(int value)
    {
        return value < 0 ? 0 : value;
    }
}
