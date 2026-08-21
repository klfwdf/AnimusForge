namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free formatter for active GCCZ postprocess runtime facts.
/// AF adapters gather live objects; this core owns the prompt text structure.
/// </summary>
public static class SiegePostprocessContextBuilder
{
    public const string DefaultSettlementName = "刚被攻下的定居点";

    public const string DefaultSpeakerName = "NPC";

    public const string AlliedSoldierSpeakerIdentity = "玩家己方入城士兵";

    public const string CivilianSpeakerIdentity = "战败定居点普通民众/商人/工匠";

    public const string DefaultSpeakerIdentity = "其他场景NPC";

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
