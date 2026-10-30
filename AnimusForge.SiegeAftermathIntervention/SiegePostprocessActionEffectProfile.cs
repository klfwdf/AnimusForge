namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free trigger sources/details used when GCCZ postprocess action tags mutate aftermath state.
/// AF adapters still own regex matching, live target checks, and Bannerlord side effects.
/// </summary>
public static class SiegePostprocessActionEffectProfile
{
    public const string BlockedMercyTrackActionName = "Demoted disposition";

    public const string GatherCiviliansSource = "ai_tag";

    public static readonly string MercyTriggerSource = SiegeAftermathCompatibilityTexts.Raw("trigger_mercy_source");

    public static readonly string MercyTriggerDetail = SiegeAftermathCompatibilityTexts.Raw("trigger_mercy_detail");

    public static readonly string SoldierMaterialReliefTriggerSource = SiegeAftermathCompatibilityTexts.Raw("trigger_soldier_relief_source");

    public static readonly string SoldierMaterialReliefTriggerDetail = SiegeAftermathCompatibilityTexts.Raw("trigger_soldier_relief_detail");

    public static readonly string CivilianVerbalReliefTriggerSource = SiegeAftermathCompatibilityTexts.Raw("trigger_civilian_relief_source");

    public static readonly string CivilianVerbalReliefTriggerDetail = SiegeAftermathCompatibilityTexts.Raw("trigger_civilian_relief_detail");

    public static readonly string ConversationReliefTriggerSource = SiegeAftermathCompatibilityTexts.Raw("trigger_conversation_relief_source");

    public static readonly string ConversationReliefTriggerDetail = SiegeAftermathCompatibilityTexts.Raw("trigger_conversation_relief_detail");

    public static readonly string InspirationTriggerSource = SiegeAftermathCompatibilityTexts.Raw("trigger_inspiration_source");

    public static readonly string InspirationTriggerDetail = SiegeAftermathCompatibilityTexts.Raw("trigger_inspiration_detail");

    public static readonly string RallyOathTriggerSource = SiegeAftermathCompatibilityTexts.Raw("trigger_rally_oath_source");

    public static readonly string RallyOathTriggerDetail = SiegeAftermathCompatibilityTexts.Raw("trigger_rally_oath_detail");

    public static readonly string CivilianRobberyTriggerSource = SiegeAftermathCompatibilityTexts.Raw("trigger_robbery_source");

    public static readonly string CivilianRobberyGoldTriggerDetail = SiegeAftermathCompatibilityTexts.Raw("trigger_robbery_gold_detail");

    public static readonly string CivilianRobberyGoodsTriggerDetail = SiegeAftermathCompatibilityTexts.Raw("trigger_robbery_goods_detail");

    public static readonly string PlunderTriggerSource = SiegeAftermathCompatibilityTexts.Raw("trigger_plunder_source");

    public static readonly string PlunderTriggerDetail = SiegeAftermathCompatibilityTexts.Raw("trigger_plunder_detail");

    public static readonly string MassacreTriggerSource = SiegeAftermathCompatibilityTexts.Raw("trigger_massacre_source");

    public static readonly string MassacreTriggerDetail = SiegeAftermathCompatibilityTexts.Raw("trigger_massacre_detail");

    public const string MassacreStopTriggerSource = "ai_tag_massacre_stop";

    public const string MassacreStopTriggerDetail = "player_ordered_massacre_stop";

    public static readonly string CulturalRepopulationTriggerSource = SiegeAftermathCompatibilityTexts.Raw("trigger_repopulation_source");

    public static readonly string CulturalRepopulationTriggerDetail = SiegeAftermathCompatibilityTexts.Raw("trigger_repopulation_detail");

    public static string GetReliefTriggerSource(bool targetIsCivilian)
    {
        return targetIsCivilian ? CivilianVerbalReliefTriggerSource : ConversationReliefTriggerSource;
    }

    public static string GetReliefTriggerDetail(bool targetIsCivilian)
    {
        return targetIsCivilian ? CivilianVerbalReliefTriggerDetail : ConversationReliefTriggerDetail;
    }
}
