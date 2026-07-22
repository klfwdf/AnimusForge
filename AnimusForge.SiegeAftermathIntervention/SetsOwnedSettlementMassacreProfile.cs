namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free SETS policy for a player-ordered massacre inside an owned or ruler-attached town.
/// AF/SETS adapters still own live mission combat, agent selection, team changes, and shout dispatch.
/// </summary>
public static class SetsOwnedSettlementMassacreProfile
{
    public const int MaxAlliedAttackers = 100;

    public const string StartActionTag = "[ACTION:SETS_START_MASSACRE]";

    public const string StopActionTag = "[ACTION:SETS_STOP_MASSACRE]";

    public const string StartSource = "sets_owned_town_shout_massacre_start";

    public const string StopSource = "sets_owned_town_shout_massacre_stop";

    public const uint StartMessageColor = 0xFFFF7777u;

    public const uint StopMessageColor = 0xFFB6F7A8u;

    public static bool ShouldOfferStartRule(bool sceneActive, bool massacreActive, bool speakerIsSelectedFollower)
    {
        return sceneActive && !massacreActive && speakerIsSelectedFollower;
    }

    public static bool ShouldOfferStopRule(bool sceneActive, bool massacreActive, bool speakerIsSelectedFollower)
    {
        return sceneActive && massacreActive && speakerIsSelectedFollower;
    }

    public static string BuildRuntimeInstruction(bool massacreActive)
    {
        if (massacreActive)
        {
            return "【SETS血洗进行中】玩家带入城内的随行士兵正在攻击本城居民。只有随行士兵直接听到玩家本轮明确命令停止、住手、收兵或停止杀戮，并在回复中明确接受命令时，后处理才可输出 "
                + StopActionTag
                + "。士兵闲聊、平民求饶、旁听者呼喊或NPC自行建议停手都不能触发；停手只终止后续攻击，不复活死者，也不抹除已经发生的内部事件。";
        }

        return "【SETS自有/附属城镇血洗命令】只有玩家选入场景的随行士兵直接听到玩家本轮明确命令全体攻击、血洗或屠杀城内居民，并在回复中明确接受执行时，后处理才可输出 "
            + StartActionTag
            + "。平民、城镇守卫、旁听者、NPC闲聊、士兵主动建议或玩家只讨论/威胁但未下令时都不能触发。触发后最多100名随行士兵持现有武器执行；玩家之后仍可通过明确喊停终止继续攻击。";
    }

    public static string BuildStartRuleDescription()
    {
        return "SETS自有/统治者附属城镇：仅当前说话者是玩家选入场景的随行士兵，且直接回应玩家本轮明确下令全体攻击、血洗或屠杀本城居民并表示服从时输出。讨论、威胁、请示、士兵主动建议及任何非随行士兵回复均不输出。";
    }

    public static string BuildStopRuleDescription()
    {
        return "仅SETS血洗正在进行时：当前说话者必须是玩家选入场景的随行士兵，并直接回应玩家本轮明确要求停止、住手、收兵或停止杀戮且表示执行时输出。求饶、旁听、NPC自行提议或模糊抱怨均不输出。";
    }

    public static string BuildStartedMessage(int attackerCount, int targetCount)
    {
        int attackers = attackerCount < 0 ? 0 : attackerCount;
        int targets = targetCount < 0 ? 0 : targetCount;
        return "【SETS】血洗命令已下达：" + attackers + " 名随行士兵开始持械攻击 " + targets + " 名城内目标；你仍可通过喊话命令停手。";
    }

    public static string BuildStoppedMessage(int survivingTargetCount)
    {
        int survivors = survivingTargetCount < 0 ? 0 : survivingTargetCount;
        return "【SETS】随行士兵已经停手；" + survivors + " 名幸存者不再被追杀，但既有死伤与内部事件仍会保留。";
    }

    public static string BuildCompletedMessage()
    {
        return "【SETS】现场已无可继续攻击的城内目标；本次血洗行动结束。";
    }
}
