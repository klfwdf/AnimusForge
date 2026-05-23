using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Towns;

public class AlleyFightMissionHandler : MissionLogic, IMissionAgentSpawnLogic, IMissionBehavior
{
	private TroopRoster _playerSideTroops;

	private TroopRoster _rivalSideTroops;

	private List<Agent> _playerSideAliveAgents = new List<Agent>();

	private List<Agent> _rivalSideAliveAgents = new List<Agent>();

	public BattleSideEnum PlayerSide => BattleSideEnum.Defender;

	public AlleyFightMissionHandler(TroopRoster playerSideTroops, TroopRoster rivalSideTroops)
	{
		_playerSideTroops = playerSideTroops;
		_rivalSideTroops = rivalSideTroops;
	}

	public override void OnBehaviorInitialize()
	{
		base.Mission.GetAgentTroopClass_Override += GetAlleyFightMissionTroopClass;
	}

	public override void EarlyStart()
	{
		base.EarlyStart();
		base.Mission.Teams.Add(BattleSideEnum.Defender, Clan.PlayerClan.Color, Clan.PlayerClan.Color2, Clan.PlayerClan.Banner);
		base.Mission.Teams.Add(BattleSideEnum.Attacker, Clan.BanditFactions.First().Color, Clan.BanditFactions.First().Color2, Clan.BanditFactions.First().Banner);
		base.Mission.PlayerTeam = base.Mission.DefenderTeam;
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		if (_playerSideAliveAgents.Contains(affectedAgent))
		{
			_playerSideAliveAgents.Remove(affectedAgent);
			_playerSideTroops.RemoveTroop(affectedAgent.Character as CharacterObject);
		}
		else if (_rivalSideAliveAgents.Contains(affectedAgent))
		{
			_rivalSideAliveAgents.Remove(affectedAgent);
			_rivalSideTroops.RemoveTroop(affectedAgent.Character as CharacterObject);
		}
		if (affectedAgent == Agent.Main)
		{
			Campaign.Current.GetCampaignBehavior<IAlleyCampaignBehavior>().OnPlayerDiedInMission();
		}
	}

	public override void AfterStart()
	{
		base.Mission.GetDeploymentPlan<DefaultMissionDeploymentPlan>(out var deploymentPlan);
		deploymentPlan.AddTroops(base.Mission.DefenderTeam, FormationClass.Infantry, _playerSideTroops.TotalManCount);
		deploymentPlan.AddTroops(base.Mission.AttackerTeam, FormationClass.Infantry, _rivalSideTroops.TotalManCount);
		base.Mission.DeploymentPlan.MakeDefaultDeploymentPlans();
	}

	public override InquiryData OnEndMissionRequest(out bool canLeave)
	{
		canLeave = true;
		return new InquiryData("", GameTexts.FindText("str_give_up_fight").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), base.Mission.OnEndMissionResult, null);
	}

	public override void OnRetreatMission()
	{
		Campaign.Current.GetCampaignBehavior<IAlleyCampaignBehavior>().OnPlayerRetreatedFromMission();
	}

	public override void OnRenderingStarted()
	{
		Mission.Current.SetMissionMode(MissionMode.Battle, atStart: true);
		SpawnAgentsForBothSides();
		base.Mission.PlayerTeam.PlayerOrderController.SelectAllFormations();
		base.Mission.PlayerTeam.PlayerOrderController.SetOrder(OrderType.Charge);
		base.Mission.PlayerEnemyTeam.MasterOrderController.SelectAllFormations();
		base.Mission.PlayerEnemyTeam.MasterOrderController.SetOrder(OrderType.Charge);
	}

	public override void OnMissionStateFinalized()
	{
		base.Mission.GetAgentTroopClass_Override -= GetAlleyFightMissionTroopClass;
	}

	private void SpawnAgentsForBothSides()
	{
		Mission.Current.PlayerEnemyTeam.SetIsEnemyOf(Mission.Current.PlayerTeam, isEnemyOf: true);
		foreach (TroopRosterElement item in _playerSideTroops.GetTroopRoster())
		{
			for (int i = 0; i < item.Number; i++)
			{
				SpawnATroop(item.Character, isPlayerSide: true);
			}
		}
		foreach (TroopRosterElement item2 in _rivalSideTroops.GetTroopRoster())
		{
			for (int j = 0; j < item2.Number; j++)
			{
				SpawnATroop(item2.Character, isPlayerSide: false);
			}
		}
	}

	private void SpawnATroop(CharacterObject character, bool isPlayerSide)
	{
		SimpleAgentOrigin troopOrigin = new SimpleAgentOrigin(character);
		Agent agent = Mission.Current.SpawnTroop(troopOrigin, isPlayerSide, hasFormation: true, spawnWithHorse: false, isReinforcement: false, 0, 0, isAlarmed: true, wieldInitialWeapons: true, null, null);
		if (isPlayerSide)
		{
			_playerSideAliveAgents.Add(agent);
		}
		else
		{
			_rivalSideAliveAgents.Add(agent);
		}
		AgentFlag agentFlags = agent.GetAgentFlags();
		agent.SetAgentFlags((AgentFlag)((uint)(agentFlags | AgentFlag.CanGetAlarmed) & 0xFFEFFFFFu));
		if (agent.IsAIControlled)
		{
			agent.SetWatchState(Agent.WatchState.Alarmed);
		}
		if (isPlayerSide)
		{
			agent.SetTeam(Mission.Current.PlayerTeam, sync: true);
		}
		else
		{
			agent.SetTeam(Mission.Current.PlayerEnemyTeam, sync: true);
		}
	}

	public void StartSpawner(BattleSideEnum side)
	{
	}

	public void StopSpawner(BattleSideEnum side)
	{
	}

	public bool IsSideSpawnEnabled(BattleSideEnum side)
	{
		return true;
	}

	public bool IsSideDepleted(BattleSideEnum side)
	{
		if (side != BattleSideEnum.Attacker)
		{
			return _playerSideAliveAgents.Count == 0;
		}
		return _rivalSideAliveAgents.Count == 0;
	}

	public float GetReinforcementInterval(BattleSideEnum side = BattleSideEnum.None)
	{
		return float.MaxValue;
	}

	public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side)
	{
		throw new NotImplementedException();
	}

	public int GetNumberOfPlayerControllableTroops()
	{
		throw new NotImplementedException();
	}

	public bool GetSpawnHorses(BattleSideEnum side)
	{
		return false;
	}

	private FormationClass GetAlleyFightMissionTroopClass(BattleSideEnum battleSide, BasicCharacterObject agentCharacter)
	{
		return agentCharacter.GetFormationClass().DismountedClass();
	}
}
