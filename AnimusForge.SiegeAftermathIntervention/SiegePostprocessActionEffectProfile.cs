namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free trigger sources/details used when GCCZ postprocess action tags mutate aftermath state.
/// AF adapters still own regex matching, live target checks, and Bannerlord side effects.
/// </summary>
public static class SiegePostprocessActionEffectProfile
{
    public const string BlockedMercyTrackActionName = "降级处置";

    public const string GatherCiviliansSource = "ai_tag";

    public const string MercyTriggerSource = "场景对话宽恕";

    public const string MercyTriggerDetail = "玩家通过场景对话选择宽恕普通民众。";

    public const string SoldierMaterialReliefTriggerSource = "士兵分发安抚";

    public const string SoldierMaterialReliefTriggerDetail = "玩家命令己方士兵分发共享物资安抚民众；士兵分发路线最高按安抚结算。";

    public const string CivilianVerbalReliefTriggerSource = "平民对话安抚";

    public const string CivilianVerbalReliefTriggerDetail = "玩家直接通过言语安抚战败民众，使其接受宽恕和秩序安排。";

    public const string ConversationReliefTriggerSource = "场景对话安抚";

    public const string ConversationReliefTriggerDetail = "玩家通过场景对话选择安抚和救济民众。";

    public const string InspirationTriggerSource = "场景对话安民宣抚";

    public const string InspirationTriggerDetail = "玩家通过场景对话召集民众并宣示新秩序，以提高忠诚度并争取本地要人支持。";

    public const string RallyOathTriggerSource = "场景对话归心盟誓";

    public const string RallyOathTriggerDetail = "玩家通过场景对话组织公开盟誓，以强力争取民众归附和要人支持。";

    public const string CivilianRobberyTriggerSource = "场景对话触发抢钱";

    public const string CivilianRobberyGoldTriggerDetail = "玩家通过场景对话向当前平民、商人、头人或要人索取第纳尔，不触发原版掠夺。";

    public const string CivilianRobberyGoodsTriggerDetail = "玩家通过场景对话向当前平民、商人、头人或要人索取货物或物资，不触发原版掠夺。";

    public const string PlunderTriggerSource = "场景对话触发搜掠";

    public const string PlunderTriggerDetail = "玩家在攻城后亲自进城时通过己方士兵对话下令全城搜掠。";

    public const string MassacreTriggerSource = "场景对话触发血洗";

    public const string MassacreTriggerDetail = "NPC回复表明对话谈崩或玩家已明确下令血洗，攻城后处置升级为血洗。";

    public const string MassacreStopTriggerSource = "ai_tag_massacre_stop";

    public const string MassacreStopTriggerDetail = "player_ordered_massacre_stop";

    public const string CulturalRepopulationTriggerSource = "场景对话屠民迁殖";

    public const string CulturalRepopulationTriggerDetail = "玩家通过场景对话要求杀尽原住民并将定居点改为己方文化。";

    public static string GetReliefTriggerSource(bool targetIsCivilian)
    {
        return targetIsCivilian ? CivilianVerbalReliefTriggerSource : ConversationReliefTriggerSource;
    }

    public static string GetReliefTriggerDetail(bool targetIsCivilian)
    {
        return targetIsCivilian ? CivilianVerbalReliefTriggerDetail : ConversationReliefTriggerDetail;
    }
}
