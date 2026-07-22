namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free SETS policy for an AI-proposed or player-ordered massacre inside an owned or ruler-attached town.
/// AF/SETS adapters still own live mission combat, agent selection, team changes, and shout dispatch.
/// </summary>
public static class SetsOwnedSettlementMassacreProfile
{
    public const int MaxAlliedAttackers = 100;

    public const string RequestActionTag = "[ACTION:SETS_REQUEST_MASSACRE]";

    public const string StartActionTag = "[ACTION:SETS_START_MASSACRE]";

    public const string StopActionTag = "[ACTION:SETS_STOP_MASSACRE]";

    public const string CancelRequestActionTag = "[ACTION:SETS_CANCEL_MASSACRE_REQUEST]";

    public const string RequestSource = "sets_owned_town_ai_massacre_request";

    public const string StartSource = "sets_owned_town_shout_massacre_start";

    public const string StopSource = "sets_owned_town_shout_massacre_stop";

    public const string CancelRequestSource = "sets_owned_town_massacre_request_cancel";

    public const uint PendingMessageColor = 0xFFFFD27Fu;

    public const uint StartMessageColor = 0xFFFF7777u;

    public const uint StopMessageColor = 0xFFB6F7A8u;

    public static bool ShouldOfferRequestRule(bool sceneActive, bool massacreActive, bool anyRequestPending, bool speakerIsSelectedFollower)
    {
        return sceneActive && !massacreActive && !anyRequestPending && speakerIsSelectedFollower;
    }

    public static bool ShouldOfferStartRule(bool sceneActive, bool massacreActive, bool speakerIsSelectedFollower)
    {
        return sceneActive && !massacreActive && speakerIsSelectedFollower;
    }

    public static bool ShouldOfferCancelRequestRule(bool sceneActive, bool massacreActive, bool requestPendingForSpeaker, bool speakerIsSelectedFollower)
    {
        return sceneActive && !massacreActive && requestPendingForSpeaker && speakerIsSelectedFollower;
    }

    public static bool ShouldOfferStopRule(bool sceneActive, bool massacreActive, bool speakerIsSelectedFollower)
    {
        return sceneActive && massacreActive && speakerIsSelectedFollower;
    }

    public static string BuildRuntimeInstruction(bool massacreActive, bool requestPending)
    {
        if (massacreActive)
        {
            return "【SETS血洗进行中】玩家带入城内的随行士兵正在攻击本城居民。只有随行士兵直接听到玩家本轮明确命令停止、住手、收兵或停止杀戮，并在回复中明确接受命令时，后处理才可输出 "
                + StopActionTag
                + "。士兵闲聊、平民求饶、旁听者呼喊或NPC自行建议停手都不能触发；停手只终止后续攻击，不复活死者，也不抹除已经发生的内部事件。";
        }

        if (requestPending)
        {
            return "【SETS血洗请求待确认】已有一名随行士兵由AI结合当下局势自行判断并向玩家请求是否血洗本城，但尚未执行。若玩家本轮明确同意该提议，原提议士兵直接回应并接受执行时，后处理输出 "
                + StartActionTag
                + "；若玩家明确拒绝、制止或取消该提议，原提议士兵直接回应时输出 "
                + CancelRequestActionTag
                + "。玩家若改为直接下达血洗命令，任一随行士兵接受后也直接输出开始标签，不再二次请示。提问、沉默、模糊态度、旁听者意见均不改变待确认状态。";
        }

        return "【SETS自有/附属城镇血洗决策】玩家选入场景的随行士兵可由AI结合人物性格、当前局势和对话自行判断是否建议血洗；只有士兵正文明确向玩家请求许可、等待玩家决定且尚未声称已经执行时，后处理才可输出 "
            + RequestActionTag
            + "，该标签只登记请求，绝不开始攻击。若玩家本轮已经直接明确命令全体攻击、血洗或屠杀本城居民，随行士兵在回复中明确接受执行时，后处理直接输出 "
            + StartActionTag
            + "，无需再请求确认。平民、城镇守卫、旁听者和非随行人员不能输出这些标签；讨论、威胁、请示但未形成明确请求或命令时不输出。开始后最多100名随行士兵持现有武器执行，玩家仍可明确喊停。";
    }

    public static string BuildRequestRuleDescription()
    {
        return "SETS自有/统治者附属城镇：当前说话者必须是玩家选入场景的随行士兵；当该士兵由AI结合性格、局势和对话自行决定建议血洗，并在本轮正文中明确向玩家请求是否执行、等待许可且没有声称已经动手时输出。该请求只登记待确认状态，不执行攻击。玩家已经直接下令时不得输出请求标签，应直接判断开始标签。";
    }

    public static string BuildStartRuleDescription(bool requestPendingForSpeaker)
    {
        return requestPendingForSpeaker
            ? "SETS血洗请求待确认：当前说话者是原提议随行士兵，玩家本轮明确同意此前血洗请求且士兵接受执行时输出；或者玩家本轮直接明确下令血洗且士兵接受时也直接输出。模糊回应、追问、拒绝、旁听和NPC自行重复提议均不输出。"
            : "SETS自有/统治者附属城镇：仅当前说话者是玩家选入场景的随行士兵，且直接回应玩家本轮明确下令全体攻击、血洗或屠杀本城居民并表示服从时输出。玩家直接下令后不再二次请示；讨论、威胁、士兵主动建议及任何非随行士兵回复均不输出开始标签。";
    }

    public static string BuildCancelRequestRuleDescription()
    {
        return "仅当前说话者是此前提出血洗请求的随行士兵，且玩家本轮明确拒绝、制止或取消该请求，士兵回应确认不执行时输出。模糊态度、追问、旁听者反对或士兵自行撤回均不输出。";
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

    public static string BuildPendingRequestMessage(string speakerName)
    {
        string resolvedName = string.IsNullOrWhiteSpace(speakerName) ? "一名随行士兵" : speakerName.Trim();
        return "【SETS】" + resolvedName + "请求你决定是否血洗本城；尚未执行，请明确同意或拒绝。";
    }

    public static string BuildCancelledRequestMessage()
    {
        return "【SETS】血洗请求已被否决，随行士兵不会执行攻击。";
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
