using System;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Library;

namespace AnimusForge.Refactor.Adapters;

public sealed class BannerlordWorldDiplomacyCancelTradeGameActionPort
    : IWorldDiplomacyCancelTradeGameActionPort
{
    public WorldDiplomacyCancelTradeExecutionReceipt Execute(WorldDiplomacyCancelTradeCommand command)
    {
        if (!command.IsValid)
        {
            return Receipt(WorldDiplomacyCancelTradeExecutionStatus.InvalidCommand, command,
                "diplomacy.cancel_trade.invalid_command");
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
            return Receipt(WorldDiplomacyCancelTradeExecutionStatus.NotMainThread, command,
                "diplomacy.cancel_trade.not_main_thread");
        }

        Kingdom playerKingdom;
        Kingdom npcKingdom;
        ITradeAgreementsCampaignBehavior trade;
        try
        {
            ResolveKingdoms(command.PlayerKingdomId, command.NpcKingdomId,
                out playerKingdom, out npcKingdom);
            if (playerKingdom == null)
            {
                return Receipt(WorldDiplomacyCancelTradeExecutionStatus.PlayerKingdomUnavailable, command,
                    "diplomacy.cancel_trade.player_kingdom_unavailable");
            }
            if (npcKingdom == null)
            {
                return Receipt(WorldDiplomacyCancelTradeExecutionStatus.NpcKingdomUnavailable, command,
                    "diplomacy.cancel_trade.npc_kingdom_unavailable");
            }
            if (playerKingdom.IsEliminated)
            {
                return Receipt(WorldDiplomacyCancelTradeExecutionStatus.PlayerKingdomEliminated, command,
                    "diplomacy.cancel_trade.player_kingdom_eliminated");
            }
            if (ReferenceEquals(playerKingdom, npcKingdom))
            {
                return Receipt(WorldDiplomacyCancelTradeExecutionStatus.SameKingdom, command,
                    "diplomacy.cancel_trade.same_kingdom");
            }
            if (!ReferenceEquals(Clan.PlayerClan?.Kingdom, playerKingdom))
            {
                return Receipt(WorldDiplomacyCancelTradeExecutionStatus.PlayerUnauthorized, command,
                    "diplomacy.cancel_trade.player_unauthorized");
            }

            Hero speaker = Hero.Find(command.SpeakerHeroId);
            if (speaker == null || !ReferenceEquals(speaker.Clan?.Kingdom, npcKingdom))
            {
                return Receipt(WorldDiplomacyCancelTradeExecutionStatus.SpeakerUnauthorized, command,
                    "diplomacy.cancel_trade.speaker_unauthorized");
            }

            trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
            if (trade == null)
            {
                return Receipt(WorldDiplomacyCancelTradeExecutionStatus.TradeBehaviorUnavailable, command,
                    "diplomacy.cancel_trade.behavior_unavailable");
            }
            if (!ReadTradeState(trade, playerKingdom, npcKingdom))
            {
                return Receipt(WorldDiplomacyCancelTradeExecutionStatus.NotTrading, command,
                    "diplomacy.cancel_trade.not_trading");
            }
        }
        catch
        {
            return Receipt(WorldDiplomacyCancelTradeExecutionStatus.RejectedBeforeStart, command,
                "diplomacy.cancel_trade.validation_exception");
        }

        DiplomacyEffectReadback result = DiplomacyEffectReadback.Execute(
            () => MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked("diplomacy_cancel_trade", () => trade.EndTradeAgreement(playerKingdom, npcKingdom)),
            () => !ReadTradeState(trade, playerKingdom, npcKingdom));
        return Receipt(!result.IsKnown ? WorldDiplomacyCancelTradeExecutionStatus.UnknownAfterStart
            : result.Applied ? WorldDiplomacyCancelTradeExecutionStatus.Applied
            : WorldDiplomacyCancelTradeExecutionStatus.ActionNotApplied, command,
            result.Applied ? "" : "diplomacy.cancel_trade." + (result.IsKnown ? "action_not_applied" : "readback_unknown"));
    }

    private static bool ReadTradeState(ITradeAgreementsCampaignBehavior trade, Kingdom first, Kingdom second)
    {
        if (!BannerlordApiCompat.TryGetTradeAgreementState(trade, first, second, out bool active))
            throw new InvalidOperationException("Trade agreement state unavailable");
        return active;
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

    private static WorldDiplomacyCancelTradeExecutionReceipt Receipt(
        WorldDiplomacyCancelTradeExecutionStatus status,
        WorldDiplomacyCancelTradeCommand command,
        string errorCode)
    {
        return new WorldDiplomacyCancelTradeExecutionReceipt(
            status,
            command.PlayerKingdomId,
            command.NpcKingdomId,
            errorCode);
    }
}
