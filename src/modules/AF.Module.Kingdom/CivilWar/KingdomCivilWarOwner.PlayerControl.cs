using System;
using System.Linq;
using System.Collections.Generic;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AnimusForge;

internal sealed partial class KingdomCivilWarOwner
{
    private static bool IsPlayerLed(KingdomCivilWarFactionState faction)
        => Clan.PlayerClan != null && faction?.LeaderClanId == Clan.PlayerClan.StringId;

    // Only explicit actions/panel refreshes read clan strength. No tick-time world scan or cache of mutable armies.
    private static float PlayerStrengthPercent(Kingdom kingdom, Clan player)
    {
        float total = 0f;
        foreach (Clan clan in CivilWarWorld.LandedClans(kingdom)) total += CivilWarWorld.Strength(clan);
        return total <= 0f ? 0f : CivilWarRules.Clamp(100f * CivilWarWorld.Strength(player) / total, 0f, 100f);
    }

    private bool PlayerDetonationAllowed(Kingdom kingdom, Clan actor, out string reason)
    {
        var state = Find(kingdom);
        int cooldown = Math.Max(state?.CooldownUntilDay ?? 0, (state?.CooldownUntilWeek ?? 0) * 7);
        if (CivilWarWorld.CurrentDay() < cooldown)
        {
            reason = CivilWarUiText.T("AF_KINGDOM_CW_PlayerControl_KingdomCooldown", "The kingdom is in post-war cooldown until {A}; the player cannot raise a rebellion by hand either.", ("A", CivilWarUiText.Date(cooldown)));
            return false;
        }
        int threshold = DuelSettings.BuildCivilWarTuning().PlayerDetonationStrengthPercent;
        float percent = PlayerStrengthPercent(kingdom, actor);
        reason = percent < threshold
            ? CivilWarUiText.T("AF_KINGDOM_CW_PlayerControl_StrengthBelowThreshold", "Your clan holds {A}% of the kingdom's military strength, below the {B}% needed to raise a rebellion by hand.", ("A", percent.ToString("0.0")), ("B", threshold.ToString()))
            : "";
        return reason.Length == 0;
    }

    private static void ApplyFoundingRelationLoss(Kingdom kingdom, Clan founder)
    {
        CivilWarWorld.ChangeRelation(founder?.Leader, kingdom?.Leader, -CivilWarPoliticalRules.FoundRulerRelationLoss);
    }

    private void DetachPoliticalSide(Kingdom k, KingdomCivilWarKingdomState s, Clan actor, KingdomCivilWarClanState member, int week, int day, ICollection<Hero> affectedParticipants = null)
    {
        var own = FactionOfClan(s, actor);
        var old = own == null ? s.Clans.Values.Where(x => x.Side == KingdomCivilWarSide.Crown) : Members(s, own);
        var others = old.Select(x => CivilWarWorld.FindClan(x.ClanId)).Where(x => x != null && x != actor).ToList();
        Clan leader = own == null ? k.RulingClan : CivilWarWorld.FindClan(own.LeaderClanId);
        if (leader != null && leader != actor && !others.Contains(leader)) others.Add(leader);
        foreach (var other in others)
        {
            // Capture exactly the Hero affected by the relation change, before any action callbacks.
            Hero otherLeader = other.Leader;
            if (otherLeader != null) affectedParticipants?.Add(otherLeader);
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(actor.Leader, otherLeader,
                other == leader ? -CivilWarPoliticalRules.ExitLeaderRelationLoss : -CivilWarPoliticalRules.ExitMemberRelationLoss, false);
        }
        member.Side = KingdomCivilWarSide.Middle; member.FactionId = ""; member.SideSinceWeek = week; member.SideSinceDay = day;
        _storage.ClanExitUntilDay[actor.StringId] = day + CivilWarPoliticalRules.ExitDays;
        if (actor == Clan.PlayerClan) s.PlayerSide = "";
        if (own?.LeaderClanId == actor.StringId) EnsurePoliticalLeader(k, s, own, startCooldown: actor != Clan.PlayerClan);
    }

