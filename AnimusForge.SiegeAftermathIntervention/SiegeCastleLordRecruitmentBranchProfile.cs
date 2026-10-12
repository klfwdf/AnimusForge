using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free political branch selection for recruiting one captured lord during
/// an active castle aftermath. The Bannerlord bridge only executes the returned branch.
/// </summary>
public enum SiegeCastleLordRecruitmentBranch
{
    Unknown = 0,
    ClanLeaderJoinPlayerKingdom = 1,
    ClanLeaderRequestRulerAudience = 2,
    ClanLeaderSupportPlayerClaim = 3,
    IntroduceClanLeaderByLetter = 4,
    JoinPlayerAsCompanion = 5
}

public static class SiegeCastleLordRecruitmentBranchProfile
{
    private static readonly string[] IntroduceClanLeaderTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_09adaf08767b"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_401efcab3e3f"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_58860af02718"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_ae3d12cba9d4"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_066c215fe364"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_ff1e764ef32f"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_e606892d2e08"),
        "introduce your clan leader", "write to your clan leader", "contact your clan leader"
    };

    private static readonly string[] JoinAsCompanionTerms =
    {
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_4a380322b7bf"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_38fe457a363e"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_5f0b61751015"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_e25da682b745"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_31f0799d19c4"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_1ee6d83190ac"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_9438f053d1af"),
        "betray your clan", "leave your clan", "join my clan", "become my companion", "follow me"
    };

    public static SiegeCastleLordRecruitmentBranch Resolve(
        bool speakerIsClanLeader,
        bool playerHasKingdom,
        bool playerRulesKingdom,
        string playerText)
    {
        if (speakerIsClanLeader)
        {
            if (playerHasKingdom && playerRulesKingdom)
            {
                return SiegeCastleLordRecruitmentBranch.ClanLeaderJoinPlayerKingdom;
            }

            return playerHasKingdom
                ? SiegeCastleLordRecruitmentBranch.ClanLeaderRequestRulerAudience
                : SiegeCastleLordRecruitmentBranch.ClanLeaderSupportPlayerClaim;
        }

        string text = (playerText ?? string.Empty).Trim();
        bool introduce = ContainsAny(text, IntroduceClanLeaderTerms);
        bool companion = ContainsAny(text, JoinAsCompanionTerms);
        if (introduce == companion)
        {
            return SiegeCastleLordRecruitmentBranch.Unknown;
        }

        return introduce
            ? SiegeCastleLordRecruitmentBranch.IntroduceClanLeaderByLetter
            : SiegeCastleLordRecruitmentBranch.JoinPlayerAsCompanion;
    }

    public static bool ResolvesImmediately(SiegeCastleLordRecruitmentBranch branch)
    {
        return branch == SiegeCastleLordRecruitmentBranch.ClanLeaderJoinPlayerKingdom
            || branch == SiegeCastleLordRecruitmentBranch.JoinPlayerAsCompanion;
    }

    public static string Describe(SiegeCastleLordRecruitmentBranch branch)
    {
        return Describe(branch, isEnglish: false);
    }

    public static string Describe(SiegeCastleLordRecruitmentBranch branch, bool isEnglish)
    {
        if (isEnglish)
        {
            return branch switch
            {
                SiegeCastleLordRecruitmentBranch.ClanLeaderJoinPlayerKingdom => "The clan leader brings the entire clan into the player\u0027s kingdom",
                SiegeCastleLordRecruitmentBranch.ClanLeaderRequestRulerAudience => "The clan leader asks the player for an audience with their ruler",
                SiegeCastleLordRecruitmentBranch.ClanLeaderSupportPlayerClaim => "The clan leader expresses support for crowning the independent player",
                SiegeCastleLordRecruitmentBranch.IntroduceClanLeaderByLetter => "A clan member writes an introduction letter to their clan leader",
                SiegeCastleLordRecruitmentBranch.JoinPlayerAsCompanion => "A clan member leaves their former clan and becomes the player\u0027s companion",
                _ => "Lord recruitment branch is not yet determined"
            };
        }
        return branch switch
        {
            SiegeCastleLordRecruitmentBranch.ClanLeaderJoinPlayerKingdom => CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_7bfbe3f8923d"),
            SiegeCastleLordRecruitmentBranch.ClanLeaderRequestRulerAudience => CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_b9c9a87aeb8f"),
            SiegeCastleLordRecruitmentBranch.ClanLeaderSupportPlayerClaim => CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_a4deb8ec2430"),
            SiegeCastleLordRecruitmentBranch.IntroduceClanLeaderByLetter => CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_1116df118f02"),
            SiegeCastleLordRecruitmentBranch.JoinPlayerAsCompanion => CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_cb1e609988ae"),
            _ => CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_recruitment_branch_profile_6277bebf2ec5")
        };
    }

    private static bool ContainsAny(string text, string[] terms)
    {
        if (text.Length == 0)
        {
            return false;
        }

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
