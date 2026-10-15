using System.Text;



namespace AnimusForge.SiegeAftermathIntervention;



/// <summary>

/// Dependency-free runtime wording and action isolation for an active castle aftermath stage.

/// The AF adapter supplies live agent roles, prisoner state, and lord battle provenance.

/// </summary>

public static class SiegeCastleRuntimePromptProfile

{

    public static string DefaultCastleName => TownPromptCatalogResources.Get(false).GetRuntimeText("DefaultCastleName");



    public const string DefaultCastleNameEn = "this newly captured castle";



    public static string DefaultPlayerName => TownPromptCatalogResources.Get(false).GetRuntimeText("DefaultPlayerName");



    public const string DefaultPlayerNameEn = "the player";



    public static string GetDefaultCastleName(bool isEnglish) => isEnglish ? DefaultCastleNameEn : DefaultCastleName;



    public static string GetDefaultPlayerName(bool isEnglish) => isEnglish ? DefaultPlayerNameEn : DefaultPlayerName;



    public static string Build(SiegeCastleRuntimePromptFacts facts, TownPromptTextCatalog textCatalog)
        => Build(facts, textCatalog, false);

    public static string Build(SiegeCastleRuntimePromptFacts facts, TownPromptTextCatalog textCatalog, bool peacefulEntry)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        bool isEnglish = text.IsEnglish;

        facts ??= SiegeCastleRuntimePromptFacts.Empty;



        string castleName = Normalize(facts.CastleName, peacefulEntry ? (isEnglish ? "this friendly castle" : "这座友方城堡") : text.GetRuntimeText("DefaultCastleName"));

        string playerName = Normalize(facts.PlayerName, text.GetRuntimeText("DefaultPlayerName"));

        StringBuilder sb = new StringBuilder();

        if (peacefulEntry)
            sb.Append(TownPromptTextCatalog.FormatTemplate(text.GetRuntimeText("CastleRuntime_PeacefulIntro"), ("settlement", castleName), ("player", playerName)));
        else
        sb.Append(text.GetRuntimeText("CastleRuntime_001"))

            .Append(castleName)

            .Append(text.GetRuntimeText("CastleRuntime_002"))

            .Append(playerName)

            .Append(text.GetRuntimeText("CastleRuntime_003"))

            .Append(text.GetRuntimeText("CastleRuntime_004"))

            .Append(text.GetRuntimeText("CastleRuntime_005"));



        AppendIfNotEmpty(sb, facts.RoleSituationContext);

        AppendIfNotEmpty(sb, facts.MemoryContext);



        if (facts.IsAlliedSoldier)

        {

            sb.Append(text.GetRuntimeText("CastleRuntime_006"));

            if (facts.PendingProposalForSpeaker != SiegeCastlePrisonerDispositionKind.None)

            {

                sb.Append(SiegeCastleSoldierProposalProfile.BuildPendingContext(facts.PendingProposalForSpeaker, isEnglish));

            }

            if (facts.SoldierAppeasementRequired && !facts.SoldierAppeasementApplied)

            {

                sb.Append(text.GetRuntimeText("CastleRuntime_007"))

                    .Append(facts.RecruitedRegularPrisoners)

                    .Append(text.GetRuntimeText("CastleRuntime_008"))

                    .Append(facts.TerminalActionForTarget == SiegeCastleActionKind.Unknown ? text.GetRuntimeText("CastleRuntime_009") : facts.TerminalActionForTarget.ToString())

                    .Append(text.GetRuntimeText("CastleRuntime_010"));

            }

            else if (facts.SoldierAppeasementApplied)

            {

                sb.Append(text.GetRuntimeText("CastleRuntime_011"));

            }

            else

            {

                sb.Append(text.GetRuntimeText("CastleRuntime_012"));

            }

            if (facts.SpeakerCultureMatchesCastle)

            {

                sb.Append(text.GetRuntimeText("CastleRuntime_013"));

            }

        }

        else if (facts.IsPrisoner)

