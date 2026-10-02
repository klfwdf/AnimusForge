using System;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Library;

namespace AnimusForge.Refactor.Adapters;

public sealed class BannerlordWorldDiplomacyBreakAllianceGameActionPort
    : IWorldDiplomacyBreakAllianceGameActionPort
{
    public WorldDiplomacyBreakAllianceExecutionReceipt Execute(WorldDiplomacyBreakAllianceCommand command)
    {
        if (!command.IsValid)
        {
            return Receipt(WorldDiplomacyBreakAllianceExecutionStatus.InvalidCommand, command,
                "diplomacy.break_alliance.invalid_command");
        }

        bool isMainThread;
        try
        {
            isMainThread = TWParallel.IsMainThread();
        }
        catch
        {
            isMainThread = false;
        }
        if (!isMainThread)
        {
            return Receipt(WorldDiplomacyBreakAllianceExecutionStatus.NotMainThread, command,
                "diplomacy.break_alliance.not_main_thread");
        }

        Kingdom playerKingdom;
        Kingdom npcKingdom;
        IAllianceCampaignBehavior alliance;
        try
        {
            ResolveKingdoms(command.PlayerKingdomId, command.NpcKingdomId,
                out playerKingdom, out npcKingdom);
            if (playerKingdom == null)
            {
                return Receipt(WorldDiplomacyBreakAllianceExecutionStatus.PlayerKingdomUnavailable, command,
                    "diplomacy.break_alliance.player_kingdom_unavailable");
            }
            if (npcKingdom == null)
            {
                return Receipt(WorldDiplomacyBreakAllianceExecutionStatus.NpcKingdomUnavailable, command,
                    "diplomacy.break_alliance.npc_kingdom_unavailable");
            }
            if (playerKingdom.IsEliminated)
            {
                return Receipt(WorldDiplomacyBreakAllianceExecutionStatus.PlayerKingdomEliminated, command,
                    "diplomacy.break_alliance.player_kingdom_eliminated");
            }
            if (npcKingdom.IsEliminated)
            {
                return Receipt(WorldDiplomacyBreakAllianceExecutionStatus.NpcKingdomEliminated, command,
                    "diplomacy.break_alliance.npc_kingdom_eliminated");
            }
            if (ReferenceEquals(playerKingdom, npcKingdom))
            {
                return Receipt(WorldDiplomacyBreakAllianceExecutionStatus.SameKingdom, command,
                    "diplomacy.break_alliance.same_kingdom");
            }
            if (!ReferenceEquals(Clan.PlayerClan?.Kingdom, playerKingdom))
            {
                return Receipt(WorldDiplomacyBreakAllianceExecutionStatus.PlayerUnauthorized, command,
                    "diplomacy.break_alliance.player_unauthorized");
            }

            Hero speaker = ResolveHero(command.SpeakerHeroId);
            if (speaker == null || !ReferenceEquals(speaker.Clan?.Kingdom, npcKingdom))
            {
                return Receipt(WorldDiplomacyBreakAllianceExecutionStatus.SpeakerUnauthorized, command,
                    "diplomacy.break_alliance.speaker_unauthorized");
            }

            alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
            if (alliance == null)
            {
                return Receipt(WorldDiplomacyBreakAllianceExecutionStatus.AllianceBehaviorUnavailable, command,
                    "diplomacy.break_alliance.behavior_unavailable");
            }
            if (!alliance.IsAllyWithKingdom(playerKingdom, npcKingdom))
            {
                return Receipt(WorldDiplomacyBreakAllianceExecutionStatus.NotAllied, command,
                    "diplomacy.break_alliance.not_allied");
            }
        }
        catch
        {
            return Receipt(WorldDiplomacyBreakAllianceExecutionStatus.RejectedBeforeStart, command,
                "diplomacy.break_alliance.validation_exception");
        }

        DiplomacyEffectReadback result = DiplomacyEffectReadback.Execute(
            () => MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked("diplomacy_break_alliance", () => PermanentAllianceGuard.RunAuthorizedBreak(
                    "diplomacy_break_alliance",
                    playerKingdom,
                    npcKingdom,
                    () => alliance.EndAlliance(playerKingdom, npcKingdom))),
            () => !alliance.IsAllyWithKingdom(playerKingdom, npcKingdom));
        return Receipt(!result.IsKnown ? WorldDiplomacyBreakAllianceExecutionStatus.UnknownAfterStart
            : result.Applied ? WorldDiplomacyBreakAllianceExecutionStatus.Applied
            : WorldDiplomacyBreakAllianceExecutionStatus.ActionNotApplied, command,
            result.Applied ? "" : "diplomacy.break_alliance." + (result.IsKnown ? "action_not_applied" : "readback_unknown"));
    }

    private static void ResolveKingdoms(
        string playerKingdomId,
        string npcKingdomId,
        out Kingdom playerKingdom,
        out Kingdom npcKingdom)
    {
        playerKingdom = null;
        npcKingdom = null;
        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (kingdom == null) continue;
            if (playerKingdom == null
                && string.Equals(kingdom.StringId, playerKingdomId, StringComparison.OrdinalIgnoreCase))
            {
                playerKingdom = kingdom;
            }
            if (npcKingdom == null
                && string.Equals(kingdom.StringId, npcKingdomId, StringComparison.OrdinalIgnoreCase))
            {
                npcKingdom = kingdom;
            }
            if (playerKingdom != null && npcKingdom != null) return;
        }
    }

    private static Hero ResolveHero(string heroId)
    {
        return Hero.Find(heroId) ?? Hero.FindFirst(hero => hero != null
            && string.Equals(hero.StringId, heroId, StringComparison.OrdinalIgnoreCase));
    }

    private static WorldDiplomacyBreakAllianceExecutionReceipt Receipt(
        WorldDiplomacyBreakAllianceExecutionStatus status,
        WorldDiplomacyBreakAllianceCommand command,
        string errorCode)
    {
        return new WorldDiplomacyBreakAllianceExecutionReceipt(
            status,
            command.PlayerKingdomId,
            command.NpcKingdomId,
            errorCode);
    }
}
