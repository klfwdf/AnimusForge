namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>The native menu shown only after SETS military rewards have been presented.</summary>
public static class SetsVictoryMenuProfile
{
    public const string MenuId = "AnimusForge_sets_victory_aftermath";
    public const string EnterOptionId = "AnimusForge_sets_victory_enter_gccz";
    public const string LeaveOptionId = "AnimusForge_sets_victory_leave";
    public const string LeaveText = "离开";

    public static string GetEntryText(SetsSettlementSceneKind kind)
    {
        switch (kind)
        {
            case SetsSettlementSceneKind.Castle: return "进入 GCCZ 城堡处置";
            case SetsSettlementSceneKind.Village: return "进入 GCCZ 村庄处置";
            default: return "进入 GCCZ 城镇处置";
        }
    }

    public static string BuildSummary(string settlementName, bool ownedIncident = false)
    {
        string name = settlementName ?? "这处定居点";
        return (ownedIncident ? "你在" + name + "的场景事件已经结束。现有战果已经处理。"
                : "你已击溃" + name + "的守军。战斗战果已经处理。")
            + "你可以亲自进入 GCCZ 继续处置，也可以离开。";
    }
}
