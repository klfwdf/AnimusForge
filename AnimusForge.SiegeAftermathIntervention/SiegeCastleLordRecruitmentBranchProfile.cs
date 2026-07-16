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
        "引见族长", "引荐族长", "写信给族长", "写信给你的族长", "写信通知族长", "通知你的族长", "联络族长",
        "introduce your clan leader", "write to your clan leader", "contact your clan leader"
    };

    private static readonly string[] JoinAsCompanionTerms =
    {
        "背叛家族", "离开家族", "加入我的家族", "成为我的同伴", "做我的同伴", "跟随我", "投靠我个人",
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
        return branch switch
        {
            SiegeCastleLordRecruitmentBranch.ClanLeaderJoinPlayerKingdom => "族长率全族加入玩家统治的王国",
            SiegeCastleLordRecruitmentBranch.ClanLeaderRequestRulerAudience => "族长请求玩家引见本国统治者",
            SiegeCastleLordRecruitmentBranch.ClanLeaderSupportPlayerClaim => "族长表达拥立独立玩家为王的政治意向",
            SiegeCastleLordRecruitmentBranch.IntroduceClanLeaderByLetter => "非族长写信引见本族族长",
            SiegeCastleLordRecruitmentBranch.JoinPlayerAsCompanion => "非族长背叛原家族并成为玩家同伴",
            _ => "尚未明确领主收编分支"
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
