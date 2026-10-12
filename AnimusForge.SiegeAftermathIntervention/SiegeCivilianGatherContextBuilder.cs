namespace AnimusForge.SiegeAftermathIntervention;

public static class SiegeCivilianGatherContextBuilder
{
    public static string Build(SiegeCivilianGatherContextFacts facts)
        => Build(facts, false);

    public static string Build(SiegeCivilianGatherContextFacts facts, bool isEnglish)
        => Build(facts, TownPromptCatalogResources.Get(isEnglish));

    public static string Build(SiegeCivilianGatherContextFacts facts, TownPromptTextCatalog textCatalog)
    {

        if (facts == null)
        {
            return string.Empty;
        }

        if (!facts.SpeechRallyActive
            && !facts.GatherPropagationActive
            && !facts.FormationControlPending
            && !facts.FormationControlComplete
            && facts.FollowerCount <= 0
            && facts.ReadyFormationCount <= 0)
        {
            return string.Empty;
        }

        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        return TownPromptTextCatalog.FormatTemplate(text.CivilianGatherContextTemplate, ("stage", DescribeStage(facts, text)), ("count", facts.FollowerCount.ToString()), ("ready", facts.ReadyFormationCount.ToString()), ("messenger", facts.MessengerCount.ToString()), ("available", facts.TotalCivilianCount.ToString()));
    }

    public static string DescribeStage(SiegeCivilianGatherContextFacts facts)
        => DescribeStage(facts, false);

    public static string DescribeStage(SiegeCivilianGatherContextFacts facts, bool isEnglish)
        => DescribeStage(facts, TownPromptCatalogResources.Get(isEnglish));

    public static string DescribeStage(SiegeCivilianGatherContextFacts facts, TownPromptTextCatalog textCatalog)
    {
        if (facts == null) return string.Empty;
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        return text.GetRuntimeText(facts.FormationControlComplete ? "GatherStageComplete" : (facts.FormationControlPending ? "GatherStagePending" : (facts.GatherPropagationActive ? "GatherStagePropagation" : "GatherStageRally")));
    }
}