    private string ChangePlayerFactionDemand(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f,
        CivilWarActionRequest request, int week, CivilWarTuning tuning, out string displayDemand, out string displayRetained)
    {
        var oldDemand = CivilWarCatalog.FindDemand(f.DemandId);
        var newDemand = CivilWarCatalog.FindDemand(request.DemandId);
        // One snapshot / two linear cache rebuilds per explicit action; no per-frame or all-world recruitment scan.
        var members = Members(s, f).ToArray();
        f.LastParticipantIds = members.Select(x => x.ClanId).ToList();
        foreach (bool ignored in RebuildPoliticalCaches(s)) { }
        f.DemandId = newDemand.Id; f.TargetId = request.TargetId;
        f.TargetName = newDemand.Target == CivilWarDemandTarget.EnemyKingdom
            ? CivilWarWorld.KingdomName(CivilWarWorld.FindKingdom(request.TargetId))
            : k.ActivePolicies.FirstOrDefault(x => x.StringId == request.TargetId)?.Name?.ToString() ?? "";
        f.WarGoal = (int)CivilWarCatalog.EffectiveWarGoal(newDemand);
        f.Refusals = f.Defers = 0; f.LastRuling = ""; f.LastEscalateChance = 0f;
        f.DemandLocked = false; f.WaitingForOtherWar = f.EscalationPending = false;
        f.Stage = KingdomCivilWarStage.FactionFormed; f.StageWeek = week;
        f.PlayerAnswerPending = false; f.PlayerPrompted = false;
        f.LastDemandDay = CivilWarWorld.CurrentDay(); RefuseAndReschedule(f, week, tuning);
        int retained = 0; var departed = new List<string>(); int stability = Host.GetStability(k);
        foreach (var member in members)
        {
            if (member.ClanId == f.LeaderClanId) continue;
            var clan = CivilWarWorld.FindClan(member.ClanId);
            float leaveRaw = CivilWarCatalog.LeaveOpposition.Raw(BuildFeatures(k, s, f, clan, stability));
            float chance = CivilWarPoliticalRules.DemandRetentionChance(DemandAffinity(oldDemand, member), DemandAffinity(newDemand, member), leaveRaw, tuning);
            member.MembershipEvaluationDay = CivilWarWorld.CurrentDay();
            if (PoliticalClan(clan, k) && RandomFloat() < chance) { retained++; continue; }
            member.Side = KingdomCivilWarSide.Middle; member.FactionId = "";
            member.SideSinceWeek = week; member.SideSinceDay = CivilWarWorld.CurrentDay();
            departed.Add(CivilWarWorld.ClanName(clan));
        }
        foreach (bool ignored in RebuildPoliticalCaches(s)) { }
        // Deliberately not localized: this return value becomes CivilWarActionResult.Message, which the Applied
        // path publishes as political-result material (LLM/AFEF), stores in the operation receipt and shows in the
        // panel chronicle, so it must stay a stable rule string. A localized list separator would be needed if
        // that decision is ever revisited.
        // Display projection data for the result notification. The returned Message below stays the
        // stable Chinese rule string that the receipt, AFEF and weekly material keep consuming.
        displayDemand = CivilWarUiText.DemandDisplayText(newDemand, ResolveDemandTargetDisplay(newDemand, f, k));
        displayRetained = retained.ToString();
        return KingdomCivilWarTexts.Raw("KW_Raw_153") + FormatDemand(newDemand, f.TargetName) + "；" + retained + KingdomCivilWarTexts.Raw("KW_Raw_154")
            + departed.Count + KingdomCivilWarTexts.Raw("KW_Raw_155") + (departed.Count == 0 ? "。" : "（" + string.Join("、", departed) + "）。") + KingdomCivilWarTexts.Raw("KW_Raw_156");
    }

    private void DissolvePlayerFaction(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState faction, Clan actor, int week, CivilWarTuning tuning, ICollection<Hero> affectedParticipants = null)
    {
        foreach (var record in Members(s, faction))
        {
            var other = CivilWarWorld.FindClan(record.ClanId);
            if (other != null && other != actor)
            {
                Hero otherLeader = other.Leader;
                if (otherLeader != null) affectedParticipants?.Add(otherLeader);
                CivilWarWorld.ChangeRelation(actor.Leader, otherLeader, -CivilWarPoliticalRules.ExitMemberRelationLoss);
            }
        }
        // This is administrative dissolution before war, not a completed war; never erase an existing cooldown.
        FinishFaction(k, s, faction, week, tuning, null, 0, startCooldown: false);
    }
}
