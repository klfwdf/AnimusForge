using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Adapters;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

public sealed partial class WorldMapPartyCommandBehavior
{
    // Click-only, main-thread snapshots. No new Tick work, saved state, IDs or public API.
    internal sealed class PlayerDirectOrderActor
    {
        internal Campaign Campaign;
        internal WorldMapPartyCommandBehavior Owner;
        internal Hero Hero;
        internal CharacterObject Character;
        internal int AgentIndex;
        internal MobileParty Party;
        internal Clan Clan;
        internal Town Governor;
        internal bool InMainParty;
        internal string InitialReason;
    }

    internal sealed class PlayerDirectOrderTarget
    {
        internal string Id, Type, DisplayType, Name;
        internal int DefaultDays;
        internal Func<bool> IsCurrent;
    }

    private static string DirectOrderText(string key, string fallback) =>
        ConversationUiLocalization.Text("AF_CONV_UI_Direct_Map_" + key, fallback);

    internal static PlayerDirectOrderActor CapturePlayerDirectOrderActor(Hero hero, CharacterObject character, int agentIndex)
    {
        hero = hero ?? character?.HeroObject;
        MobileParty party = hero?.PartyBelongedTo;
        if (hero == null) TryResolveNonHeroPartyActorForExternal(character, agentIndex, out party);
        var actor = new PlayerDirectOrderActor {
            Campaign = Campaign.Current, Owner = Instance ?? Campaign.Current?.GetCampaignBehavior<WorldMapPartyCommandBehavior>(),
            Hero = hero, Character = character, AgentIndex = agentIndex, Party = party, Clan = hero?.Clan,
            Governor = hero?.GovernorOf, InMainParty = hero != null && IsHeroActuallyInPlayerMainPartyRoster(hero)
        };
        actor.InitialReason = GetPlayerDirectOrderUnavailableReason(actor, "GO_TO_SETTLEMENT");
        return actor;
    }

    private static bool IsPlayerDirectOrderActorCurrent(PlayerDirectOrderActor actor)
    {
        if (actor == null || actor.Campaign == null || !ReferenceEquals(actor.Campaign, Campaign.Current)
            || actor.Owner == null || !ReferenceEquals(actor.Owner, Instance ?? Campaign.Current?.GetCampaignBehavior<WorldMapPartyCommandBehavior>())) return false;
        if (actor.Hero != null)
            return ReferenceEquals(actor.Hero, actor.Character?.HeroObject ?? actor.Hero)
                && ReferenceEquals(actor.Party, actor.Hero.PartyBelongedTo) && ReferenceEquals(actor.Clan, actor.Hero.Clan)
                && ReferenceEquals(actor.Governor, actor.Hero.GovernorOf)
                && actor.InMainParty == IsHeroActuallyInPlayerMainPartyRoster(actor.Hero);
        return TryResolveNonHeroPartyActorForExternal(actor.Character, actor.AgentIndex, out MobileParty party) && ReferenceEquals(party, actor.Party);
    }

    internal static string GetPlayerDirectOrderUnavailableReason(PlayerDirectOrderActor actor, string kind)
    {
        if (!IsPlayerDirectOrderActorCurrent(actor)) return DirectOrderText("ActorUnavailable", "The world-map owner or actor's party source is unavailable or has changed.");
        Hero hero = actor.Hero;
        if (hero == Hero.MainHero || (hero != null && (hero.IsDead || !hero.IsActive || hero.IsDisabled || hero.IsPrisoner || hero.IsTraveling || hero.IsNotable))
            || PromptRuleCaptureBannerlordAdapter.ShouldExcludeWorldMapCommandRuleForTarget(hero, actor.Character, actor.AgentIndex))
            return DirectOrderText("ActorIneligible", "The current character cannot receive world-map party commands under the original rules.");
        if (kind == "MERGE_TO_PLAYER")
            return hero != null && TryResolveMergeToPlayerPostprocessVariant(hero, out _)
                && actor.Owner.CanMergeToPlayer(hero, ResolveActorParty(hero), out _) ? null
                : DirectOrderText("MergeIneligible", "The original return-to-player mechanic is not available for this actor.");
        if (hero == null) return IsValidNonHeroPartyFallbackParty(actor.Party) ? null
            : DirectOrderText("NoParty", "The current NPC has no valid controllable world-map party.");
        if (actor.InMainParty) return null; // The original owner opens its companion task-party screen.
        if (hero.GovernorOf != null)
        {
            if (kind == "STOP") return null; // Also cancels a pending expedition request.
            return TryValidateGovernorExpeditionCandidate(hero, out _, out _, out _, out string reason) ? null : reason;
        }
        return ResolveActorParty(hero) != null ? null : DirectOrderText("NoParty", "The current NPC has no valid controllable world-map party.");
    }

