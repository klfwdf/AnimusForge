namespace AnimusForge.SiegeAftermathIntervention;



/// <summary>

/// Castle-only role facts injected into AF conversations during the active aftermath stage.

/// Bannerlord adapters resolve live agent identity and battle provenance.

/// </summary>

public static class SiegeCastleNpcSituationProfile

{

    public static string DefaultCastleName => TownPromptCatalogResources.Get(false).GetRuntimeText("DefaultNpcCastleName");



    public const string DefaultCastleNameEn = "this castle";



    public static string DefaultPlayerName => TownPromptCatalogResources.Get(false).GetRuntimeText("DefaultPlayerName");



    public const string DefaultPlayerNameEn = "the player";



    public static string BuildAlliedSoldierPrompt(string castleName, string playerName)
    {
        return BuildAlliedSoldierPrompt(castleName, playerName, isEnglish: false);
    }

    public static string BuildAlliedSoldierPrompt(string castleName, string playerName, bool isEnglish)
        => BuildAlliedSoldierPrompt(castleName, playerName, TownPromptCatalogResources.Get(isEnglish));

    public static string BuildAlliedSoldierPrompt(string castleName, string playerName, TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        return TownPromptTextCatalog.FormatTemplate(text.CastleNpcAlliedSoldierTemplate, ("settlement", Normalize(castleName, text.GetRuntimeText("DefaultNpcCastleName"))), ("player", Normalize(playerName, text.GetRuntimeText("DefaultPlayerName"))));
    }

    public static string BuildAlliedArmyCaptainPrompt(string castleName, string playerName)
    {
        return BuildAlliedArmyCaptainPrompt(castleName, playerName, isEnglish: false);
    }

    public static string BuildAlliedArmyCaptainPrompt(string castleName, string playerName, bool isEnglish)
        => BuildAlliedArmyCaptainPrompt(castleName, playerName, TownPromptCatalogResources.Get(isEnglish));

    public static string BuildAlliedArmyCaptainPrompt(string castleName, string playerName, TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        return TownPromptTextCatalog.FormatTemplate(text.CastleNpcAlliedLordTemplate, ("settlement", Normalize(castleName, text.GetRuntimeText("DefaultNpcCastleName"))), ("player", Normalize(playerName, text.GetRuntimeText("DefaultPlayerName"))));
    }

    public static string BuildDefeatedGarrisonPrisonerPrompt(string castleName, string playerName)
    {
        return BuildDefeatedGarrisonPrisonerPrompt(castleName, playerName, isEnglish: false);
    }

    public static string BuildDefeatedGarrisonPrisonerPrompt(string castleName, string playerName, bool isEnglish)
        => BuildDefeatedGarrisonPrisonerPrompt(castleName, playerName, TownPromptCatalogResources.Get(isEnglish));

    public static string BuildDefeatedGarrisonPrisonerPrompt(string castleName, string playerName, TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        return TownPromptTextCatalog.FormatTemplate(text.CastleNpcRegularPrisonerTemplate, ("settlement", Normalize(castleName, text.GetRuntimeText("DefaultNpcCastleName"))), ("player", Normalize(playerName, text.GetRuntimeText("DefaultPlayerName"))));
    }

    public static string BuildCurrentCastleDefeatedLordPrompt(string castleName, string playerName)
    {
        return BuildCurrentCastleDefeatedLordPrompt(castleName, playerName, isEnglish: false);
    }

    public static string BuildCurrentCastleDefeatedLordPrompt(string castleName, string playerName, bool isEnglish)
        => BuildCurrentCastleDefeatedLordPrompt(castleName, playerName, TownPromptCatalogResources.Get(isEnglish));

    public static string BuildCurrentCastleDefeatedLordPrompt(string castleName, string playerName, TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        return TownPromptTextCatalog.FormatTemplate(text.CastleNpcCapturedCastleLordTemplate, ("settlement", Normalize(castleName, text.GetRuntimeText("DefaultNpcCastleName"))), ("player", Normalize(playerName, text.GetRuntimeText("DefaultPlayerName"))));
    }

    public static string BuildPreviouslyPlayerDefeatedLordPrompt(string castleName, string playerName)
    {
        return BuildPreviouslyPlayerDefeatedLordPrompt(castleName, playerName, isEnglish: false);
    }

    public static string BuildPreviouslyPlayerDefeatedLordPrompt(string castleName, string playerName, bool isEnglish)
        => BuildPreviouslyPlayerDefeatedLordPrompt(castleName, playerName, TownPromptCatalogResources.Get(isEnglish));

    public static string BuildPreviouslyPlayerDefeatedLordPrompt(string castleName, string playerName, TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        return TownPromptTextCatalog.FormatTemplate(text.CastleNpcPriorDefeatedNobleTemplate, ("settlement", Normalize(castleName, text.GetRuntimeText("DefaultNpcCastleName"))), ("player", Normalize(playerName, text.GetRuntimeText("DefaultPlayerName"))));
    }

    public static string BuildPreviouslyCapturedLordPrompt(string castleName, string playerName)
    {
        return BuildPreviouslyCapturedLordPrompt(castleName, playerName, isEnglish: false);
    }

    public static string BuildPreviouslyCapturedLordPrompt(string castleName, string playerName, bool isEnglish)
        => BuildPreviouslyCapturedLordPrompt(castleName, playerName, TownPromptCatalogResources.Get(isEnglish));

    public static string BuildPreviouslyCapturedLordPrompt(string castleName, string playerName, TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        return TownPromptTextCatalog.FormatTemplate(text.CastleNpcFallbackNobleTemplate, ("settlement", Normalize(castleName, text.GetRuntimeText("DefaultNpcCastleName"))), ("player", Normalize(playerName, text.GetRuntimeText("DefaultPlayerName"))));
    }

    private static string Normalize(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
