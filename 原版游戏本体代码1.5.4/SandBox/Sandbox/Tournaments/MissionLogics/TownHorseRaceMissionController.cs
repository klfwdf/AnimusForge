using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Tournaments.AgentControllers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Tournaments.MissionLogics;

public class TownHorseRaceMissionController : MissionLogic, ITournamentGameBehavior
{
	public class CheckPoint
	{
		private readonly VolumeBox _volumeBox;

		private readonly List<GameEntity> _bestTargetList;

		public string Name => _volumeBox.GameEntity.Name;

		public CheckPoint(VolumeBox volumeBox)
		{
			_volumeBox = volumeBox;
			_bestTargetList = GameEntity.CreateFromWeakEntity(_volumeBox.GameEntity).CollectChildrenEntitiesWithTag("best_target_point");
			_volumeBox.SetIsOccupiedDelegate(OnAgentsEnterCheckBox);
		}

		public Vec3 GetBestTargetPosition()
		{
			if (_bestTargetList.Count > 0)
			{
				return _bestTargetList[MBRandom.RandomInt(_bestTargetList.Count)].GetGlobalFrame().origin;
			}
			return _volumeBox.GameEntity.GetGlobalFrame().origin;
		}

		public void AddToCheckList(Agent agent)
		{
			_volumeBox.AddToCheckList(agent);
		}

		public void RemoveFromCheckList(Agent agent)
		{
			_volumeBox.RemoveFromCheckList(agent);
		}

		private void OnAgentsEnterCheckBox(VolumeBox volumeBox, List<Agent> agentsInVolume)
		{
			foreach (Agent item in agentsInVolume)
			{
				item.GetController<TownHorseRaceAgentController>().OnEnterCheckPoint(volumeBox);
			}
		}
	}

	public const int TourCount = 2;

	private readonly List<TownHorseRaceAgentController> _agents;

	private List<Team> _teams;

	private List<GameEntity> _startPoints;

	private BasicMissionTimer _startTimer;

	private CultureObject _culture;

	public List<CheckPoint> CheckPoints { get; private set; }

	public TownHorseRaceMissionController(CultureObject culture)
	{
		_culture = culture;
		_agents = new List<TownHorseRaceAgentController>();
	}

	public override void AfterStart()
	{
		base.AfterStart();
		CollectCheckPointsAndStartPoints();
		foreach (TownHorseRaceAgentController agent in _agents)
		{
			agent.DisableMovement();
		}
		_startTimer = new BasicMissionTimer();
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_startTimer == null || !(_startTimer.ElapsedTime > 3f))
		{
			return;
		}
		foreach (TownHorseRaceAgentController agent in _agents)
		{
			agent.Start();
		}
	}

	private void CollectCheckPointsAndStartPoints()
	{
		CheckPoints = new List<CheckPoint>();
		foreach (WeakGameEntity item in base.Mission.ActiveMissionObjects.Select((MissionObject amo) => amo.GameEntity))
		{
			VolumeBox firstScriptOfType = item.GetFirstScriptOfType<VolumeBox>();
			if (firstScriptOfType != null)
			{
				CheckPoints.Add(new CheckPoint(firstScriptOfType));
			}
		}
		CheckPoints = CheckPoints.OrderBy((CheckPoint x) => x.Name).ToList();
		_startPoints = base.Mission.Scene.FindEntitiesWithTag("sp_horse_race").ToList();
	}

	private MatrixFrame GetStartFrame(int index)
	{
		MatrixFrame result = ((index >= _startPoints.Count) ? ((_startPoints.Count > 0) ? _startPoints[0].GetGlobalFrame() : MatrixFrame.Identity) : _startPoints[index].GetGlobalFrame());
		result.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		return result;
	}

	private void SetItemsAndSpawnCharacter(CharacterObject troop)
	{
		int count = _agents.Count;
		Equipment equipment = new Equipment();
		equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.ArmorItemEndSlot, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("charger")));
		equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.HorseHarness, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("horse_harness_e")));
		equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("horse_whip")));
		equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Body, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("short_padded_robe")));
		MatrixFrame startFrame = GetStartFrame(count);
		AgentBuildData agentBuildData = new AgentBuildData(troop).Team(_teams[count]).InitialPosition(in startFrame.origin).InitialDirection(startFrame.rotation.f.AsVec2.Normalized())
			.Equipment(equipment)
			.Controller((troop != CharacterObject.PlayerCharacter) ? AgentControllerType.AI : AgentControllerType.Player);
		Agent agent = base.Mission.SpawnAgent(agentBuildData);
		agent.Health = agent.Monster.HitPoints;
		agent.WieldInitialWeapons();
		_agents.Add(AddHorseRaceAgentController(agent));
		if (troop == CharacterObject.PlayerCharacter)
		{
			base.Mission.PlayerTeam = _teams[count];
		}
	}

	private TownHorseRaceAgentController AddHorseRaceAgentController(Agent agent)
	{
		return agent.AddController(typeof(TownHorseRaceAgentController)) as TownHorseRaceAgentController;
	}

	private void InitializeTeams(int count)
	{
		_teams = new List<Team>();
		for (int i = 0; i < count; i++)
		{
			_teams.Add(base.Mission.Teams.Add(BattleSideEnum.None));
		}
	}

	public void StartMatch(TournamentMatch match, bool isLastRound)
	{
		throw new NotImplementedException();
	}

	public void SkipMatch(TournamentMatch match)
	{
		throw new NotImplementedException();
	}

	public bool IsMatchEnded()
	{
		throw new NotImplementedException();
	}

	public void OnMatchEnded()
	{
		throw new NotImplementedException();
	}
}