    // Validate the entire STOP + command request before entering admission: admission's STOP
    // is intentionally not transactional. An invalid target must never clear an old queue.
    internal static string ValidatePlayerDirectOrder(PlayerDirectOrderActor actor, string raw)
    {
        var matches = WorldMapOrderTagRegex.Matches(raw ?? "");
        if (matches.Count == 0 || matches.Count > 2 || WorldMapOrderTagRegex.Replace(raw ?? "", "").Trim().Length != 0)
            return DirectOrderText("InvalidOrder", "The world-map command parameters are invalid. No task was changed.");
        PartyCommandEntry command = null;
        bool leadingStop = false;
        for (int i = 0; i < matches.Count; i++)
        {
            if (!TryParseTag(matches[i].Value, validateTargets: true, out PartyCommandEntry parsed, out bool stop)
                || (stop && i != 0) || (!stop && command != null))
                return DirectOrderText("InvalidOrder", "The world-map command parameters are invalid. No task was changed.");
            if (stop) leadingStop = true;
            else command = parsed;
        }
        if (matches.Count == 2 && (!leadingStop || command == null))
            return DirectOrderText("InvalidOrder", "The world-map command parameters are invalid. No task was changed.");
        string kind = command == null ? "STOP" : IsKind(command, CommandKind.MergeToPlayer) ? "MERGE_TO_PLAYER" : "GO_TO_SETTLEMENT";
        string unavailable = GetPlayerDirectOrderUnavailableReason(actor, kind);
        if (!string.IsNullOrEmpty(unavailable)) return unavailable;
        if (command != null && (!IsExecutableCommand(command)
            || (IsKind(command, CommandKind.ClearHideout) && !CanOfferHideoutClearTarget(ResolveSettlementById(command.TargetId)))))
            return DirectOrderText("TargetUnavailable", "The selected destination or hideout is no longer eligible. No task was changed.");
        MobileParty taskParty = actor.InMainParty ? null : actor.Party; // Implicit task-party creation changes the execution source.
        if (command != null && (IsKind(command, CommandKind.FollowHero) || IsKind(command, CommandKind.AttackHero)) && command.TargetType == "hero")
        {
            Hero targetHero = ResolveHeroById(command.TargetId);
            if (targetHero == actor.Hero || !IsDirectOrderHeroTarget(targetHero, taskParty))
                return DirectOrderText("TargetUnavailable", "The selected destination or hideout is no longer eligible. No task was changed.");
        }
        if (command != null && command.TargetType == "party")
        {
            MobileParty targetParty = ResolveMobilePartyById(command.TargetId);
            if (!IsPartyUsable(targetParty) || targetParty == taskParty)
                return DirectOrderText("TargetUnavailable", "The selected destination or hideout is no longer eligible. No task was changed.");
        }
        PartyCommandQueueState state = null;
        string key = actor.Hero?.StringId ?? BuildPartyActorKey(actor.Party, createGuid: false);
        lock (actor.Owner._queueLock)
            if (!string.IsNullOrWhiteSpace(key)) actor.Owner._queues.TryGetValue(key, out state);
        if (!string.IsNullOrWhiteSpace(NormalizeExternalSourceId(state?.SourceId)))
            return DirectOrderText("Protected", "This queue belongs to a protected external order source. No task was changed.");
        if (command != null && IsStopPending(state))
            return DirectOrderText("SafeExit", "The old task is waiting for a safe exit. Retry after it finishes; the old queue was preserved.");
        if (leadingStop && command != null && (HasActiveMapEvent(actor.Party) || HasFollowSiegeState(state)
            || MustWaitForNpcHideoutBattle(actor.Party, state)))
            return DirectOrderText("BattleReplace", "The old task is in battle or siege control. Replacement was refused and the old queue was preserved.");
        return null;
    }

