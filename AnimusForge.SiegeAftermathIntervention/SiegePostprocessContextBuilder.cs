namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free formatter for active GCCZ postprocess runtime facts.
/// AF adapters gather live objects; this core owns the prompt text structure.
/// </summary>
public static class SiegePostprocessContextBuilder
{
    public const string DefaultSettlementName = "the captured settlement";

    public const string DefaultSpeakerName = "NPC";

    public const string AlliedSoldierSpeakerIdentity = "Allied soldier inside the town";

    public const string CivilianSpeakerIdentity = "Defeated settlement civilian/merchant/artisan";

    public const string DefaultSpeakerIdentity = "Other scene NPC";

    public static string SelectSpeakerIdentity(bool isAlliedSoldier, bool isCivilian)
    {
        if (isAlliedSoldier)
        {
            return AlliedSoldierSpeakerIdentity;
        }

        return isCivilian ? CivilianSpeakerIdentity : DefaultSpeakerIdentity;
    }

    public static string Build(SiegePostprocessContextFacts facts)
    {
        return Build(facts, TownPromptTextCatalog.CreateEnglishFallback());
    }

    public static string Build(
        SiegePostprocessContextFacts facts,
        TownPromptTextCatalog textCatalog)
    {
        return TownPromptComposer.BuildPostprocessContext(facts, textCatalog);
    }
}
