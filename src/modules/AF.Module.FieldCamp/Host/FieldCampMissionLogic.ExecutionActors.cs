using System;
using System.Collections.Generic;
using RichExecutions.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class FieldCampMissionLogic
{
    private sealed class CampCeremonyActor
    {
        internal Agent Agent;
        internal CampAgent Camp;
        internal bool Prisoner;
        internal Guid Session;
        internal string Role;
        internal Team Team;
        internal Formation Formation;
        internal Agent.MortalityState Mortality;
        internal AgentFlag Flags;
        internal MissionWeapon[] Weapons;
        internal Vec3 Home;
        internal Vec2 HomeFacing;
        internal Vec3 Target;
        internal Vec2 Facing;
        internal bool Arrived = true;
        internal bool Failed;
    }

    private readonly Dictionary<string, CampCeremonyActor> _campCeremonyRoles = new Dictionary<string, CampCeremonyActor>(StringComparer.Ordinal);
    private readonly Dictionary<Agent, CampCeremonyActor> _campCeremonyActors = new Dictionary<Agent, CampCeremonyActor>();

    private static Agent AcquireCampCeremonyActorHook(Guid session, string role, Hero victim) => Current?.AcquireCampCeremonyActor(session, role, victim);
    private static bool OwnsCampVictimOriginHook(Agent agent) => Current is FieldCampMissionLogic camp &&
        camp._campCeremonyActors.TryGetValue(agent, out CampCeremonyActor actor) && actor.Prisoner && agent.Origin is PrisonerAgentOrigin;
    private static bool CampPrisonerPresentHook(Hero hero) => Current is FieldCampMissionLogic camp &&
        camp._prisoners.Exists(agent => agent != null && agent.IsActive() && agent.Character == hero?.CharacterObject);
    private static bool WalkCampActorHook(Guid session, Agent agent, Vec3 target, Vec2 facing, IReadOnlyList<Vec3> approach) =>
        Current?.WalkCampCeremonyActor(session, agent, target, facing, approach) == true;
    private static FieldCampCeremonyReadiness CampActorReadinessHook(Guid session) =>
        Current?.CampActorsReady(session) ?? FieldCampCeremonyReadiness.Failed;
    private static void ReleaseCampActorsHook() => Current?.ReleaseCampCeremonyActors();

    private Agent AcquireCampCeremonyActor(Guid session, string role, Hero victim)
    {
        if (_campCeremonyRoles.TryGetValue(role, out CampCeremonyActor previous))
        {
            if (previous.Agent.IsActive() && (!previous.Prisoner || previous.Agent.Character == victim?.CharacterObject))
            { previous.Session = session; previous.Failed = false; return previous.Agent; }
            _campCeremonyActors.Remove(previous.Agent);
            _campCeremonyRoles.Remove(role);
        }
        Agent chosen = null;
        CampAgent entry = null;
        bool prisoner = string.Equals(role, "victim", StringComparison.Ordinal);
        if (prisoner)
        {
            for (int i = 0; i < _prisoners.Count; i++)
                if (_prisoners[i].IsActive() && _prisoners[i].Character == victim?.CharacterObject)
                { chosen = _prisoners[i]; break; }
        }
        else
        {
            float best = float.MaxValue;
            for (int i = 0; i < _agents.Count; i++)
            {
                CampAgent candidate = _agents[i]; Agent agent = candidate.Agent;
                if (candidate.FollowingPlayer || agent == null || !agent.IsActive() || agent.Character?.IsHero != false ||
                    _campCeremonyActors.ContainsKey(agent) || IsSceneConversationHeld(agent) || IsDuelist(agent) ||
                    candidate.Activity == Activity.Wounded || candidate.Activity == Activity.Sentry || candidate.Activity == Activity.Post) continue;
                float score = agent.Position.AsVec2.DistanceSquared(_ceremonyOrigin.AsVec2);
                if (role.StartsWith("shooter", StringComparison.Ordinal) && candidate.TroopClass != FormationClass.Ranged) score += 10000f;
                if (score >= best) continue;
                best = score; chosen = agent; entry = candidate;
            }
        }
        if (chosen == null || IsSceneConversationHeld(chosen)) return null;
        if (_ceremonyMoves.TryGetValue(chosen, out CeremonyMove clearance)) RemoveCeremonyMove(clearance);
        var actor = new CampCeremonyActor
        {
            Agent = chosen, Camp = entry, Prisoner = prisoner, Session = session, Role = role,
            Team = chosen.Team, Formation = chosen.Formation, Mortality = chosen.CurrentMortalityState, Flags = chosen.GetAgentFlags(),
            Home = entry != null ? entry.Home : chosen.Position,
            HomeFacing = entry != null ? entry.HomeFacing : chosen.LookDirection.AsVec2,
            Weapons = new MissionWeapon[(int)EquipmentIndex.NumAllWeaponSlots]
        };
        for (int i = (int)EquipmentIndex.WeaponItemBeginSlot; i < actor.Weapons.Length; i++) actor.Weapons[i] = chosen.Equipment[(EquipmentIndex)i];
        StopCampNavigation(chosen);
        if (entry != null) ReleasePose(entry);
        chosen.SetActionChannel(0, in ActionIndexCache.act_none, true);
        chosen.SetIsAIPaused(true);
        _campCeremonyRoles.Add(role, actor); _campCeremonyActors.Add(chosen, actor);
        Logger.Log(LogSource, "borrowed existing camp actor role=" + role + " agent=" + chosen.Index + " session=" + session);
        return chosen;
    }

    // Retain the existing same-DLL hook signature; ceremony personnel now
    // arrive immediately, while spectators use their independent walking queue.
    private bool WalkCampCeremonyActor(Guid session, Agent agent, Vec3 target, Vec2 facing, IReadOnlyList<Vec3> approach)
    {
        if (!_campCeremonyActors.TryGetValue(agent, out CampCeremonyActor actor) || actor.Session != session ||
            !agent.IsActive() || !target.IsValid || !facing.IsNonZero()) return false;
        actor.Target = target; actor.Facing = facing; actor.Arrived = false; actor.Failed = false;
        StopCampNavigation(agent);
        agent.SetIsAIPaused(true);
        agent.Controller = AgentControllerType.None;
        agent.SetActionChannel(0, in ActionIndexCache.act_none, true);
        agent.TeleportToPosition(target);
        HoldCampCeremonyActor(actor);
        Logger.Log(LogSource, "positioned existing camp ceremony actor role=" + actor.Role + " agent=" + agent.Index + " session=" + session);
        return true;
    }

    private void HoldCampCeremonyActor(CampCeremonyActor actor)
    {
        StopCampNavigation(actor.Agent);
        actor.Agent.SetIsAIPaused(true);
        actor.Agent.Controller = AgentControllerType.None;
        actor.Agent.LookDirection = new Vec3(actor.Facing.x, actor.Facing.y, 0f);
        actor.Arrived = true;
    }

    private FieldCampCeremonyReadiness CampActorsReady(Guid session)
    {
        bool waiting = false;
        foreach (CampCeremonyActor actor in _campCeremonyRoles.Values)
        {
            if (actor.Session != session) continue;
            if (actor.Failed || !actor.Agent.IsActive()) return FieldCampCeremonyReadiness.Failed;
            if (!actor.Arrived) waiting = true;
        }
        return waiting ? FieldCampCeremonyReadiness.Pending : FieldCampCeremonyReadiness.Ready;
    }

    private void ReleaseCampCeremonyActors()
    {
        foreach (CampCeremonyActor actor in _campCeremonyRoles.Values)
        {
            Agent agent = actor.Agent;
            if (!agent.IsActive() || VengeanceIntegration.IsExecutedVictim(agent)) continue;
            StopCampNavigation(agent); agent.SetIsAIPaused(false); agent.Controller = AgentControllerType.AI;
            agent.SetTeam(actor.Team, false); agent.Formation = actor.Formation;
            agent.SetMortalityState(actor.Mortality); agent.SetAgentFlags(actor.Flags);
            for (int i = (int)EquipmentIndex.WeaponItemBeginSlot; i < actor.Weapons.Length; i++)
            {
                var slot = (EquipmentIndex)i; agent.RemoveEquippedWeapon(slot); MissionWeapon weapon = actor.Weapons[i];
                if (!weapon.IsEmpty) agent.EquipWeaponWithNewEntity(slot, ref weapon);
            }
            agent.UpdateWeapons(); ApplyCampActionSet(agent);
            if (Mission.IsMissionEnding) continue;
            var move = new CeremonyMove { Agent = agent, Camp = actor.Camp, Prisoner = actor.Prisoner,
                Home = actor.Home, HomeFacing = actor.HomeFacing, Goal = actor.Home, Facing = actor.HomeFacing };
            if (!_ceremonyMoves.ContainsKey(agent)) { _ceremonyWalkers.Add(move); _ceremonyMoves.Add(agent, move); }
            if (actor.Camp != null && !_ceremonyMoved.Contains(actor.Camp)) _ceremonyMoved.Add(actor.Camp);
        }
        _campCeremonyActors.Clear(); _campCeremonyRoles.Clear();
        if (!_ceremonyActive && _ceremonyWalkers.Count > 0) _ceremonyReturnDeadline = _elapsed + CeremonyReturnGiveUpSeconds;
    }
}
