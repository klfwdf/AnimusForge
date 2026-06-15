namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free UI wording for mercy-track transitions around destructive outcomes.
/// AF adapters still own live outcome flags, plunder state, logging, and display side effects.
/// </summary>
public static class SiegeMercyTrackTransitionProfile
{
    public const uint BlockedAfterDestructiveMessageColor = 0xFFFFD27Fu;

    public const uint ReversiblePlunderStoppedMessageColor = 0xFFB6F7A8u;

    public const string ReversiblePlunderStoppedMessage = "【攻城处置】搜掠已被后续宽恕/宣抚类指令覆盖，士兵停止索财；离场将按当前正向处置结算。";

    public static string BuildBlockedAfterDestructiveMessage(string actionName)
    {
        string safeActionName = string.IsNullOrWhiteSpace(actionName) ? "安抚" : actionName.Trim();
        return "【攻城处置】" + safeActionName + "不能覆盖已经升级的处置；血洗不能回退为搜掠、宽恕或救济，屠民迁殖是最高级结算。";
    }
}
