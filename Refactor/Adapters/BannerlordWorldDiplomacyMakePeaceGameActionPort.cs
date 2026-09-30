using System;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Main-thread Bannerlord boundary for one oral kingdom peace command.
/// </summary>
public sealed class BannerlordWorldDiplomacyMakePeaceGameActionPort
    : IWorldDiplomacyMakePeaceGameActionPort
{
    public WorldDiplomacyMakePeaceExecutionReceipt Execute(WorldDiplomacyMakePeaceCommand command)
    {
        if (!command.IsValid)
        {
            return Receipt(WorldDiplomacyMakePeaceExecutionStatus.InvalidCommand, command,
                "diplomacy.make_peace.invalid_command");
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
            return Receipt(WorldDiplomacyMakePeaceExecutionStatus.NotMainThread, command,
                "diplomacy.make_peace.not_main_thread");
        }

        Kingdom payer;
        Kingdom receiver;
        int requestedTribute;
        int requestedDuration;
        try
        {
            payer = ResolveKingdom(command.PayerKingdomId);
            receiver = ResolveKingdom(command.ReceiverKingdomId);
            if (payer == null)
            {
                return Receipt(WorldDiplomacyMakePeaceExecutionStatus.PayerUnavailable, command,
                    "diplomacy.make_peace.payer_unavailable");
            }
            if (receiver == null)
            {
                return Receipt(WorldDiplomacyMakePeaceExecutionStatus.ReceiverUnavailable, command,
                    "diplomacy.make_peace.receiver_unavailable");
            }
            if (payer.IsEliminated)
            {
                return Receipt(WorldDiplomacyMakePeaceExecutionStatus.PayerEliminated, command,
                    "diplomacy.make_peace.payer_eliminated");
            }
            if (receiver.IsEliminated)
            {
                return Receipt(WorldDiplomacyMakePeaceExecutionStatus.ReceiverEliminated, command,
                    "diplomacy.make_peace.receiver_eliminated");
            }
            if (ReferenceEquals(payer, receiver))
            {
                return Receipt(WorldDiplomacyMakePeaceExecutionStatus.SameKingdom, command,
                    "diplomacy.make_peace.same_kingdom");
            }

            Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
            if (playerKingdom == null
                || playerKingdom.IsEliminated
                || !ReferenceEquals(Hero.MainHero, playerKingdom.RulingClan?.Leader)
                || (!ReferenceEquals(playerKingdom, payer) && !ReferenceEquals(playerKingdom, receiver)))
            {
                return Receipt(WorldDiplomacyMakePeaceExecutionStatus.PlayerUnauthorized, command,
                    "diplomacy.make_peace.player_unauthorized");
            }

            Kingdom npcKingdom = ReferenceEquals(playerKingdom, payer) ? receiver : payer;
            Hero speaker = ResolveHero(command.SpeakerHeroId);
            if (speaker == null
                || !ReferenceEquals(speaker.Clan?.Kingdom, npcKingdom)
                || !ReferenceEquals(npcKingdom.RulingClan?.Leader, speaker))
            {
                return Receipt(WorldDiplomacyMakePeaceExecutionStatus.SpeakerUnauthorized, command,
                    "diplomacy.make_peace.speaker_unauthorized");
            }
            if (!FactionManager.IsAtWarAgainstFaction(payer, receiver))
            {
                return Receipt(WorldDiplomacyMakePeaceExecutionStatus.NotAtWar, command,
                    "diplomacy.make_peace.not_at_war");
            }

            requestedTribute = DiplomacyPeaceTermsService.ResolveTributeAmount(
                command.AmountToken,
                payer,
                receiver);
            if (requestedTribute < 0)
            {
                return Receipt(WorldDiplomacyMakePeaceExecutionStatus.InvalidTerms, command,
                    "diplomacy.make_peace.invalid_tribute");
            }
            requestedDuration = DiplomacyPeaceTermsService.ResolveDurationDays(
                command.DurationToken,
                requestedTribute > 0);
        }
        catch
        {
            return Receipt(WorldDiplomacyMakePeaceExecutionStatus.RejectedBeforeStart, command,
                "diplomacy.make_peace.validation_exception");
        }

        DiplomacyPeaceEffectReceipt result = DiplomacyPeaceTermsService.ApplyPeace(
            payer, receiver, requestedTribute, requestedDuration, "diplomacy_make_peace");
        return new WorldDiplomacyMakePeaceExecutionReceipt(
            result.Complete ? WorldDiplomacyMakePeaceExecutionStatus.Applied
                : result.PeaceApplied ? WorldDiplomacyMakePeaceExecutionStatus.PartiallyApplied
                : result.PeaceKnown ? WorldDiplomacyMakePeaceExecutionStatus.ActionNotApplied
                : WorldDiplomacyMakePeaceExecutionStatus.UnknownAfterStart,
            command.PayerKingdomId, command.ReceiverKingdomId,
            result.ActualDailyTribute, result.ActualDurationDays, result.Diagnostic);
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

    private static WorldDiplomacyMakePeaceExecutionReceipt Receipt(
        WorldDiplomacyMakePeaceExecutionStatus status,
        WorldDiplomacyMakePeaceCommand command,
        string errorCode)
    {
        return new WorldDiplomacyMakePeaceExecutionReceipt(
            status,
            command.PayerKingdomId,
            command.ReceiverKingdomId,
            0,
            0,
            errorCode);
    }
}
