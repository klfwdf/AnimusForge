using System;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Library;

namespace AnimusForge.Refactor.Adapters;

public sealed class BannerlordWorldDiplomacyMakeTradeGameActionPort
    : IWorldDiplomacyMakeTradeGameActionPort
{
    public WorldDiplomacyMakeTradeExecutionReceipt Execute(WorldDiplomacyMakeTradeCommand command)
    {
        if (!command.IsValid)
        {
            return Receipt(WorldDiplomacyMakeTradeExecutionStatus.InvalidCommand, command, 0,
                "diplomacy.make_trade.invalid_command");
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
            return Receipt(WorldDiplomacyMakeTradeExecutionStatus.NotMainThread, command, 0,
                "diplomacy.make_trade.not_main_thread");
        }

        Kingdom playerKingdom;
        Kingdom npcKingdom;
        ITradeAgreementsCampaignBehavior trade;
        CampaignTime duration;
        try
        {
            ResolveKingdoms(command.PlayerKingdomId, command.NpcKingdomId,
                out playerKingdom, out npcKingdom);
            if (playerKingdom == null)
            {
                return Receipt(WorldDiplomacyMakeTradeExecutionStatus.PlayerKingdomUnavailable, command, 0,
                    "diplomacy.make_trade.player_kingdom_unavailable");
            }
            if (npcKingdom == null)
            {
                return Receipt(WorldDiplomacyMakeTradeExecutionStatus.NpcKingdomUnavailable, command, 0,
                    "diplomacy.make_trade.npc_kingdom_unavailable");
            }
            if (playerKingdom.IsEliminated)
            {
                return Receipt(WorldDiplomacyMakeTradeExecutionStatus.PlayerKingdomEliminated, command, 0,
                    "diplomacy.make_trade.player_kingdom_eliminated");
            }
            if (npcKingdom.IsEliminated)
            {
                return Receipt(WorldDiplomacyMakeTradeExecutionStatus.NpcKingdomEliminated, command, 0,
                    "diplomacy.make_trade.npc_kingdom_eliminated");
            }
            if (ReferenceEquals(playerKingdom, npcKingdom))
            {
                return Receipt(WorldDiplomacyMakeTradeExecutionStatus.SameKingdom, command, 0,
                    "diplomacy.make_trade.same_kingdom");
            }
            if (!ReferenceEquals(Clan.PlayerClan?.Kingdom, playerKingdom)
                || !ReferenceEquals(Hero.MainHero, playerKingdom.RulingClan?.Leader))
            {
                return Receipt(WorldDiplomacyMakeTradeExecutionStatus.PlayerUnauthorized, command, 0,
                    "diplomacy.make_trade.player_unauthorized");
            }

            Hero speaker = Hero.Find(command.SpeakerHeroId);
            if (speaker == null
                || !ReferenceEquals(speaker.Clan?.Kingdom, npcKingdom)
                || !ReferenceEquals(npcKingdom.RulingClan?.Leader, speaker))
            {
                return Receipt(WorldDiplomacyMakeTradeExecutionStatus.SpeakerUnauthorized, command, 0,
                    "diplomacy.make_trade.speaker_unauthorized");
            }

            trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
            if (trade == null)
            {
                return Receipt(WorldDiplomacyMakeTradeExecutionStatus.TradeBehaviorUnavailable, command, 0,
                    "diplomacy.make_trade.behavior_unavailable");
            }
            if (ReadTradeState(trade, playerKingdom, npcKingdom))
            {
                return Receipt(WorldDiplomacyMakeTradeExecutionStatus.AlreadyTrading, command, 0,
                    "diplomacy.make_trade.already_trading");
            }

            var tradeModel = Campaign.Current?.Models?.TradeAgreementModel;
            if (tradeModel == null)
            {
                return Receipt(WorldDiplomacyMakeTradeExecutionStatus.TradeModelUnavailable, command, 0,
                    "diplomacy.make_trade.model_unavailable");
            }
            string durationToken = command.DurationToken;
            if (durationToken.Equals("default", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(durationToken)
                || durationToken == "0")
            {
                duration = tradeModel.GetTradeAgreementDurationInYears(playerKingdom, npcKingdom);
            }
            else if (int.TryParse(durationToken, out int parsedDays))
            {
                duration = CampaignTime.Days(Math.Max(1, MBMath.ClampInt(parsedDays, 1, 252)));
            }
            else
            {
                duration = tradeModel.GetTradeAgreementDurationInYears(playerKingdom, npcKingdom);
            }
        }
        catch
        {
            return Receipt(WorldDiplomacyMakeTradeExecutionStatus.RejectedBeforeStart, command, 0,
                "diplomacy.make_trade.validation_exception");
        }

        int durationDays = (int)duration.ToDays;
        DiplomacyEffectReadback result = DiplomacyEffectReadback.Execute(
            () => MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked("diplomacy_make_trade", () => trade.MakeTradeAgreement(playerKingdom, npcKingdom, duration)),
            () => ReadTradeState(trade, playerKingdom, npcKingdom));
        return Receipt(!result.IsKnown ? WorldDiplomacyMakeTradeExecutionStatus.UnknownAfterStart
            : result.Applied ? WorldDiplomacyMakeTradeExecutionStatus.Applied
            : WorldDiplomacyMakeTradeExecutionStatus.ActionNotApplied, command, durationDays,
            result.Applied ? "" : "diplomacy.make_trade." + (result.IsKnown ? "action_not_applied" : "readback_unknown"));
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

    private static WorldDiplomacyMakeTradeExecutionReceipt Receipt(
        WorldDiplomacyMakeTradeExecutionStatus status,
        WorldDiplomacyMakeTradeCommand command,
        int durationDays,
        string errorCode)
    {
        return new WorldDiplomacyMakeTradeExecutionReceipt(
            status,
            command.PlayerKingdomId,
            command.NpcKingdomId,
            durationDays,
            errorCode);
    }
}
