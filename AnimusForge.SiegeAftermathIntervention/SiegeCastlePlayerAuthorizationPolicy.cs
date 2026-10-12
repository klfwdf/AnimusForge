using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Conservative hard gate for player authorization of ordinary-prisoner outcomes.
/// A generic approval such as "\u540c\u610f" is valid only when the same speaker has a matching pending proposal.
/// </summary>
public static class SiegeCastlePlayerAuthorizationPolicy
{
    private static readonly string[] GlobalRejectionTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_0c04b3a5f184"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_136de7a8c46f"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_2cd0f3be8738"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_6b7589029582"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_84ffb3e8bb88"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_b9572c22c138"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_089e6ed3f55f"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_aaf6f7c76d35"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_5ae8ebc1813b"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_9ba359e542ee"),
        "disagree", "reject", "cancel", "denied", "not approved"
    };

    private static readonly string[] RecruitTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_fa2406490d10"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_bd79df93ce2a"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_21c8736915c1"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_1df9e9ce831e"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_f6607a60405f"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_39f68a15b539"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_c935014af1a4"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_26618ab7e208"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_fec6aa702236"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_8c6e9d12bdf9"),
        "recruit", "enlist", "join my army", "join us", "take them in"
    };

    private static readonly string[] RecruitNegationTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_7505814d6080"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_7ca1fdc2d72d"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_3cfeb71e1aff"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_02dfee1b95cb"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_163e48c5155b"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_e24fd1a48daf"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_ab415bfa5bd4"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_6557e5c32cef"),
        "don't recruit", "do not recruit", "not recruit", "train recruits", "do not train", "don't train", "don’t train"
    };

    private static readonly string[] SlaughterTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_5bfb7629ee9b"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_6d6d9ee1cf41"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_32b4f0184ed6"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_e6acfbbdd612"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_a781b76ad955"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_4c3e4e1ad30a"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_3c1b1002d16e"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_e9cd1c8d3cc6"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_1f66dfcfa604"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_a9c8b238c5bd"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_6839aa3ee227"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_bdb3184920ae"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_ae1d4c7667c4"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_45263470e39d"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_b44355efea26"),
        "slaughter", "execute them", "kill them", "put them to death"
    };

    private static readonly string[] SlaughterNegationTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_b95f18f9c0e9"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_4d713ad49839"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_d5b6fced3272"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_bf3e8d9ba09c"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_9f59475ee9f5"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_68cfbd796b19"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_ab874d50fb60"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_1fc417a739d4"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_a456126f3266"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_e1ed24d97b31"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_82ed29d133e0"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_f058ccf4c920"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_e0bd1475b277"),
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_d9f31ee0b05c"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_87cab6396d3b"), "spare them", "don't kill", "do not kill", "not kill", "do not slaughter", "don't slaughter", "don’t slaughter", "do not execute", "don't execute", "don’t execute", CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_2416b5bfdd20")
    };

    private static readonly string[] ReleaseTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_cc3d94550a8c"), CastleAftermathCompatibilityTexts.Raw("effect_release"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_a30330eb0b46"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_3058239c1b74"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_1e665fe1b7db"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_299b29a2d7a3"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_0c86bd367a56"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_3adf7f3c9d89"),
        "release", "set free", "let them go", "let you go"
    };

    private static readonly string[] ReleaseNegationTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_3ab0cf8694cc"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_f27753663f32"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_686d200a6aaf"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_5f5eb234012d"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_25c1762b0266"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_63b0c21ddf9b"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_da04e6c040a1"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_31103a9e0a13"),
        "don't release", "do not release", "not release"
    };

    private static readonly string[] SellTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_a87341d3be81"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_2f54671d6855"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_f10ff6d46053"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_ad43709c38b7"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_3f89e5fed4d2"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_274e4739719e"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_4e70f08a3280"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_06d48a57ea68"),
        "sell the prisoners", "ransom them", "slave market"
    };

    private static readonly string[] SellNegationTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_90ea6e209372"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_64774539dbc2"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_0ed437bb02a5"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_8b1f405d65d1"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_a4ba83390884"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_218cba5a87e1"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_b967092e6781"),
        "don't sell", "do not sell", "not sell"
    };

    private static readonly string[] GenericLaborTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_f00fe9b70f75"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_a1b9e7dc436a"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_91c983a914e4"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_929ba8d568d5"),
        "forced labor", "work sentence", "labor service"
    };

    private static readonly string[] ExternalLaborTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_7ed27f5dbaa3"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_5711e8e1dd3f"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_35745f9d56c8"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_faa7d6a291ad"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_13645b6b7d0d"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_3a290ee48782"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_837c05d538a0"),
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_f1e438c27149"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_c37aa5722b8f"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_f973a531673b"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_601fa4a8007e"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_e4129a9784b6"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_edcef338753c"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_8c799a9ccf36"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_6f6269eb2c38"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_6775846d1067"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_96a13e760570"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_bfb8273f7ab9"),
        "repair the roads", "send to the villages", "serf"
    };

    private static readonly string[] RepairCastleTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_cf422c9de3bf"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_1871961c11d8"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_8574cc532eb9"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_ac3a05003816"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_ecdd6e92384f"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_faf87d99eac7"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_504105c8a69b"),
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_3d2e9d864679"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_722a6a0dbdb4"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_6128bcd33922"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_a8530672dc55"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_30a30ac0ed59"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_2631448afae6"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_1fca0b2db3bc"),
        "repair the castle", "repair the walls", "rebuild the fortifications"
    };

    private static readonly string[] LaborNegationTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_a648055098db"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_1b5619eb5a0d"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_c0d78481b482"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_2494612c5204"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_bc56761a48ce"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_5f698882d970"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_2c74f68d9426"),
        "no forced labor", "do not use forced labor", "do not repair", "don't repair", "don’t repair"
    };

    private static readonly string[] InstructorTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_6cd003586762"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_bfe2fd693278"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_cbbcd6a1e6f2"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_d6ab6399a838"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_238d22d9d87c"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_10b516ed9ad7"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_e743e5042372"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_83a26bc11e90"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_003f5df8fab0"),
        "serve as instructors", "train recruits", "drill recruits"
    };

    private static readonly string[] InstructorNegationTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_827159fa4751"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_2fe7f07ea44c"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_067ede91fca4"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_6dff6500b9b0"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_9edfa7c77354"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_0985708c6797"),
        "do not serve as instructors", "don't train recruits", "do not train recruits", "do not train", "don't train", "don’t train"
    };

    private static readonly string[] GenericApprovalTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_905819e2e3a0"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_8cbe697b1573"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_d3dec71e7996"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_952600807338"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_d56646b12f72"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_cdeff49aeb07"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_37c7729f1180"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_db90a40de8b7"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_98e69fb20504"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_75713ce7cac4"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_9e393ff9c54f"),
        "agreed", "approved", "do it", "proceed", "go ahead"
    };

    private static readonly string[] ShortApprovalTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_f867f3417859"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_4cc7b00050cb"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_c875b0f2fef0"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_b5141d3d19e9"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_0ef5c86fab94"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_c02e026355ef"), "ok", "okay", "yes"
    };

    private static readonly string[] ShortRejectionTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_b02dfe2160c6"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_0c70665b6eb6"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_9ba359e542ee"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_aaf6f7c76d35"), "no", "nope"
    };

    private static readonly string[] QuestionOrDiscussionTerms =
    {
        "?", "？", CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_5ac1612110de"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_f4e084b26fd9"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_11c128bc383d"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_44612170a6e8"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_ce5edac7492e"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_dd944934984e"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_78c82d9f4112"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_abee4974732a"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_da9989b814bc"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_4872e35393fe"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_bc7f02642ee3"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_97a6d75de91f"),
        "should we", "what do you think", "do you think", "shall we", "would you", "could we"
    };

    private static readonly string[] GeneralDispositionAdviceTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_b13980077a47"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_bdf3f48adf44"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_05bf0324c181"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_2acf2de63fcb"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_ce1ffd5603c6"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_15a8d6f50a6b"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_8c33b659de06"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_81796de4726c"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_f697cd256ff2"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_ecb402e111ec"),
        "what should we do with", "how should we handle", "what do you suggest"
    };

    public static SiegeCastlePlayerAuthorizationDecision Evaluate(
        string playerText,
        SiegeCastlePrisonerDispositionKind pendingProposalForSpeaker)
    {
        string text = (playerText ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return SiegeCastlePlayerAuthorizationDecision.Denied("player_text_missing");
        }

        int intentCount = CountDispositionIntents(text);
        if (intentCount > 1)
        {
            return SiegeCastlePlayerAuthorizationDecision.Denied("player_authorization_ambiguous");
        }

        if (ContainsAny(text, GlobalRejectionTerms) || EqualsAny(text, ShortRejectionTerms))
        {
            return SiegeCastlePlayerAuthorizationDecision.Denied("player_rejected_or_cancelled");
        }

        SiegeCastlePrisonerDispositionKind intent = DetectIntent(text);
        if (pendingProposalForSpeaker == SiegeCastlePrisonerDispositionKind.Labor
            && intent == SiegeCastlePrisonerDispositionKind.RepairCastle
            && IsBareLaborReference(text)
            && (ContainsAny(text, GenericApprovalTerms) || EqualsAny(text, ShortApprovalTerms)))
        {
            return SiegeCastlePlayerAuthorizationDecision.Authorized(
                pendingProposalForSpeaker,
                usedPendingProposal: true,
                "player_approved_matching_proposal");
        }
        if (intent != SiegeCastlePrisonerDispositionKind.None && !IsDiscussionText(text))
        {
            return SiegeCastlePlayerAuthorizationDecision.Authorized(
                intent,
                usedPendingProposal: false,
                GetExplicitReasonCode(intent));
        }

        if (IsDiscussionText(text))
        {
            return SiegeCastlePlayerAuthorizationDecision.Denied("player_discussion_not_authorization");
        }

        if (ContainsAny(text, GenericApprovalTerms) || EqualsAny(text, ShortApprovalTerms))
        {
            return pendingProposalForSpeaker == SiegeCastlePrisonerDispositionKind.None
                ? SiegeCastlePlayerAuthorizationDecision.Denied("generic_approval_requires_matching_proposal")
                : SiegeCastlePlayerAuthorizationDecision.Authorized(
                    pendingProposalForSpeaker,
                    usedPendingProposal: true,
                    "player_approved_matching_proposal");
        }

        return SiegeCastlePlayerAuthorizationDecision.Denied("player_authorization_not_found");
    }

    public static SiegeCastlePrisonerDispositionKind DetectIntent(string playerText)
    {
        string text = (playerText ?? string.Empty).Trim();
        if (CountDispositionIntents(text) != 1)
        {
            return SiegeCastlePrisonerDispositionKind.None;
        }
        foreach (SiegeCastlePrisonerDispositionKind disposition in DetectPositiveIntents(text))
        {
            return disposition;
        }
        return SiegeCastlePrisonerDispositionKind.None;
    }

    public static IReadOnlyList<SiegeCastlePrisonerDispositionKind> DetectPositiveIntents(string playerText)
    {
        string text = (playerText ?? string.Empty).Trim();
        var result = new List<SiegeCastlePrisonerDispositionKind>();
        foreach (SiegeCastlePrisonerDispositionKind disposition in new[]
        {
            SiegeCastlePrisonerDispositionKind.Recruit,
            SiegeCastlePrisonerDispositionKind.Slaughter,
            SiegeCastlePrisonerDispositionKind.Release,
            SiegeCastlePrisonerDispositionKind.Sell,
            SiegeCastlePrisonerDispositionKind.RepairCastle,
            SiegeCastlePrisonerDispositionKind.Labor,
            SiegeCastlePrisonerDispositionKind.Instructor
        })
        {
            if (HasPositiveIntent(text, disposition))
            {
                result.Add(disposition);
            }
        }
        return result;
    }

    public static bool HasPositiveIntent(string playerText, SiegeCastlePrisonerDispositionKind disposition)
    {
        string text = playerText ?? string.Empty;
        return disposition switch
        {
            SiegeCastlePrisonerDispositionKind.Recruit => ContainsAny(text, RecruitTerms) && !ContainsAny(text, RecruitNegationTerms),
            SiegeCastlePrisonerDispositionKind.Slaughter => ContainsAny(text, SlaughterTerms) && !ContainsAny(text, SlaughterNegationTerms),
            SiegeCastlePrisonerDispositionKind.Release => ContainsAny(text, ReleaseTerms) && !ContainsAny(text, ReleaseNegationTerms),
            SiegeCastlePrisonerDispositionKind.Sell => ContainsAny(text, SellTerms) && !ContainsAny(text, SellNegationTerms),
            SiegeCastlePrisonerDispositionKind.Labor => HasExternalLaborIntent(text),
            SiegeCastlePrisonerDispositionKind.RepairCastle => HasRepairCastleIntent(text),
            SiegeCastlePrisonerDispositionKind.Instructor => ContainsAny(text, InstructorTerms) && !ContainsAny(text, InstructorNegationTerms),
            _ => false
        };
    }

    public static bool IsDiscussionText(string playerText)
        => ContainsAny(playerText ?? string.Empty, QuestionOrDiscussionTerms);

    public static bool IsGeneralDispositionAdviceRequest(string playerText)
        => ContainsAny(playerText ?? string.Empty, GeneralDispositionAdviceTerms);

    private static int CountDispositionIntents(string text)
    {
        return DetectPositiveIntents(text).Count;
    }

    private static string GetExplicitReasonCode(SiegeCastlePrisonerDispositionKind disposition)
    {
        return disposition switch
        {
            SiegeCastlePrisonerDispositionKind.Recruit => "player_explicit_recruit_authorization",
            SiegeCastlePrisonerDispositionKind.Slaughter => "player_explicit_slaughter_authorization",
            SiegeCastlePrisonerDispositionKind.Release => "player_explicit_release_authorization",
            SiegeCastlePrisonerDispositionKind.Sell => "player_explicit_sell_authorization",
            SiegeCastlePrisonerDispositionKind.Labor => "player_explicit_labor_authorization",
            SiegeCastlePrisonerDispositionKind.RepairCastle => "player_explicit_repair_castle_authorization",
            SiegeCastlePrisonerDispositionKind.Instructor => "player_explicit_instructor_authorization",
            _ => "player_explicit_castle_disposition_authorization"
        };
    }

    private static bool HasExternalLaborIntent(string text)
    {
        return !ContainsAny(text, LaborNegationTerms)
            && !ContainsAny(text, RepairCastleTerms)
            && ContainsAny(text, ExternalLaborTerms);
    }

    private static bool HasRepairCastleIntent(string text)
    {
        if (ContainsAny(text, LaborNegationTerms))
        {
            return false;
        }
        return ContainsAny(text, RepairCastleTerms)
            || (ContainsAny(text, GenericLaborTerms) && !ContainsAny(text, ExternalLaborTerms));
    }

    private static bool IsBareLaborReference(string text)
    {
        return ContainsAny(text, GenericLaborTerms)
            && !ContainsAny(text, ExternalLaborTerms)
            && !ContainsAny(text, RepairCastleTerms);
    }

    private static bool ContainsAny(string text, string[] terms)
    {
        foreach (string term in terms)
        {
            if (!string.IsNullOrWhiteSpace(term)
                && text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }
        return false;
    }

    private static bool EqualsAny(string text, string[] terms)
    {
        string normalized = (text ?? string.Empty).Trim().Trim('。', '！', '!', '？', '?', '，', ',', '.', '；', ';', '…').Trim();
        foreach (string term in terms)
        {
            if (string.Equals(normalized, term, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}

public sealed class SiegeCastlePlayerAuthorizationDecision
{
    private SiegeCastlePlayerAuthorizationDecision(
        bool isAuthorized,
        SiegeCastlePrisonerDispositionKind disposition,
        bool usedPendingProposal,
        string reasonCode)
    {
        IsAuthorized = isAuthorized;
        Disposition = disposition;
        UsedPendingProposal = usedPendingProposal;
        ReasonCode = reasonCode ?? string.Empty;
    }

    public bool IsAuthorized { get; }

    public SiegeCastlePrisonerDispositionKind Disposition { get; }

    public bool UsedPendingProposal { get; }

    public string ReasonCode { get; }

    internal static SiegeCastlePlayerAuthorizationDecision Authorized(
        SiegeCastlePrisonerDispositionKind disposition,
        bool usedPendingProposal,
        string reasonCode)
    {
        return new SiegeCastlePlayerAuthorizationDecision(true, disposition, usedPendingProposal, reasonCode);
    }

    internal static SiegeCastlePlayerAuthorizationDecision Denied(string reasonCode)
    {
        return new SiegeCastlePlayerAuthorizationDecision(
            false,
            SiegeCastlePrisonerDispositionKind.None,
            usedPendingProposal: false,
            reasonCode);
    }
}
