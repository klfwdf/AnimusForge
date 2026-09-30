using System;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Main-thread Bannerlord boundary for one independent player-clan peace command.
/// </summary>
public sealed class BannerlordWorldDiplomacyIndependentClanPeaceGameActionPort
    : IWorldDiplomacyIndependentClanPeaceGameActionPort
{
    private const string ActionSource = "diplomacy_independent_clan_peace";

    public WorldDiplomacyIndependentClanPeaceExecutionReceipt Execute(
        WorldDiplomacyIndependentClanPeaceCommand command)
    {
        if (!command.IsValid)
        {
            return Receipt(WorldDiplomacyIndependentClanPeaceExecutionStatus.InvalidCommand, command,
                "diplomacy.independent_clan_peace.invalid_command");
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
            return Receipt(WorldDiplomacyIndependentClanPeaceExecutionStatus.NotMainThread, command,
                "diplomacy.independent_clan_peace.not_main_thread");
        }

        Clan playerClan;
        Kingdom targetKingdom;
        Hero speaker;
        try
        {
            if (Campaign.Current == null || Hero.MainHero == null)
            {
                return Receipt(WorldDiplomacyIndependentClanPeaceExecutionStatus.RejectedBeforeStart, command,
                    "diplomacy.independent_clan_peace.campaign_unavailable");
            }

            playerClan = Clan.PlayerClan ?? Hero.MainHero.Clan;
            if (playerClan == null
                || !string.Equals(playerClan.StringId, command.PlayerClanId, StringComparison.OrdinalIgnoreCase))
            {
                return Receipt(WorldDiplomacyIndependentClanPeaceExecutionStatus.PlayerClanUnavailable, command,
                    "diplomacy.independent_clan_peace.player_clan_unavailable");
            }
            if (playerClan.IsEliminated)
            {
                return Receipt(WorldDiplomacyIndependentClanPeaceExecutionStatus.PlayerClanEliminated, command,
                    "diplomacy.independent_clan_peace.player_clan_eliminated");
            }
            if (playerClan.Kingdom != null
                || playerClan.IsUnderMercenaryService
                || (playerClan.Leader != null && !ReferenceEquals(playerClan.Leader, Hero.MainHero)))
            {
                return Receipt(WorldDiplomacyIndependentClanPeaceExecutionStatus.PlayerUnauthorized, command,
                    "diplomacy.independent_clan_peace.player_unauthorized");
            }

            speaker = Hero.Find(command.SpeakerHeroId);
            if (speaker == null)
            {
                return Receipt(WorldDiplomacyIndependentClanPeaceExecutionStatus.SpeakerUnavailable, command,
                    "diplomacy.independent_clan_peace.speaker_unavailable");
            }
            Clan targetClan = speaker.Clan;
            if (ReferenceEquals(speaker, Hero.MainHero)
                || speaker.IsDead
                || targetClan == null
                || ReferenceEquals(targetClan, playerClan)
                || targetClan.IsEliminated
                || targetClan.IsBanditFaction
                || targetClan.IsOutlaw)
            {
                return Receipt(WorldDiplomacyIndependentClanPeaceExecutionStatus.SpeakerUnauthorized, command,
                    "diplomacy.independent_clan_peace.speaker_unauthorized");
            }

            targetKingdom = targetClan.Kingdom ?? speaker.MapFaction as Kingdom;
            if (targetKingdom == null
                || !string.Equals(targetKingdom.StringId, command.TargetKingdomId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Receipt(WorldDiplomacyIndependentClanPeaceExecutionStatus.TargetKingdomUnavailable, command,
                    "diplomacy.independent_clan_peace.target_kingdom_unavailable");
            }
            if (targetKingdom.IsEliminated)
            {
                return Receipt(WorldDiplomacyIndependentClanPeaceExecutionStatus.TargetKingdomEliminated, command,
                    "diplomacy.independent_clan_peace.target_kingdom_eliminated");
            }
            if (!ReferenceEquals(targetKingdom.RulingClan?.Leader, speaker))
            {
                return Receipt(WorldDiplomacyIndependentClanPeaceExecutionStatus.SpeakerUnauthorized, command,
                    "diplomacy.independent_clan_peace.speaker_unauthorized");
            }
            if (!FactionManager.IsAtWarAgainstFaction(playerClan, targetKingdom))
            {
                return Receipt(WorldDiplomacyIndependentClanPeaceExecutionStatus.NotAtWar, command,
                    "diplomacy.independent_clan_peace.not_at_war");
            }
            if (FactionManager.IsAtConstantWarAgainstFaction(playerClan, targetKingdom))
            {
                return Receipt(WorldDiplomacyIndependentClanPeaceExecutionStatus.ConstantWar, command,
                    "diplomacy.independent_clan_peace.constant_war");
            }
        }
        catch
        {
            return Receipt(WorldDiplomacyIndependentClanPeaceExecutionStatus.RejectedBeforeStart, command,
                "diplomacy.independent_clan_peace.validation_exception");
        }

        DiplomacyEffectReadback result = DiplomacyEffectReadback.Execute(
            () => MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked(ActionSource, () => MakePeaceAction.Apply(playerClan, targetKingdom)),
            () => !FactionManager.IsAtWarAgainstFaction(playerClan, targetKingdom));
        if (result.Applied)
        {
            try { DiplomacyRecentPeaceGuard.RegisterPeace(playerClan, targetKingdom, ActionSource); }
            catch { /* Peace was confirmed independently of guard registration. */ }
        }
        return Receipt(!result.IsKnown ? WorldDiplomacyIndependentClanPeaceExecutionStatus.UnknownAfterStart
            : result.Applied ? WorldDiplomacyIndependentClanPeaceExecutionStatus.Applied
            : WorldDiplomacyIndependentClanPeaceExecutionStatus.ActionNotApplied, command,
            result.Applied ? "" : "diplomacy.independent_clan_peace." + (result.IsKnown ? "action_not_applied" : "readback_unknown"));
    }

    private static WorldDiplomacyIndependentClanPeaceExecutionReceipt Receipt(
        WorldDiplomacyIndependentClanPeaceExecutionStatus status,
        WorldDiplomacyIndependentClanPeaceCommand command,
        string errorCode)
    {
        return new WorldDiplomacyIndependentClanPeaceExecutionReceipt(
            status,
            command.PlayerClanId,
            command.TargetKingdomId,
            command.SpeakerHeroId,
            errorCode);
    }
}
