using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class VillageAftermathBehavior
{
    private static Team _villageVictimTeam;
    private static readonly Dictionary<Agent, Team> VillageVictims = new Dictionary<Agent, Team>();
    private static readonly List<Agent> VillageHunters = new List<Agent>();
    private static readonly HashSet<int> VillageDeathReceipts = new HashSet<int>();
    private static float _nextVillageMassacreTick;

    private static void StartVillageMassacre()
    {
        if (!IsActive() || _villageVictimTeam != null) return;
        var mission = _activeMission; var player = mission.PlayerTeam ?? mission.MainAgent?.Team;
        if (player == null) return;
        VillageQuestionOrders.Clear();
        _villageVictimTeam = mission.Teams.Add(BattleSideEnum.Defender, 0xFF7A2020u, 0xFF2A0808u, null, false, false);
        _villageVictimTeam.SetIsEnemyOf(player, true); player.SetIsEnemyOf(_villageVictimTeam, true);
        foreach (var agent in mission.Agents)
        {
            if (!IsLiveHostileSpeaker(agent)) continue;
            var role = ResolveHostileSpeakerRole(agent.Index, out _);
            if (role == VillageDispositionSpeakerRole.Villager || role == VillageDispositionSpeakerRole.VillageNotable)
            { VillageVictims[agent] = agent.Team; agent.SetTeam(_villageVictimTeam, true); }
            else if (role == VillageDispositionSpeakerRole.PlayerTroop || role == VillageDispositionSpeakerRole.PlayerCompanion)
                VillageHunters.Add(agent);
        }
        _nextVillageMassacreTick = 0f;
    }

    private static void TickVillageMassacre(Mission mission)
    {
        if (_villageVictimTeam == null || !IsActiveForMission(mission) || mission.CurrentTime < _nextVillageMassacreTick) return;
        _nextVillageMassacreTick = mission.CurrentTime + 0.75f;
        // Only the explicit snapshot, never a whole-scene scan every frame. No militia reserve, no counterattack.
        foreach (var victim in VillageVictims.Keys)
        {
            if (victim.State == AgentState.Killed && victim.Character is CharacterObject c && c.HeroObject?.IsAlive == true
                && VillageDeathReceipts.Add(victim.Index)) KillCharacterAction.ApplyByMurder(c.HeroObject, Hero.MainHero, false);
        }
        var alive = VillageVictims.Keys.Where(IsLiveHostileSpeaker).ToList();
        foreach (var victim in alive) SiegeAiInterventionBehavior.DriveVillageVictim(victim, mission.MainAgent);
        int active = 0, limit = SiegeMassacreInteractionProfile.CalculateActiveHunterLimit(VillageHunters.Count);
        foreach (var soldier in VillageHunters)
        {
            if (!IsLiveHostileSpeaker(soldier) || active++ >= limit || alive.Count == 0) continue;
            var target = alive.OrderBy(a => a.Position.DistanceSquared(soldier.Position)).First();
            SiegeAiInterventionBehavior.DriveVillageHunter(soldier, target);
        }
    }

    private static void StopVillageMassacre()
    {
        try
        {
            if (!ReferenceEquals(_activeMission, Mission.Current)) return;
            if (_villageVictimTeam != null && _activeMission?.PlayerTeam != null)
            {
                _villageVictimTeam.SetIsEnemyOf(_activeMission.PlayerTeam, false);
                _activeMission.PlayerTeam.SetIsEnemyOf(_villageVictimTeam, false);
            }
            foreach (var pair in VillageVictims)
                if (IsLiveHostileSpeaker(pair.Key) && pair.Value != null) pair.Key.SetTeam(pair.Value, true);
            foreach (var soldier in VillageHunters)
                if (IsLiveHostileSpeaker(soldier)) { soldier.InvalidateTargetAgent(); soldier.DisableScriptedMovement(); soldier.ClearTargetFrame(); }
        }
        catch (System.Exception ex) { Logger.Log("VillageAftermath", "Stop village combat cleanup: " + ex.Message); }
        finally
        { VillageVictims.Clear(); VillageHunters.Clear(); VillageDeathReceipts.Clear(); _villageVictimTeam = null; }

    }
}

public partial class SiegeAiInterventionBehavior
{
    internal static void DriveVillageVictim(Agent victim, Agent threat)
    {
        if (!VillageAftermathBehavior.IsActiveForMission(Mission.Current) || victim?.IsActive() != true || threat == null) return;
        victim.InvalidateTargetAgent();
        TryForceInterventionCivilianDirectRetreat(victim, Mission.Current, threat, "village_massacre_flee", out _);
    }
    internal static void DriveVillageHunter(Agent soldier, Agent target)
    {
        if (!VillageAftermathBehavior.IsActiveForMission(Mission.Current) || soldier?.IsActive() != true || target?.IsActive() != true) return;
        DisableCompanionStyleFollow(soldier);
        ForceAgentForMassacreFight(soldier);
        soldier.SetWatchState(Agent.WatchState.Alarmed);
        soldier.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
        soldier.SetLookAgent(target);
        if (soldier.Position.DistanceSquared(target.Position) > 6f)
            TrySetInterventionAgentTargetPosition(soldier, target.Position, "village_massacre_target");
        else { soldier.DisableScriptedMovement(); soldier.ClearTargetFrame(); }
    }
}
