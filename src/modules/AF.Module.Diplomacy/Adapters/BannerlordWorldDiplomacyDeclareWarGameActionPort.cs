using System;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Library;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Main-thread Bannerlord boundary for one validated declaration of war.
/// Live objects are resolved from stable IDs immediately before the action.
/// </summary>
public sealed class BannerlordWorldDiplomacyDeclareWarGameActionPort
    : IWorldDiplomacyDeclareWarGameActionPort
{
    public WorldDiplomacyDeclareWarExecutionReceipt Execute(WorldDiplomacyDeclareWarCommand command)
    {
        if (!command.IsValid)
        {
            return Receipt(WorldDiplomacyDeclareWarExecutionStatus.InvalidCommand, command,
                "diplomacy.declare_war.invalid_command");
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
            return Receipt(WorldDiplomacyDeclareWarExecutionStatus.NotMainThread, command,
                "diplomacy.declare_war.not_main_thread");
        }

        Kingdom declarer;
        Kingdom target;
        try
        {
            declarer = ResolveKingdom(command.DeclarerKingdomId);
            target = ResolveKingdom(command.TargetKingdomId);
            if (declarer == null)
            {
                return Receipt(WorldDiplomacyDeclareWarExecutionStatus.DeclarerUnavailable, command,
                    "diplomacy.declare_war.declarer_unavailable");
            }
            if (target == null)
            {
                return Receipt(WorldDiplomacyDeclareWarExecutionStatus.TargetUnavailable, command,
                    "diplomacy.declare_war.target_unavailable");
            }
            if (declarer.IsEliminated)
            {
                return Receipt(WorldDiplomacyDeclareWarExecutionStatus.DeclarerEliminated, command,
                    "diplomacy.declare_war.declarer_eliminated");
            }
            if (target.IsEliminated)
            {
                return Receipt(WorldDiplomacyDeclareWarExecutionStatus.TargetEliminated, command,
                    "diplomacy.declare_war.target_eliminated");
            }
            if (!HasCurrentAuthority(command, declarer, target))
            {
                return Receipt(WorldDiplomacyDeclareWarExecutionStatus.SpeakerUnauthorized, command,
                    "diplomacy.declare_war.speaker_unauthorized");
            }
            if (ReferenceEquals(declarer, target))
            {
                return Receipt(WorldDiplomacyDeclareWarExecutionStatus.SameKingdom, command,
                    "diplomacy.declare_war.same_kingdom");
            }
            if (FactionManager.IsAtWarAgainstFaction(declarer, target))
            {
                return Receipt(WorldDiplomacyDeclareWarExecutionStatus.AlreadyAtWar, command,
                    "diplomacy.declare_war.already_at_war");
            }

            IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
            if (alliance != null && alliance.IsAllyWithKingdom(declarer, target))
            {
                return Receipt(WorldDiplomacyDeclareWarExecutionStatus.Allied, command,
                    "diplomacy.declare_war.allied");
            }
        }
        catch
        {
            return Receipt(WorldDiplomacyDeclareWarExecutionStatus.FailedBeforeStart, command,
                "diplomacy.declare_war.validation_exception");
        }

        try
        {
            MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked(
                "diplomacy_declare_war",
                () => DeclareWarAction.ApplyByKingdomDecision(declarer, target));
            if (!DirectDiplomacyWarGuard.DidDeclarationTakeEffect(FactionManager.IsAtWarAgainstFaction(declarer, target)))
            {
                return Receipt(WorldDiplomacyDeclareWarExecutionStatus.ActionNotApplied, command,
                    "diplomacy.declare_war.action_not_applied");
            }
            return Receipt(WorldDiplomacyDeclareWarExecutionStatus.Applied, command, "");
        }
        catch
        {
            // The engine changes the stance before notifying OnWarDeclared observers.
            // A failing observer must not turn a confirmed effect into a lost receipt.
            try
            {
                return FactionManager.IsAtWarAgainstFaction(declarer, target)
                    ? Receipt(WorldDiplomacyDeclareWarExecutionStatus.Applied, command,
                        "diplomacy.declare_war.applied_after_observer_exception")
                    : Receipt(WorldDiplomacyDeclareWarExecutionStatus.ActionNotApplied, command,
                        "diplomacy.declare_war.action_exception_not_applied");
            }
            catch
            {
                // Keep uncertainty explicit when the engine cannot confirm its state.
            }
            return Receipt(WorldDiplomacyDeclareWarExecutionStatus.UnknownAfterStart, command,
                "diplomacy.declare_war.action_exception");
        }
    }

    private static bool HasCurrentAuthority(
        WorldDiplomacyDeclareWarCommand command,
        Kingdom declarer,
        Kingdom target)
    {
        Hero speaker = Hero.FindFirst(hero => hero != null
            && string.Equals(hero.StringId, command.SpeakerHeroId, StringComparison.OrdinalIgnoreCase));
        if (speaker == null)
        {
            return false;
        }

        if (command.DeclarerKind == WorldDiplomacyDeclareWarDeclarerKind.PlayerKingdom)
        {
            return DirectDiplomacyWarGuard.CanDeclareForPlayerKingdom(
                    ReferenceEquals(declarer.RulingClan?.Leader, Hero.MainHero),
                    ReferenceEquals(Clan.PlayerClan?.Kingdom, declarer))
                && ReferenceEquals(speaker.Clan?.Kingdom, target);
        }
        if (command.DeclarerKind != WorldDiplomacyDeclareWarDeclarerKind.NpcKingdom)
        {
            return false;
        }
        return ReferenceEquals(speaker.Clan?.Kingdom, declarer)
            && ReferenceEquals(declarer.RulingClan?.Leader, speaker);
    }

    private static Kingdom ResolveKingdom(string kingdomId)
    {
        return Kingdom.All?.FirstOrDefault(kingdom => kingdom != null
            && string.Equals(kingdom.StringId, kingdomId, StringComparison.OrdinalIgnoreCase));
    }

    private static WorldDiplomacyDeclareWarExecutionReceipt Receipt(
        WorldDiplomacyDeclareWarExecutionStatus status,
        WorldDiplomacyDeclareWarCommand command,
        string errorCode)
    {
        return new WorldDiplomacyDeclareWarExecutionReceipt(
            status,
            command.DeclarerKingdomId,
            command.TargetKingdomId,
            errorCode);
    }
}
