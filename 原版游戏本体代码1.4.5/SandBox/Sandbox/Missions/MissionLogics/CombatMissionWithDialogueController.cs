using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.AI.AgentComponents;

namespace SandBox.Missions.MissionLogics;

public class CombatMissionWithDialogueController : MissionLogic, IMissionAgentSpawnLogic, IMissionBehavior
{
	private BattleAgentLogic _battleAgentLogic;

	private readonly BasicCharacterObject _characterToTalkTo;

	private bool _isMissionInitialized;

	private bool _troopsInitialized;

	private bool _conversationInitialized;

	private int _numSpawnedTroops;

	private readonly IMissionTroopSupplier[] _troopSuppliers;

	public BattleSideEnum PlayerSide => BattleSideEnum.None;

	public CombatMissionWithDialogueController(IMissionTroopSupplier[] suppliers, BasicCharacterObject characterToTalkTo)
	{
		_troopSuppliers = suppliers;
		_characterToTalkTo = characterToTalkTo;
	}

	public override void OnCreated()
	{
		base.OnCreated();
		base.Mission.DoesMissionRequireCivilianEquipment = true;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_battleAgentLogic = Mission.Current.GetMissionBehavior<BattleAgentLogic>();
	}

	public override void AfterStart()
	{
		base.AfterStart();
		base.Mission.DeploymentPlan.MakeDefaultDeploymentPlans();
	}

	public override void OnMissionTick(float dt)
	{
		if (!_isMissionInitialized)
		{
			SpawnAgents();
			_isMissionInitialized = true;
			Mission.Current.OnDeploymentFinished();
			return;
		}
		if (!_troopsInitialized)
		{
			_troopsInitialized = true;
			foreach (Agent agent in base.Mission.Agents)
			{
				_battleAgentLogic.OnAgentBuild(agent, null);
			}
		}
		if (_conversationInitialized || Agent.Main == null || !Agent.Main.IsActive())
		{
			return;
		}
		foreach (Agent agent2 in base.Mission.Agents)
		{
			ScriptedMovementComponent component = agent2.GetComponent<ScriptedMovementComponent>();
			if (component != null && component.ShouldConversationStartWithAgent())
			{
				StartConversation(agent2, setActionsInstantly: true);
				_conversationInitialized = true;
			}
		}
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		if (!_conversationInitialized && affectedAgent.Team != Mission.Current.PlayerTeam && affectorAgent != null && affectorAgent == Agent.Main)
		{
			_conversationInitialized = true;
			StartFight(hasPlayerChangedSide: false);
		}
	}

	public void StartFight(bool hasPlayerChangedSide)
	{
		base.Mission.SetMissionMode(MissionMode.Battle, atStart: false);
		if (hasPlayerChangedSide)
		{
			Agent.Main.SetTeam((Agent.Main.Team == base.Mission.AttackerTeam) ? base.Mission.DefenderTeam : base.Mission.AttackerTeam, sync: true);
			Mission.Current.PlayerTeam = Agent.Main.Team;
		}
		foreach (Agent agent in base.Mission.Agents)
		{
			if (Agent.Main != agent)
			{
				if (hasPlayerChangedSide && agent.Team != Mission.Current.PlayerTeam && agent.Origin.BattleCombatant as PartyBase == PartyBase.MainParty)
				{
					agent.SetTeam(Mission.Current.PlayerTeam, sync: true);
				}
				AgentFlag agentFlags = agent.GetAgentFlags();
				agent.SetAgentFlags(agentFlags | AgentFlag.CanGetAlarmed);
				agent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator();
				agent.GetComponent<CampaignAgentComponent>().AgentNavigator.AddBehaviorGroup<AlarmedBehaviorGroup>();
				agent.SetAlarmState(Agent.AIStateFlag.Alarmed);
			}
		}
	}

	public void StartConversation(Agent agent, bool setActionsInstantly)
	{
		Campaign.Current.ConversationManager.SetupAndStartMissionConversation(agent, base.Mission.MainAgent, setActionsInstantly);
		foreach (Agent conversationAgent in Campaign.Current.ConversationManager.ConversationAgents)
		{
			conversationAgent.ForceAiBehaviorSelection();
			conversationAgent.AgentVisuals.SetClothComponentKeepStateOfAllMeshes(keepState: true);
		}
		base.Mission.MainAgentServer.AgentVisuals.SetClothComponentKeepStateOfAllMeshes(keepState: true);
		base.Mission.SetMissionMode(MissionMode.Conversation, setActionsInstantly);
	}

	private void SpawnAgents()
	{
		Agent targetAgent = null;
		IMissionTroopSupplier[] troopSuppliers = _troopSuppliers;
		for (int i = 0; i < troopSuppliers.Length; i++)
		{
			foreach (IAgentOriginBase item in troopSuppliers[i].SupplyTroops(25).ToList())
			{
				Agent agent = Mission.Current.SpawnTroop(item, item.BattleCombatant.Side == BattleSideEnum.Attacker, hasFormation: false, spawnWithHorse: false, isReinforcement: false, 0, 0, isAlarmed: false, wieldInitialWeapons: true, null, null);
				_numSpawnedTroops++;
				if (!agent.IsMainAgent)
				{
					agent.AddComponent(new ScriptedMovementComponent(agent, agent.Character == _characterToTalkTo, item.IsUnderPlayersCommand ? 5 : 2));
					if (agent.Character == _characterToTalkTo)
					{
						targetAgent = agent;
					}
				}
			}
		}
		foreach (Agent agent2 in base.Mission.Agents)
		{
			ScriptedMovementComponent component = agent2.GetComponent<ScriptedMovementComponent>();
			if (component != null)
			{
				if (agent2.Team.Side == Mission.Current.PlayerTeam.Side)
				{
					component.SetTargetAgent(targetAgent);
				}
				else
				{
					component.SetTargetAgent(Agent.Main);
				}
			}
			agent2.SetFiringOrder(FiringOrder.RangedWeaponUsageOrderEnum.HoldYourFire);
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
		return false;
	}

	public float GetReinforcementInterval(BattleSideEnum battleSide = BattleSideEnum.None)
	{
		return 0f;
	}

	public bool IsSideDepleted(BattleSideEnum side)
	{
		int num = _troopSuppliers[(int)side].GetAllTroops().Count() - _troopSuppliers[(int)side].NumTroopsNotSupplied - _troopSuppliers[(int)side].NumRemovedTroops;
		if (Mission.Current.PlayerTeam == base.Mission.DefenderTeam)
		{
			if (side == BattleSideEnum.Attacker)
			{
				num -= MobileParty.MainParty.Party.NumberOfHealthyMembers;
			}
			else if (Agent.Main != null && Agent.Main.IsActive())
			{
				num += MobileParty.MainParty.Party.NumberOfHealthyMembers;
			}
		}
		return num == 0;
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
		throw new NotImplementedException();
	}
}
