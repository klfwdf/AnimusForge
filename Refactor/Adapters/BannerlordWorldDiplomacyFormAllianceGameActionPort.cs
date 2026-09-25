using System;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Library;

namespace AnimusForge.Refactor.Adapters;

public sealed class BannerlordWorldDiplomacyFormAllianceGameActionPort
    : IWorldDiplomacyFormAllianceGameActionPort
{
    private const int MaximumAllianceCount = 2;

    public WorldDiplomacyFormAllianceExecutionReceipt Execute(WorldDiplomacyFormAllianceCommand command)
    {
        if (!command.IsValid)
        {
            return Receipt(WorldDiplomacyFormAllianceExecutionStatus.InvalidCommand, command,
                "diplomacy.form_alliance.invalid_command");
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
            return Receipt(WorldDiplomacyFormAllianceExecutionStatus.NotMainThread, command,
                "diplomacy.form_alliance.not_main_thread");
        }

        Kingdom playerKingdom;
        Kingdom npcKingdom;
        IAllianceCampaignBehavior alliance;
        try
        {
            playerKingdom = ResolveKingdom(command.PlayerKingdomId);
            npcKingdom = ResolveKingdom(command.NpcKingdomId);
            if (playerKingdom == null)
            {
                return Receipt(WorldDiplomacyFormAllianceExecutionStatus.PlayerKingdomUnavailable, command,
                    "diplomacy.form_alliance.player_kingdom_unavailable");
            }
            if (npcKingdom == null)
            {
                return Receipt(WorldDiplomacyFormAllianceExecutionStatus.NpcKingdomUnavailable, command,
                    "diplomacy.form_alliance.npc_kingdom_unavailable");
            }
            if (playerKingdom.IsEliminated)
            {
                return Receipt(WorldDiplomacyFormAllianceExecutionStatus.PlayerKingdomEliminated, command,
                    "diplomacy.form_alliance.player_kingdom_eliminated");
            }
            if (npcKingdom.IsEliminated)
            {
                return Receipt(WorldDiplomacyFormAllianceExecutionStatus.NpcKingdomEliminated, command,
                    "diplomacy.form_alliance.npc_kingdom_eliminated");
            }
            if (ReferenceEquals(playerKingdom, npcKingdom))
            {
                return Receipt(WorldDiplomacyFormAllianceExecutionStatus.SameKingdom, command,
                    "diplomacy.form_alliance.same_kingdom");
            }
            if (!ReferenceEquals(Clan.PlayerClan?.Kingdom, playerKingdom)
                || !ReferenceEquals(Hero.MainHero, playerKingdom.RulingClan?.Leader))
            {
                return Receipt(WorldDiplomacyFormAllianceExecutionStatus.PlayerUnauthorized, command,
                    "diplomacy.form_alliance.player_unauthorized");
            }

            Hero speaker = ResolveHero(command.SpeakerHeroId);
            if (speaker == null
                || !ReferenceEquals(speaker.Clan?.Kingdom, npcKingdom)
                || !ReferenceEquals(npcKingdom.RulingClan?.Leader, speaker))
            {
                return Receipt(WorldDiplomacyFormAllianceExecutionStatus.SpeakerUnauthorized, command,
                    "diplomacy.form_alliance.speaker_unauthorized");
            }

            alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
            if (alliance == null)
            {
                return Receipt(WorldDiplomacyFormAllianceExecutionStatus.AllianceBehaviorUnavailable, command,
                    "diplomacy.form_alliance.behavior_unavailable");
            }
            if (alliance.IsAllyWithKingdom(playerKingdom, npcKingdom))
            {
                return Receipt(WorldDiplomacyFormAllianceExecutionStatus.AlreadyAllied, command,
                    "diplomacy.form_alliance.already_allied");
            }

            int playerAllianceCount = 0;
            int npcAllianceCount = 0;
            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (kingdom == null || kingdom.IsEliminated) continue;
                if (!ReferenceEquals(kingdom, playerKingdom)
                    && alliance.IsAllyWithKingdom(playerKingdom, kingdom)) playerAllianceCount++;
                if (!ReferenceEquals(kingdom, npcKingdom)
                    && alliance.IsAllyWithKingdom(npcKingdom, kingdom)) npcAllianceCount++;
            }
            if (playerAllianceCount >= MaximumAllianceCount)
            {
                return Receipt(WorldDiplomacyFormAllianceExecutionStatus.PlayerAllianceLimitReached, command,
                    "diplomacy.form_alliance.player_limit_reached");
            }
            if (npcAllianceCount >= MaximumAllianceCount)
            {
                return Receipt(WorldDiplomacyFormAllianceExecutionStatus.NpcAllianceLimitReached, command,
                    "diplomacy.form_alliance.npc_limit_reached");
            }
        }
        catch
        {
            return Receipt(WorldDiplomacyFormAllianceExecutionStatus.RejectedBeforeStart, command,
                "diplomacy.form_alliance.validation_exception");
        }

        try
        {
            MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked(
                "diplomacy_form_alliance",
                () => alliance.StartAlliance(playerKingdom, npcKingdom));
            if (!alliance.IsAllyWithKingdom(playerKingdom, npcKingdom))
            {
                return Receipt(WorldDiplomacyFormAllianceExecutionStatus.ActionNotApplied, command,
                    "diplomacy.form_alliance.action_not_applied");
            }
            return Receipt(WorldDiplomacyFormAllianceExecutionStatus.Applied, command, "");
        }
        catch
        {
            return Receipt(WorldDiplomacyFormAllianceExecutionStatus.UnknownAfterStart, command,
                "diplomacy.form_alliance.action_exception");
        }
    }

    private static Kingdom ResolveKingdom(string kingdomId)
    {
        return Kingdom.All?.FirstOrDefault(kingdom => kingdom != null
            && string.Equals(kingdom.StringId, kingdomId, StringComparison.OrdinalIgnoreCase));
    }

    private static Hero ResolveHero(string heroId)
    {
        return Hero.Find(heroId) ?? Hero.FindFirst(hero => hero != null
            && string.Equals(hero.StringId, heroId, StringComparison.OrdinalIgnoreCase));
    }

    private static WorldDiplomacyFormAllianceExecutionReceipt Receipt(
        WorldDiplomacyFormAllianceExecutionStatus status,
        WorldDiplomacyFormAllianceCommand command,
        string errorCode)
    {
        return new WorldDiplomacyFormAllianceExecutionReceipt(
            status,
            command.PlayerKingdomId,
            command.NpcKingdomId,
            errorCode);
    }
}
