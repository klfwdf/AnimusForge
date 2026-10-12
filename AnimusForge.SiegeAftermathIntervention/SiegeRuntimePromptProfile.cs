namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free runtime prompt wording for the active GCCZ post-siege intervention scene.
/// AF adapters still resolve live Bannerlord agents, occupations, mission state, memory, and gather context.
/// </summary>
public static class SiegeRuntimePromptProfile
{
    public static string DefaultSettlementName => TownPromptCatalogResources.Get(false).GetRuntimeText("DefaultSettlementName");
    public const string DefaultSettlementNameEn = "this newly captured settlement";

    public static string Build(SiegeRuntimePromptFacts facts)
    {
        return Build(facts, TownPromptTextCatalog.CreateEnglishFallback());
    }

    public static string Build(
        SiegeRuntimePromptFacts facts,
        TownPromptTextCatalog textCatalog)
    {
        return TownPromptComposer.BuildMainPrompt(facts, textCatalog);
    }

    public static string BuildPlayerCommanderContext(string playerName, bool alliedSoldier, bool civilian)
        => BuildPlayerCommanderContext(playerName, alliedSoldier, civilian, false);

    public static string BuildPlayerCommanderContext(string playerName, bool alliedSoldier, bool civilian, bool isEnglish)
        => BuildPlayerCommanderContext(playerName, alliedSoldier, civilian, TownPromptCatalogResources.Get(isEnglish));

    public static string BuildPlayerCommanderContext(string playerName, bool alliedSoldier, bool civilian, TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        string template = alliedSoldier ? text.CommanderContextTemplate : (civilian ? text.MilitaryDisposerContextTemplate : text.GeneralDisposerContextTemplate);
        return TownPromptTextCatalog.FormatTemplate(template, ("player", NormalizePlayerName(playerName, text)));
    }

    public static string BuildImmediateReactionIdentityOverride(string playerName, bool alliedSoldier, bool civilian)
        => BuildImmediateReactionIdentityOverride(playerName, alliedSoldier, civilian, false);

    public static string BuildImmediateReactionIdentityOverride(string playerName, bool alliedSoldier, bool civilian, bool isEnglish)
        => BuildImmediateReactionIdentityOverride(playerName, alliedSoldier, civilian, TownPromptCatalogResources.Get(isEnglish));

    public static string BuildImmediateReactionIdentityOverride(string playerName, bool alliedSoldier, bool civilian, TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        string role = alliedSoldier ? text.ImmediateReactionRoleAlliedSoldier : (civilian ? text.ImmediateReactionRoleCivilian : text.ImmediateReactionRoleDefault);
        return TownPromptTextCatalog.FormatTemplate(text.ImmediateReactionOverrideTemplate, ("player", NormalizePlayerName(playerName, text)), ("speaker_identity", role));
    }

    public static string BuildCompactImmediateReactionIdentityOverride(
        string playerName,
        bool alliedSoldier,
        bool civilian,
        TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        string normalizedPlayerName = NormalizePlayerName(playerName, text);
        string speakerIdentity = alliedSoldier
            ? text.CompactAmbientAlliedSoldierIdentity
            : (civilian
                ? text.CompactAmbientCivilianIdentity
                : text.CompactAmbientOtherIdentity);
        return TownPromptTextCatalog.FormatTemplate(text.CompactAmbientIdentityTemplate, ("player", normalizedPlayerName), ("speaker_identity", speakerIdentity ?? string.Empty))
            .Trim();
    }

    private static string NormalizePlayerName(string playerName, TownPromptTextCatalog text)
    {
        return string.IsNullOrWhiteSpace(playerName) ? text.GetRuntimeText("DefaultPlayerName") : playerName.Trim();
    }
}

public sealed class SiegeRuntimePromptFacts
{
    public SiegeRuntimePromptFacts(
        string settlementName,
        TownDialogueRole dialogueRole,
        bool isAlliedSoldier,
        bool isGuardOrSoldier,
        bool isCivilian,
        bool soldierAppeasementRequired,
        bool soldierAppeasementApplied,
        string gatherContext,
        string memoryContext,
        string sharedReliefPoolDescription,
        bool plunderStarted,
        bool massacreStarted)
    {
        SettlementName = settlementName ?? string.Empty;
        DialogueRole = TownDialogueRoleClassifier.NormalizeForRuntime(dialogueRole);
        IsAlliedSoldier = isAlliedSoldier;
        IsGuardOrSoldier = isGuardOrSoldier;
        IsCivilian = isCivilian;
        SoldierAppeasementRequired = soldierAppeasementRequired;
        SoldierAppeasementApplied = soldierAppeasementApplied;
        GatherContext = gatherContext ?? string.Empty;
        MemoryContext = memoryContext ?? string.Empty;
        SharedReliefPoolDescription = sharedReliefPoolDescription ?? string.Empty;
        PlunderStarted = plunderStarted;
        MassacreStarted = massacreStarted;
    }

    public static SiegeRuntimePromptFacts Empty
    {
        get
        {
            return new SiegeRuntimePromptFacts(
                settlementName: string.Empty,
                dialogueRole: TownDialogueRoleClassifier.SafeFallbackRole,
                isAlliedSoldier: false,
                isGuardOrSoldier: false,
                isCivilian: true,
                soldierAppeasementRequired: false,
                soldierAppeasementApplied: false,
                gatherContext: string.Empty,
                memoryContext: string.Empty,
                sharedReliefPoolDescription: string.Empty,
                plunderStarted: false,
                massacreStarted: false);
        }
    }

    public string SettlementName { get; }

    public TownDialogueRole DialogueRole { get; }

    public bool IsAlliedSoldier { get; }

    public bool IsGuardOrSoldier { get; }

    public bool IsCivilian { get; }

    public bool SoldierAppeasementRequired { get; }

    public bool SoldierAppeasementApplied { get; }

    public string GatherContext { get; }

    public string MemoryContext { get; }

    public string SharedReliefPoolDescription { get; }

    public bool PlunderStarted { get; }

    public bool MassacreStarted { get; }
}
