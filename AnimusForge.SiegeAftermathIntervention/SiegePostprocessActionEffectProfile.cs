namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free trigger sources/details used when GCCZ postprocess action tags mutate aftermath state.
/// AF adapters still own regex matching, live target checks, and Bannerlord side effects.
/// </summary>
public static class SiegePostprocessActionEffectProfile
{
    public const string BlockedMercyTrackActionName = "Demoted disposition";

    public const string GatherCiviliansSource = "ai_tag";

    public const string MercyTriggerSource = "Scene dialogue mercy";

    public const string MercyTriggerDetail = "The player chose mercy toward civilians in scene dialogue.";

    public const string SoldierMaterialReliefTriggerSource = "Soldier distribution reassurance";

    public const string SoldierMaterialReliefTriggerDetail = "The player ordered allied soldiers to distribute shared supplies to civilians.";

    public const string CivilianVerbalReliefTriggerSource = "Civilian dialogue reassurance";

    public const string CivilianVerbalReliefTriggerDetail = "The player verbally reassured defeated civilians to accept mercy and order.";

    public const string ConversationReliefTriggerSource = "Scene dialogue relief";

    public const string ConversationReliefTriggerDetail = "The player chose relief and reassurance in scene dialogue.";

    public const string InspirationTriggerSource = "Scene dialogue public reassurance";

    public const string InspirationTriggerDetail = "The player gathered civilians to declare a new civic order.";

    public const string RallyOathTriggerSource = "Scene dialogue public oath";

    public const string RallyOathTriggerDetail = "The player organized a public oath of allegiance.";

    public const string CivilianRobberyTriggerSource = "Scene dialogue extortion";

    public const string CivilianRobberyGoldTriggerDetail = "The player demanded denars from a civilian, merchant, or notable without vanilla plunder.";

    public const string CivilianRobberyGoodsTriggerDetail = "The player demanded supplies from a civilian, merchant, or notable without vanilla plunder.";

    public const string PlunderTriggerSource = "Scene dialogue plunder";

    public const string PlunderTriggerDetail = "The player ordered full-town plunder through allied soldiers.";

    public const string MassacreTriggerSource = "Scene dialogue massacre";

    public const string MassacreTriggerDetail = "Negotiations collapsed or the player ordered massacre; escalated to massacre.";

    public const string MassacreStopTriggerSource = "ai_tag_massacre_stop";

    public const string MassacreStopTriggerDetail = "player_ordered_massacre_stop";

    public const string CulturalRepopulationTriggerSource = "Scene dialogue colonization";

    public const string CulturalRepopulationTriggerDetail = "The player ordered eradicating native inhabitants and converting town culture.";

    public static string GetReliefTriggerSource(bool targetIsCivilian)
    {
        return targetIsCivilian ? CivilianVerbalReliefTriggerSource : ConversationReliefTriggerSource;
    }

    public static string GetReliefTriggerDetail(bool targetIsCivilian)
    {
        return targetIsCivilian ? CivilianVerbalReliefTriggerDetail : ConversationReliefTriggerDetail;
    }
}