    private static bool IsDirectOrderHeroTarget(Hero hero, MobileParty actorParty)
    {
        MobileParty party = hero == Hero.MainHero ? MobileParty.MainParty : hero?.PartyBelongedTo;
        return hero != null && hero.IsActive && !hero.IsDead && !hero.IsPrisoner && IsPartyUsable(party) && party != actorParty;
    }

    internal static List<PlayerDirectOrderTarget> CapturePlayerDirectOrderTargets(PlayerDirectOrderActor actor, string kind, bool playerOnly = false)
    {
        var targets = new List<PlayerDirectOrderTarget>();
        if (!IsPlayerDirectOrderActorCurrent(actor)) return targets;
        MobileParty taskParty = actor.InMainParty ? null : actor.Party;
        void AddHero(Hero hero)
        {
            if (hero == actor.Hero || !IsDirectOrderHeroTarget(hero, taskParty) || !WorldMapOrderCoordinator.IsSafeIdentifier(hero.StringId) || hero.StringId.Contains(":")) return;
            string id = hero.StringId;
            MobileParty party = hero == Hero.MainHero ? MobileParty.MainParty : hero.PartyBelongedTo;
            targets.Add(new PlayerDirectOrderTarget {
                Id = id, Type = "hero", Name = hero.Name?.ToString() ?? id, DefaultDays = DefaultHeroAttackDays,
                IsCurrent = () => ReferenceEquals(hero, ResolveHeroById(id)) && IsDirectOrderHeroTarget(hero, taskParty)
                    && ReferenceEquals(party, hero == Hero.MainHero ? MobileParty.MainParty : hero.PartyBelongedTo)
            });
        }
        if (playerOnly) { AddHero(Hero.MainHero); return targets; }
        if (kind == "FOLLOW" || kind == "ATTACK")
        {
            foreach (Hero hero in Hero.AllAliveHeroes) AddHero(hero);
            foreach (MobileParty party in MobileParty.All)
            {
                if (!IsPartyUsable(party) || party == taskParty || !WorldMapOrderCoordinator.IsSafeIdentifier(party.StringId) || party.StringId.Contains(":")) continue;
                string id = party.StringId;
                targets.Add(new PlayerDirectOrderTarget {
                    Id = id, Type = "party", Name = party.Name?.ToString() ?? id, DefaultDays = DefaultHeroAttackDays,
                    IsCurrent = () => ReferenceEquals(party, ResolveMobilePartyById(id)) && IsPartyUsable(party) && party != taskParty
                });
            }
        }
        if (kind != "FOLLOW")
        {
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null || !WorldMapOrderCoordinator.IsSafeIdentifier(settlement.StringId) || settlement.StringId.Contains(":")
                    || (kind == "ATTACK" && !IsSupportedAttackSettlement(settlement))
                    || (kind == "CLEAR_HIDEOUT" && !CanOfferHideoutClearTarget(settlement))
                    || (settlement.IsHideout && !BannerlordApiCompat.IsHideoutSpotted(settlement.Hideout))) continue;
                string id = settlement.StringId;
                targets.Add(new PlayerDirectOrderTarget {
                    Id = id, Type = "settlement", DisplayType = settlement.IsTown ? "TownType" : settlement.IsCastle ? "CastleType"
                        : settlement.IsVillage ? "VillageType" : settlement.IsHideout ? "HideoutType" : "LocationType", Name = settlement.Name?.ToString() ?? id,
                    DefaultDays = kind == "ATTACK" ? GetDefaultAttackDaysForSettlement(settlement) : kind == "CLEAR_HIDEOUT" ? NpcHideoutClearPolicy.DefaultDays : 1,
                    IsCurrent = () => ReferenceEquals(settlement, ResolveSettlementById(id))
                        && (kind != "CLEAR_HIDEOUT" || CanOfferHideoutClearTarget(settlement))
                        && (kind != "ATTACK" || IsSupportedAttackSettlement(settlement))
                        && (!settlement.IsHideout || BannerlordApiCompat.IsHideoutSpotted(settlement.Hideout))
                });
            }
        }
        return targets.OrderBy(x => x.Name, StringComparer.CurrentCulture).ThenBy(x => x.Type, StringComparer.Ordinal)
            .ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
    }
}