        {

            sb.Append(facts.IsLord

                ? text.GetRuntimeText("CastleRuntime_014")

                : text.GetRuntimeText(peacefulEntry ? "CastleRuntime_PeacefulPrisoner" : "CastleRuntime_015"));

            if (facts.IsLord)

            {

                sb.Append(text.GetRuntimeText("CastleRuntime_016")).Append(facts.SpeakerTrust)

                    .Append(text.GetRuntimeText("CastleRuntime_017")).Append(facts.SpeakerIsClanLeader ? text.GetRuntimeText("CastleRuntime_018") : text.GetRuntimeText("CastleRuntime_019"))

                    .Append(text.GetRuntimeText("CastleRuntime_020")).Append(facts.PlayerHasKingdom ? text.GetRuntimeText("CastleRuntime_021") : text.GetRuntimeText("CastleRuntime_022"))

                    .Append(text.GetRuntimeText("CastleRuntime_023")).Append(facts.PlayerRulesKingdom ? text.GetRuntimeText("CastleRuntime_024") : text.GetRuntimeText("CastleRuntime_025")).Append(text.GetRuntimeText("CastleRuntime_026"));

                sb.Append(text.GetRuntimeText("CastleRuntime_027"))

                    .Append(SiegeCastleLordDuelProfile.BuildEquipmentFact(

                        facts.PlayerIsMounted,

                        facts.PlayerCarriesRangedWeapon,

                        facts.PlayerWieldsRangedWeapon, isEnglish))

                    .Append(text.GetRuntimeText("CastleRuntime_028"));

                if (facts.SpeakerIsClanLeader)

                {

                    sb.Append(facts.PlayerHasKingdom

                        ? (facts.PlayerRulesKingdom

                            ? text.GetRuntimeText("CastleRuntime_029")

                            : text.GetRuntimeText("CastleRuntime_030"))

                        : text.GetRuntimeText("CastleRuntime_031"));

                }

                else

                {

                    sb.Append(text.GetRuntimeText("CastleRuntime_032"));

                }

            }

            else

            {

                sb.Append(text.GetRuntimeText("CastleRuntime_033")).Append(facts.SpeakerTrust)

                    .Append(text.GetRuntimeText("CastleRuntime_034")).Append(SiegeCastlePrisonerTrustProfile.VoluntaryRecruitThreshold)

                    .Append(text.GetRuntimeText("CastleRuntime_035")).Append(SiegeCastlePrisonerTrustProfile.VoluntaryLaborThreshold)

                    .Append(text.GetRuntimeText("CastleRuntime_036")).Append(SiegeCastlePrisonerTrustProfile.VoluntaryInstructorThreshold)

                    .Append(text.GetRuntimeText("CastleRuntime_037"));

            }

            if (facts.TreatedForTarget)

            {

                sb.Append(text.GetRuntimeText("CastleRuntime_038"));

            }

            if (facts.ArmamentsReceivedForTarget)

            {

                sb.Append(text.GetRuntimeText("CastleRuntime_039"));

            }

            if (facts.IsLord && facts.TerminalActionForTarget != SiegeCastleActionKind.Unknown)

            {

                sb.Append(text.GetRuntimeText("CastleRuntime_040")).Append(facts.TerminalActionForTarget)

                    .Append(text.GetRuntimeText("CastleRuntime_041"));

            }

            if (facts.PendingProposalForSpeaker != SiegeCastlePrisonerDispositionKind.None)

            {

                sb.Append(SiegeCastleSoldierProposalProfile.BuildPendingContext(facts.PendingProposalForSpeaker, isEnglish));

            }

        }



        if (!facts.IsLord && !string.IsNullOrWhiteSpace(facts.RegularDispositionPlan))

