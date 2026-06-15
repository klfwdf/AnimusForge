namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free current-outcome wording for the GCCZ postprocess context.
/// </summary>
public static class SiegePostprocessOutcomeTextBuilder
{
    public static string Build(SiegePostprocessOutcomeFacts facts)
    {
        if (facts == null)
        {
            return "尚未选择最终处置";
        }

        if (facts.MassacreStarted)
        {
            return "血洗已开始，不可回退";
        }

        if (facts.PlunderStarted)
        {
            return "搜掠已开始，但可被后续宽恕/安抚/宣抚覆盖";
        }

        if (facts.HasPendingAftermath)
        {
            return "已有待结算处置：" + (facts.PendingAftermathName ?? string.Empty).Trim();
        }

        return "尚未选择最终处置";
    }
}
