namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy for ordering inspected soldiers to kill the regular
/// prisoners brought into an AF troop-inspection mission.
/// AF owns semantic postprocessing and Bannerlord owns the live mission fight.
/// </summary>
public static class TroopInspectionPrisonerSlaughterProfile
{
    public const string ActionTag = "[ACTION:TROOP_INSPECTION_SLAUGHTER_PRISONERS]";

    public const string StartSource = "troop_inspection_ai_prisoner_slaughter";

    public const uint StartMessageColor = 0xFFFF7777u;

    public const uint CompletionMessageColor = 0xFFB6F7A8u;

    public const uint WarningMessageColor = 0xFFFFD27Fu;

    public static bool ShouldOfferRule(
        bool inspectionActive,
        bool externalCastleRuntime,
        bool slaughterActive,
        bool speakerIsInspectedRegularSoldier,
        int regularPrisonerCount,
        int attackerCount)
    {
        return inspectionActive
            && !externalCastleRuntime
            && !slaughterActive
            && speakerIsInspectedRegularSoldier
            && regularPrisonerCount > 0
            && attackerCount > 0;
    }

    public static string BuildRuntimeInstruction(int regularPrisonerCount, int attackerCount)
    {
        return "【检阅士兵·普通俘虏屠戮】当前是AF检阅士兵场景，说话者是玩家本轮带入检阅的普通士兵；现场有 "
            + ClampCount(regularPrisonerCount)
            + " 名普通士兵俘虏和 "
            + ClampCount(attackerCount)
            + " 名可执行命令的检阅士兵。玩家可以使用任何自然语言直接命令士兵杀死、处死或屠戮现场普通俘虏；不要依赖固定词、关键词匹配或固定句式，而要由AI结合玩家本轮完整意思和士兵的直接回复作语义判断。只有玩家本轮确实下达立即执行的明确命令，并且当前士兵在正文中明确接受、服从或开始执行时，后处理才输出 "
            + ActionTag
            + "。询问意见、讨论可能性、假设、否定、撤回、威胁但未下令、NPC主动建议、旁听者发言或含糊答复都不得输出。该标签只针对普通士兵俘虏，永远不包含被俘领主；实际死亡后才从俘虏名册扣除。";
    }

    public static string BuildPostprocessRuleDescription(int regularPrisonerCount, int attackerCount)
    {
        return "AF检阅士兵场景专用：说话者必须是玩家本轮带入检阅的普通士兵，现场必须仍有普通士兵俘虏。由AI根据玩家本轮完整话语与该士兵的直接回复进行语义判断，不得使用固定关键词门槛。仅当玩家明确命令立即屠戮/处死现场普通俘虏，且士兵明确接受执行时输出。询问、讨论、假设、否定、撤回、NPC主动建议、旁听或未明确服从均不输出。当前普通俘虏 "
            + ClampCount(regularPrisonerCount)
            + " 名，可执行士兵 "
            + ClampCount(attackerCount)
            + " 名；被俘领主不属于此标签。";
    }

    public static string BuildStartedMessage(int attackerCount, int targetCount)
    {
        return "【检阅士兵】屠戮命令已下达："
            + ClampCount(attackerCount)
            + " 名士兵开始持械攻击 "
            + ClampCount(targetCount)
            + " 名普通俘虏；只有场景内实际死亡者才会从俘虏名册扣除。";
    }

    public static string BuildCompletedMessage(int killedCount)
    {
        return "【检阅士兵】屠戮结束："
            + ClampCount(killedCount)
            + " 名普通俘虏已在场景内实际死亡并从俘虏名册扣除；被俘领主未受影响。";
    }

    public static string BuildUnavailableMessage(string reason)
    {
        switch ((reason ?? string.Empty).Trim())
        {
            case "not_normal_inspection":
                return "【检阅士兵】当前不属于普通检阅场景，屠戮命令未执行。";
            case "speaker_not_inspected_soldier":
                return "【检阅士兵】当前回应者不是可执行命令的检阅士兵，屠戮命令未执行。";
            case "no_regular_prisoners":
                return "【检阅士兵】现场没有可处置的普通士兵俘虏；被俘领主不会由此命令处理。";
            case "slaughter_already_active":
                return "【检阅士兵】士兵已经在执行屠戮命令。";
            case "native_fight_unavailable":
                return "【检阅士兵】原版场景战斗控制器当前不可用，屠戮命令未执行；详情已写入日志。";
            default:
                return "【检阅士兵】未能执行屠戮命令；详情已写入日志。";
        }
    }

    private static int ClampCount(int value)
    {
        return value < 0 ? 0 : value;
    }
}
