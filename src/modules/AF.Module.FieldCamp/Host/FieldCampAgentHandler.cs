using System;
using SandBox;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

// Town actor lifecycle with field-camp placement and roster ownership. No fake settlement
// or LocationEncounter: those would activate town crime, passage and encounter cleanup.
internal sealed class FieldCampAgentHandler : MissionAgentHandler
{
    public override void EarlyStart()
    {
        // Native EarlyStart dereferences Settlement.CurrentSettlement for weather.
        // Camp work/seat owners already manage their own dynamically created props.
        Mission.SetReportStuckAgentsMode(true);
    }

    // Camp has no LocationComplex passages or delayed native spawn queue. Inherited
    // tick/mode callbacks would tick a stale town or use its static disabled-face IDs.
    public override void OnMissionTick(float dt) { }
    public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart) { }

    internal Agent SpawnResident(CharacterObject character, PartyBase party, Team team, Vec3 position, Vec2 direction, bool civilian, IAgentOriginBase origin = null)
    {
        var data = new AgentData(origin ?? new PartyAgentOrigin(party, character, -1, default(UniqueTroopDescriptor), false, true))
            .Monster(TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(character.Race, "_settlement"))
            .NoHorses(true);
        var resident = new LocationCharacter(data, _ => { }, null, fixedLocation: true,
            LocationCharacter.CharacterRelations.Friendly, null, civilian);
        // Native SpawnWanderingAgentWithInitialFrame reads Settlement.CurrentSettlement
        // for soldier colours and can snap onto prop roofs. Use validated terrain and
        // camp team colours, then run the same LocationCharacter lifecycle.
        AgentBuildData build = resident.GetAgentBuildData().Team(team)
            .InitialPosition(in position).InitialDirection(in direction)
            .ClothingColor1(team.Color).ClothingColor2(team.Color2)
            .CivilianEquipment(civilian).NoHorses(true).Controller(AgentControllerType.AI);
        Agent agent = Mission.SpawnAgent(build);
        if (agent == null) return null;
        FieldCampMissionLogic.ApplyCampActionSet(agent);
        try
        {
            agent.GetComponent<CampaignAgentComponent>()?.CreateAgentNavigator(resident);
            resident.AddBehaviors(agent);
            resident.AfterAgentCreated?.Invoke(agent);
            Game.Current.EventManager.TriggerEvent(new LocationCharacterAgentSpawnedMissionEvent(resident, agent, WeakGameEntity.Invalid));
        }
        catch (Exception ex)
        {
            // The agent exists and must still be returned to the camp activity owner.
            Logger.Log("FieldCamp", "resident native setup incomplete agent=" + agent.Index + " error=" + ex.Message);
        }
        return agent;
    }
}
