using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Conservative player-intent gate for the castle soldier-appeasement settlement.
/// Soldier acceptance alone must never waive the morale penalty unless the player actually
/// offered reassurance, compensation, a discipline explanation, or a spoil arrangement.
/// </summary>
public static class SiegeCastleSoldierAppeasementAuthorizationPolicy
{
    private static readonly string[] AppeasementTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_8585ac1d1cbd"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_102f7d8002fe"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_a6631588a75f"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_184c79e9eff7"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_6e4d4f20453e"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_c0c74196c2d8"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_6138d03398eb"),
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_f2286fa997d3"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_a3a7eb335347"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_9db60007db37"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_4fe0ce4458fb"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_eaec5cbcc035"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_a4a68bc36dcc"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_f39fbc9e4fd5"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_e24d94d2f56a"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_d866d899e8ba"),
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_57b9271a3339"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_0efcd6a338c4"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_650cd2604b54"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_046cfe468fca"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_c3060fa6bc29"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_48d556a5d844"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_3b3bf25b8270"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_f5339fb513eb"),
        "reassure", "compensate", "reward you", "share the spoils", "discipline explanation"
    };

    private static readonly string[] RefusalTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_a48800a3e126"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_72ada1fe8a61"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_1ef481a95b56"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_4333ee25e014"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_9b0856208288"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_dcfd3b75843f"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_344d93c10d00"),
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_b7f9a16419f5"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_03e566fb50b2"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_soldier_appeasement_authorization_policy_30fc4c903414"),
        "no compensation", "will not compensate", "no explanation"
    };

    private static readonly string[] DiscussionTerms =
    {
        "?", "？", CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_5ac1612110de"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_f4e084b26fd9"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_11c128bc383d"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_dd944934984e"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_78c82d9f4112"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_da9989b814bc"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_4872e35393fe"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_bc7f02642ee3"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_97a6d75de91f"),
        "what do you think", "do you think", "should i", "should we", "would you"
    };

    public static SiegeCastleSoldierAppeasementAuthorizationDecision Evaluate(string playerText)
    {
        string text = (playerText ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return SiegeCastleSoldierAppeasementAuthorizationDecision.Denied("player_text_missing");
        }

        if (ContainsAny(text, DiscussionTerms))
        {
            return SiegeCastleSoldierAppeasementAuthorizationDecision.Denied("player_discussion_not_appeasement");
        }

        if (ContainsAny(text, RefusalTerms))
        {
            return SiegeCastleSoldierAppeasementAuthorizationDecision.Denied("player_refused_appeasement");
        }

        return ContainsAny(text, AppeasementTerms)
            ? SiegeCastleSoldierAppeasementAuthorizationDecision.Authorized("player_explicit_appeasement")
            : SiegeCastleSoldierAppeasementAuthorizationDecision.Denied("player_appeasement_not_found");
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
}

public sealed class SiegeCastleSoldierAppeasementAuthorizationDecision
{
    private SiegeCastleSoldierAppeasementAuthorizationDecision(bool isAuthorized, string reasonCode)
    {
        IsAuthorized = isAuthorized;
        ReasonCode = reasonCode ?? string.Empty;
    }

    public bool IsAuthorized { get; }

    public string ReasonCode { get; }

    internal static SiegeCastleSoldierAppeasementAuthorizationDecision Authorized(string reasonCode)
    {
        return new SiegeCastleSoldierAppeasementAuthorizationDecision(true, reasonCode);
    }

    internal static SiegeCastleSoldierAppeasementAuthorizationDecision Denied(string reasonCode)
    {
        return new SiegeCastleSoldierAppeasementAuthorizationDecision(false, reasonCode);
    }
}