        {

            sb.Append(text.GetRuntimeText("CastleRuntime_042")).Append(facts.RegularDispositionPlan.Trim())

                .Append(text.GetRuntimeText("CastleRuntime_043"));

            if (facts.RegularDispositionRevisionCount > 0)

            {

                sb.Append(text.GetRuntimeText("CastleRuntime_044"))

                    .Append(facts.RegularDispositionRevisionCount)

                    .Append(text.GetRuntimeText("CastleRuntime_045"));

            }

            if (facts.SlaughteredRegularPrisoners > 0)

            {

                sb.Append(text.GetRuntimeText("CastleRuntime_046")).Append(facts.SlaughteredRegularPrisoners)

                    .Append(text.GetRuntimeText("CastleRuntime_047"));

            }

        }

        sb.Append(text.GetRuntimeText("CastleRuntime_048"))

            .Append(text.GetRuntimeText("CastleRuntime_049"));



        return sb.ToString();


    }



    public static string Build(SiegeCastleRuntimePromptFacts facts)
        => Build(facts, TownPromptCatalogResources.Get(true));

    public static string Build(SiegeCastleRuntimePromptFacts facts, bool isEnglish)
        => Build(facts, TownPromptCatalogResources.Get(isEnglish));





    public static string BuildImmediateReactionIdentityOverride(string castleName, string playerName, bool isAlliedSoldier, bool isPrisoner, bool isLord)
        => BuildImmediateReactionIdentityOverride(castleName, playerName, isAlliedSoldier, isPrisoner, isLord, TownPromptCatalogResources.Get(true));

    public static string BuildImmediateReactionIdentityOverride(string castleName, string playerName, bool isAlliedSoldier, bool isPrisoner, bool isLord, bool isEnglish)
        => BuildImmediateReactionIdentityOverride(castleName, playerName, isAlliedSoldier, isPrisoner, isLord, TownPromptCatalogResources.Get(isEnglish));

    public static string BuildImmediateReactionIdentityOverride(string castleName, string playerName, bool isAlliedSoldier, bool isPrisoner, bool isLord, TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        string role = isAlliedSoldier

            ? text.GetRuntimeText("CastleImmediateRole_001")

            : (isPrisoner

                ? (isLord

                    ? text.GetRuntimeText("CastleImmediateRole_002")

                    : text.GetRuntimeText("CastleImmediateRole_003"))

                : text.GetRuntimeText("CastleImmediateRole_004"));
        return TownPromptTextCatalog.FormatTemplate(text.GetRuntimeText("CastleImmediateTemplate"), ("settlement", Normalize(castleName, text.GetRuntimeText("DefaultCastleName"))), ("player", Normalize(playerName, text.GetRuntimeText("DefaultPlayerName"))), ("role", role));
    }

    public static bool ShouldExposeTownAftermathRules(bool isCastleStage)

    {

        return !isCastleStage;

    }



    private static void AppendIfNotEmpty(StringBuilder sb, string text)

    {

        if (!string.IsNullOrWhiteSpace(text))

        {

            sb.Append(text.Trim());

        }

    }



    private static string Normalize(string value, string fallback)

    {

        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    }

}



public sealed class SiegeCastleRuntimePromptFacts

{

    public SiegeCastleRuntimePromptFacts(

        string castleName,

        string playerName,

        bool isAlliedSoldier,

        bool isPrisoner,

        bool isLord,

        string roleSituationContext,

        string memoryContext,

        int remainingRegularPrisoners = 0,

        int recruitedRegularPrisoners = 0,

        int slaughteredRegularPrisoners = 0,

        bool soldierAppeasementRequired = false,

        bool soldierAppeasementApplied = false,

        bool speakerCultureMatchesCastle = false,

        SiegeCastlePrisonerDispositionKind pendingProposalForSpeaker = SiegeCastlePrisonerDispositionKind.None,

        int speakerTrust = SiegeCastlePrisonerTrustProfile.DefaultDefeatedGarrisonTrust,

        bool treatedForTarget = false,

        bool armamentsReceivedForTarget = false,

        SiegeCastleActionKind terminalActionForTarget = SiegeCastleActionKind.Unknown,

        bool speakerIsClanLeader = false,

        bool playerHasKingdom = false,

        bool playerRulesKingdom = false,

        int regularDispositionRevisionCount = 0,

        string regularDispositionPlan = null,

        bool playerIsMounted = false,

        bool playerCarriesRangedWeapon = false,

        bool playerWieldsRangedWeapon = false)

