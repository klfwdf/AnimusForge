using System;
using AnimusForge.DiplomacyDialogue;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Domain;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    internal static bool TryProcessBilateral(Hero npc, string tag, string payload)
    {
        var owner = Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>();
        if (!IsWorldDiplomacyEnabled() || owner?.IsLiveCampaign != true
            || !owner._orchestration.IsBilateralConversation(npc?.StringId, npc?.Clan?.Kingdom?.StringId)) return false;
        string player = Clan.PlayerClan.Kingdom.StringId, actor = npc.Clan.Kingdom.StringId;
        if (string.Equals(tag, "COMMITMENT", StringComparison.OrdinalIgnoreCase))
        {
            owner._orchestration.ControlOralDiplomaticCommitment(npc.StringId, actor, payload, DiplomacyDialogueSourceScope.Current);
            return true;
        }
        // Late/custom COMMIT tags for a direct matter are consumed, not reinterpreted
        // as assent and not sent to the old execution authority.
        if (string.Equals(tag, "COMMIT", StringComparison.OrdinalIgnoreCase))
            return DialogueTagPayload.TryParse(payload, out var parsed) && owner._orchestration.OwnsDirectBilateralMatter(parsed, actor, player);
        if (!DiplomacyBilateralRouting.TryAction(tag, out var action)) return false;
        string[] p = (payload ?? "").Split('|')[0].Split(':');
        if (p.Length < 2 || !DiplomacyBilateralRouting.IsPair(p[0].Trim(), p[1].Trim(), player, actor)) return false;
        if (!DiplomacyBilateralPayload.TryParse(tag, payload, player, actor, out var request))
        { Log("bilateral payload rejected action=" + tag); return true; }
        owner._orchestration.ExecuteBilateral(npc.StringId, actor, action, DiplomacyDialogueSourceScope.Current,
            () => owner.ApplyBilateral(npc, request.Tag, request.Payload, request.Parts), request);
        return true;
    }

    private DiplomacyBilateralResult ApplyBilateral(Hero npc, string tag, string payload, string[] p)
    {
        var player = Clan.PlayerClan?.Kingdom; var other = npc?.Clan?.Kingdom;
        var result = new DiplomacyBilateralResult { State = "rejected", Actor = p[0].Trim(), Target = p[1].Trim(), Diagnostic = "invalid_terms" };
        if (!TWParallel.IsMainThread() || player?.RulingClan?.Leader != Hero.MainHero || other?.RulingClan?.Leader != npc
            || player == other || player == null || other == null || player.IsEliminated || other.IsEliminated) return result;
        void Receipt(bool applied, string status, string diagnostic)
        { result.State = DiplomacyBilateralEffects.State(applied,status); result.Diagnostic = diagnostic; }
        switch (tag)
        {
            case "DECLARE_WAR":
            {
                if (p.Length != 2) return result;
                var r = WorldDiplomacyOralDeclareWarRules.ResolveCommand(payload, true, other.StringId, npc.StringId, true, player.StringId, false, true);
                if (!r.IsReady) return result;
                AnimusForge.Refactor.Contracts.WorldDiplomacyDeclareWarExecutionReceipt receipt = default;
                RunAuthorizedEventWar(ResolveKingdom(r.Command.DeclarerKingdomId), ResolveKingdom(r.Command.TargetKingdomId),
                    () => receipt = new BannerlordWorldDiplomacyDeclareWarGameActionPort().Execute(r.Command));
                Receipt(receipt.IsApplied, receipt.Status.ToString(), receipt.ErrorCode); break;
            }
            case "MAKE_PEACE":
            {
                if (p.Length != 4 && p.Length != 7 || !int.TryParse(p[2], out int amount) || !int.TryParse(p[3], out int days)
                    || amount < 0 || amount == 0 && days != 0 || amount > 0 && (days < 1 || days > 252)) return result;
                var terms = new WorldDiplomacyPeaceTerms { TributePayerKingdomId = result.Actor, TributeReceiverKingdomId = result.Target,
                    DailyTribute = amount, DurationDays = days, CessionFromKingdomId = p.Length == 7 ? p[4].Trim() : "",
                    CessionToKingdomId = p.Length == 7 ? p[5].Trim() : "", CessionSettlementId = p.Length == 7 ? p[6].Trim() : "" };
                if (p.Length == 7 && (terms.CessionFromKingdomId == "" || terms.CessionToKingdomId == "" || terms.CessionSettlementId == "")) return result;
                if (!WorldDiplomacyPeaceAdmissionApplication.TryValidatePeaceTerms(new PeaceAdmissionPort(this), terms,
                    player.StringId, other.StringId, out result.Diagnostic)) return result;
                var r = WorldDiplomacyOralMakePeaceRules.ResolveCommand(string.Join(":", p, 0, 4), true, player.StringId, false, true, true, other.StringId, npc.StringId, true);
                if (!r.IsReady) return result;
                return DiplomacyBilateralEffects.Peace(terms, () =>
                {
                    AnimusForge.Refactor.Contracts.WorldDiplomacyMakePeaceExecutionReceipt receipt = default;
                    RunAuthorizedEventPeace(player, other, () => receipt = new BannerlordWorldDiplomacyMakePeaceGameActionPort().Execute(r.Command));
                    return receipt;
                }, () => TryApplyValidatedCession(terms, player, other));
            }
            case "FORM_ALLIANCE":
            {
                // The native alliance API has no requested-duration argument.
                if (p.Length != 2 && (p.Length != 3 || !p[2].Trim().Equals("default", StringComparison.OrdinalIgnoreCase))) return result;
                if (FactionManager.IsAtWarAgainstFaction(player, other)) return result;
                var r = WorldDiplomacyOralFormAllianceRules.ResolveCommand(payload, true, player.StringId, false, true, true, other.StringId, npc.StringId, true);
                if (!r.IsReady) return result;
                var receipt = new BannerlordWorldDiplomacyFormAllianceGameActionPort().Execute(r.Command);
                Receipt(receipt.IsApplied, receipt.Status.ToString(), receipt.ErrorCode); break;
            }
            case "MAKE_TRADE":
            {
                if (p.Length != 2 && (p.Length != 3 || !p[2].Trim().Equals("default", StringComparison.OrdinalIgnoreCase)
                    && (!int.TryParse(p[2], out int days) || days < 1 || days > 252))) return result;
                if (FactionManager.IsAtWarAgainstFaction(player, other)) return result;
                var r = WorldDiplomacyOralMakeTradeRules.ResolveCommand(payload, true, player.StringId, false, true, true, other.StringId, npc.StringId, true);
                if (!r.IsReady) return result;
                var receipt = new BannerlordWorldDiplomacyMakeTradeGameActionPort().Execute(r.Command);
                Receipt(receipt.IsApplied, receipt.Status.ToString(), receipt.ErrorCode);
                result.Terms.DurationDays = receipt.AppliedDurationDays; break;
            }
            case "BREAK_ALLIANCE":
            {
                if (p.Length != 2) return result;
                var r = WorldDiplomacyOralBreakAllianceRules.ResolveCommand(payload, true, player.StringId, false, true, other.StringId, npc.StringId);
                if (!r.IsReady) return result;
                var receipt = new BannerlordWorldDiplomacyBreakAllianceGameActionPort().Execute(r.Command);
                Receipt(receipt.IsApplied, receipt.Status.ToString(), receipt.ErrorCode); break;
            }
            case "CANCEL_TRADE":
            {
                if (p.Length != 2) return result;
                var r = WorldDiplomacyOralCancelTradeRules.ResolveCommand(payload, true, player.StringId, false, true, other.StringId, npc.StringId);
                if (!r.IsReady) return result;
                var receipt = new BannerlordWorldDiplomacyCancelTradeGameActionPort().Execute(r.Command);
                Receipt(receipt.IsApplied, receipt.Status.ToString(), receipt.ErrorCode); break;
            }
        }
        return result;
    }
}
