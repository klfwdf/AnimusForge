using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy for gathering civilians during an ordinary SETS town visit.
/// Live mission-agent selection and formation assignment remain in the fused AF adapter.
/// </summary>
public static class SetsTownCivilianGatherProfile
{
    // FormationClass.Cavalry, displayed as command group 3.
    public const int NativeCommandFormationClassIndex = 2;

    public const float FormationAssignmentInitialDelaySeconds = 0.8f;

    public const float FormationAssignmentBatchIntervalSeconds = 0.12f;

    public const float FormationOrderFinalizeDelaySeconds = 0.25f;

    public const int FormationAssignmentBatchSize = 8;

    public const uint MessageColor = 0xFFB6F7A8u;

    public const string PlayerCommandSource = "sets_town_civilian_gather_player_command";

    public const string AiActionTagSource = "sets_town_civilian_gather_action_tag";

    public const string NoEligibleCivilianMessage = "【SETS】当前没有可召集的成年平民。";

    public static string BuildQueuedMessage(int civilianCount)
    {
        return "【SETS】正在召集 " + Math.Max(0, civilianCount) + " 名成年平民，完成后将编入3号民众编队。";
    }

    public static bool ShouldHandleExplicitPlayerCommand(string playerText)
    {
        string normalized = Normalize(playerText);
        if (string.IsNullOrEmpty(normalized)
            || Contains(normalized, "不要召集")
            || Contains(normalized, "别召集")
            || Contains(normalized, "无需召集"))
        {
            return false;
        }

        bool namesCivilians = Contains(normalized, "平民")
            || Contains(normalized, "民众")
            || Contains(normalized, "居民")
            || Contains(normalized, "百姓")
            || Contains(normalized, "城里人")
            || Contains(normalized, "大家");
        bool givesGatherOrder = Contains(normalized, "召集")
            || Contains(normalized, "集合")
            || Contains(normalized, "聚集")
            || Contains(normalized, "叫过来")
            || Contains(normalized, "喊过来")
            || Contains(normalized, "通知过来")
            || Contains(normalized, "带过来");
        return namesCivilians && givesGatherOrder;
    }

    public static string BuildGatheredMessage(int civilianCount)
    {
        return "【SETS】已召集 " + Math.Max(0, civilianCount) + " 名成年平民并编入3号民众编队。";
    }

    private static string Normalize(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Replace(" ", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("，", string.Empty)
                .Replace("。", string.Empty)
                .Replace("！", string.Empty)
                .Replace("？", string.Empty)
                .Trim();
    }

    private static bool Contains(string text, string value)
    {
        return text?.IndexOf(value, StringComparison.Ordinal) >= 0;
    }
}