    {

        CastleName = castleName ?? string.Empty;

        PlayerName = playerName ?? string.Empty;

        IsAlliedSoldier = isAlliedSoldier;

        IsPrisoner = isPrisoner;

        IsLord = isLord;

        RoleSituationContext = roleSituationContext ?? string.Empty;

        MemoryContext = memoryContext ?? string.Empty;

        RemainingRegularPrisoners = remainingRegularPrisoners < 0 ? 0 : remainingRegularPrisoners;

        RecruitedRegularPrisoners = recruitedRegularPrisoners < 0 ? 0 : recruitedRegularPrisoners;

        SlaughteredRegularPrisoners = slaughteredRegularPrisoners < 0 ? 0 : slaughteredRegularPrisoners;

        SoldierAppeasementRequired = soldierAppeasementRequired;

        SoldierAppeasementApplied = soldierAppeasementApplied;

        SpeakerCultureMatchesCastle = speakerCultureMatchesCastle;

        PendingProposalForSpeaker = pendingProposalForSpeaker;

        SpeakerTrust = SiegeCastlePrisonerTrustProfile.Clamp(speakerTrust);

        TreatedForTarget = treatedForTarget;

        ArmamentsReceivedForTarget = armamentsReceivedForTarget;

        TerminalActionForTarget = terminalActionForTarget;

        SpeakerIsClanLeader = speakerIsClanLeader;

        PlayerHasKingdom = playerHasKingdom;

        PlayerRulesKingdom = playerRulesKingdom;

        RegularDispositionRevisionCount = regularDispositionRevisionCount < 0 ? 0 : regularDispositionRevisionCount;

        RegularDispositionPlan = regularDispositionPlan ?? string.Empty;

        PlayerIsMounted = playerIsMounted;

        PlayerCarriesRangedWeapon = playerCarriesRangedWeapon;

        PlayerWieldsRangedWeapon = playerWieldsRangedWeapon;

    }



    public static SiegeCastleRuntimePromptFacts Empty => new SiegeCastleRuntimePromptFacts(

        castleName: string.Empty,

        playerName: string.Empty,

        isAlliedSoldier: false,

        isPrisoner: false,

        isLord: false,

        roleSituationContext: string.Empty,

        memoryContext: string.Empty,

        remainingRegularPrisoners: 0,

        recruitedRegularPrisoners: 0,

        slaughteredRegularPrisoners: 0,

        soldierAppeasementRequired: false,

        soldierAppeasementApplied: false,

        speakerCultureMatchesCastle: false,

        pendingProposalForSpeaker: SiegeCastlePrisonerDispositionKind.None);



    public string CastleName { get; }



    public string PlayerName { get; }



    public bool IsAlliedSoldier { get; }



    public bool IsPrisoner { get; }



    public bool IsLord { get; }



    public string RoleSituationContext { get; }



    public string MemoryContext { get; }



    public int RemainingRegularPrisoners { get; }



    public int RecruitedRegularPrisoners { get; }



    public int SlaughteredRegularPrisoners { get; }



    public bool SoldierAppeasementRequired { get; }



    public bool SoldierAppeasementApplied { get; }



    public bool SpeakerCultureMatchesCastle { get; }



    public SiegeCastlePrisonerDispositionKind PendingProposalForSpeaker { get; }



    public int SpeakerTrust { get; }



    public bool TreatedForTarget { get; }



    public bool ArmamentsReceivedForTarget { get; }



    public SiegeCastleActionKind TerminalActionForTarget { get; }



    public bool SpeakerIsClanLeader { get; }



    public bool PlayerHasKingdom { get; }



    public bool PlayerRulesKingdom { get; }



    public int RegularDispositionRevisionCount { get; }



    public string RegularDispositionPlan { get; }



    public bool PlayerIsMounted { get; }



    public bool PlayerCarriesRangedWeapon { get; }



    public bool PlayerWieldsRangedWeapon { get; }

}

