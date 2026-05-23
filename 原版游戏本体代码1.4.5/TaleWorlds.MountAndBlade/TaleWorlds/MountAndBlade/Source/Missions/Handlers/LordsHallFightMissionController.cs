using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Objects;

namespace TaleWorlds.MountAndBlade.Source.Missions.Handlers;

public class LordsHallFightMissionController : MissionLogic, IMissionAgentSpawnLogic, IMissionBehavior
{
	private class MissionSide
	{
		private readonly BattleSideEnum _side;

		private readonly IMissionTroopSupplier _troopSupplier;

		private readonly bool _isPlayerSide;

		private bool _troopSpawningActive = true;

		private int _numberOfSpawnedTroops;

		public bool TroopSpawningActive => _troopSpawningActive;

		public int NumberOfActiveTroops => _numberOfSpawnedTroops - _troopSupplier.NumRemovedTroops;

		public MissionSide(BattleSideEnum side, IMissionTroopSupplier troopSupplier, bool isPlayerSide)
		{
			_side = side;
			_isPlayerSide = isPlayerSide;
			_troopSupplier = troopSupplier;
		}

		public void SpawnTroops(Dictionary<int, Dictionary<int, AreaData>> areaMarkerDictionary, int spawnCount)
		{
			List<IAgentOriginBase> list = _troopSupplier.SupplyTroops(spawnCount).OrderByDescending(delegate(IAgentOriginBase x)
			{
				FormationClass agentTroopClass = Mission.Current.GetAgentTroopClass(_side, x.Troop);
				return (agentTroopClass == FormationClass.Ranged || agentTroopClass == FormationClass.HorseArcher) ? 1 : 0;
			}).ToList();
			for (int num = 0; num < list.Count; num++)
			{
				IAgentOriginBase agentOriginBase = list[num];
				bool flag = Mission.Current.GetAgentTroopClass(_side, agentOriginBase.Troop).IsRanged();
				List<KeyValuePair<int, AreaData>> list2 = areaMarkerDictionary.ElementAt(num % areaMarkerDictionary.Count).Value.ToList();
				List<(KeyValuePair<int, AreaData>, float)> list3 = new List<(KeyValuePair<int, AreaData>, float)>();
				foreach (KeyValuePair<int, AreaData> item in list2)
				{
					int num2 = 1000 * item.Value.GetAvailableMachines(flag).Count() + item.Value.GetAvailableMachines(!flag).Count();
					list3.Add((new KeyValuePair<int, AreaData>(item.Key, item.Value), num2));
				}
				KeyValuePair<int, AreaData> keyValuePair = MBRandom.ChooseWeighted(list3);
				AreaEntityData obj = keyValuePair.Value.GetAvailableMachines(flag).GetRandomElementInefficiently() ?? keyValuePair.Value.GetAvailableMachines(!flag).GetRandomElementInefficiently();
				MatrixFrame globalFrame = obj.Entity.GetGlobalFrame();
				Agent agent = Mission.Current.SpawnTroop(agentOriginBase, isPlayerSide: false, hasFormation: false, spawnWithHorse: false, isReinforcement: false, 0, 0, isAlarmed: false, wieldInitialWeapons: false, globalFrame.origin, globalFrame.rotation.f.AsVec2.Normalized());
				_numberOfSpawnedTroops++;
				AgentFlag agentFlags = agent.GetAgentFlags();
				agent.SetAgentFlags((AgentFlag)((uint)agentFlags & 0xFFEFFFFFu));
				agent.WieldInitialWeapons(Agent.WeaponWieldActionType.Instant);
				agent.SetWatchState(Agent.WatchState.Alarmed);
				agent.SetBehaviorValueSet(HumanAIComponent.BehaviorValueSet.DefensiveArrangementMove);
				obj.AssignAgent(agent);
			}
		}

		public void SpawnTroops(int spawnCount, bool isReinforcement)
		{
			if (!_troopSpawningActive)
			{
				return;
			}
			List<IAgentOriginBase> list = _troopSupplier.SupplyTroops(spawnCount).ToList();
			for (int i = 0; i < list.Count; i++)
			{
				if (BattleSideEnum.Attacker == _side)
				{
					Mission.Current.SpawnTroop(list[i], _isPlayerSide, hasFormation: true, spawnWithHorse: false, isReinforcement, spawnCount, i, isAlarmed: true, wieldInitialWeapons: true, null, null);
					_numberOfSpawnedTroops++;
				}
			}
		}

		public void SetSpawnTroops(bool spawnTroops)
		{
			_troopSpawningActive = spawnTroops;
		}

		public IEnumerable<IAgentOriginBase> GetAllTroops()
		{
			return _troopSupplier.GetAllTroops();
		}
	}

	private class AreaData
	{
		private const string ArcherSpawnPointTag = "defender_archer";

		private const string InfantrySpawnPointTag = "defender_infantry";

		private readonly List<FightAreaMarker> _areaList;

		private readonly List<AreaEntityData> _archerUsablePoints;

		private readonly List<AreaEntityData> _infantryUsablePoints;

		public IEnumerable<FightAreaMarker> AreaList => _areaList;

		public IEnumerable<AreaEntityData> ArcherUsablePoints => _archerUsablePoints;

		public IEnumerable<AreaEntityData> InfantryUsablePoints => _infantryUsablePoints;

		public AreaData(List<FightAreaMarker> areaList)
		{
			_areaList = new List<FightAreaMarker>();
			_archerUsablePoints = new List<AreaEntityData>();
			_infantryUsablePoints = new List<AreaEntityData>();
			foreach (FightAreaMarker area in areaList)
			{
				AddAreaMarker(area);
			}
		}

		public IEnumerable<AreaEntityData> GetAvailableMachines(bool isArcher)
		{
			List<AreaEntityData> list = (isArcher ? _archerUsablePoints : _infantryUsablePoints);
			foreach (AreaEntityData item in list)
			{
				if (!item.InUse)
				{
					yield return item;
				}
			}
		}

		public void AddAreaMarker(FightAreaMarker marker)
		{
			_areaList.Add(marker);
			foreach (GameEntity entity in marker.GetGameEntitiesWithTagInRange("defender_archer"))
			{
				PathFaceRecord record = PathFaceRecord.NullFaceRecord;
				Mission.Current.Scene.GetNavMeshFaceIndex(ref record, entity.GetGlobalFrame().origin, checkIfDisabled: true);
				if (record.FaceIndex != -1 && _archerUsablePoints.All((AreaEntityData x) => x.Entity != entity))
				{
					_archerUsablePoints.Add(new AreaEntityData(entity));
				}
			}
			foreach (GameEntity entity2 in marker.GetGameEntitiesWithTagInRange("defender_infantry"))
			{
				if (_infantryUsablePoints.All((AreaEntityData x) => x.Entity != entity2))
				{
					_infantryUsablePoints.Add(new AreaEntityData(entity2));
				}
			}
		}

		public AreaEntityData FindAgentMachine(Agent agent)
		{
			return _infantryUsablePoints.FirstOrDefault((AreaEntityData x) => x.UserAgent == agent) ?? _archerUsablePoints.FirstOrDefault((AreaEntityData x) => x.UserAgent == agent);
		}
	}

	private class AreaEntityData
	{
		public readonly GameEntity Entity;

		public Agent UserAgent { get; private set; }

		public bool InUse => UserAgent != null;

		public AreaEntityData(GameEntity entity)
		{
			Entity = entity;
		}

		public void AssignAgent(Agent agent)
		{
			UserAgent = agent;
			MatrixFrame globalFrame = Entity.GetGlobalFrame();
			agent.SetBehaviorValueSet(HumanAIComponent.BehaviorValueSet.DefaultMove);
			UserAgent.SetFormationFrameEnabled(new WorldPosition(agent.Mission.Scene, globalFrame.origin), globalFrame.rotation.f.AsVec2.Normalized(), Vec2.Zero, 0f);
		}

		public void StopUse()
		{
			if (UserAgent.IsActive())
			{
				UserAgent.SetFormationFrameDisabled();
			}
			UserAgent = null;
		}
	}

	private const int ReinforcementWaveAgentCount = 5;

	private readonly float _areaLostRatio;

	private readonly float _attackerDefenderTroopCountRatio;

	private readonly int _attackerSideTroopCountMax;

	private readonly int _defenderSideTroopCountMax;

	private readonly MissionSide[] _missionSides;

	private Team[] _attackerTeams;

	private Team[] _defenderTeams;

	private Dictionary<int, Dictionary<int, AreaData>> _dividedAreaDictionary;

	private List<int> _areaIndexList;

	private int _lastAreaLostByDefender;

	private bool _troopsInitialized;

	private bool _isMissionInitialized;

	private bool _spawnReinforcements;

	private bool _setChargeOrderNextFrame;

	private BattleSideEnum _playerSide;

	private int _removedAllyCounter;

	public BattleSideEnum PlayerSide { get; private set; }

	public LordsHallFightMissionController(IMissionTroopSupplier[] suppliers, float areaLostRatio, float attackerDefenderTroopCountRatio, int attackerSideTroopCountMax, int defenderSideTroopCountMax, BattleSideEnum playerSide)
	{
		PlayerSide = playerSide;
		_areaLostRatio = areaLostRatio;
		_attackerDefenderTroopCountRatio = attackerDefenderTroopCountRatio;
		_attackerSideTroopCountMax = attackerSideTroopCountMax;
		_defenderSideTroopCountMax = defenderSideTroopCountMax;
		_missionSides = new MissionSide[2];
		_playerSide = playerSide;
		for (int i = 0; i < 2; i++)
		{
			IMissionTroopSupplier troopSupplier = suppliers[i];
			bool isPlayerSide = i == (int)playerSide;
			_missionSides[i] = new MissionSide((BattleSideEnum)i, troopSupplier, isPlayerSide);
		}
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		base.Mission.GetAgentTroopClass_Override += GetLordsHallFightTroopClass;
	}

	public override void OnMissionStateFinalized()
	{
		base.OnMissionStateFinalized();
		base.Mission.GetAgentTroopClass_Override -= GetLordsHallFightTroopClass;
	}

	public override void OnCreated()
	{
		base.OnCreated();
		base.Mission.DoesMissionRequireCivilianEquipment = false;
	}

	public override void OnMissionTick(float dt)
	{
		if (!_isMissionInitialized)
		{
			InitializeMission();
			_isMissionInitialized = true;
			return;
		}
		if (!_troopsInitialized)
		{
			_troopsInitialized = true;
		}
		if (_setChargeOrderNextFrame)
		{
			if (base.Mission.PlayerTeam.ActiveAgents.Count > 0)
			{
				base.Mission.PlayerTeam.PlayerOrderController.SelectAllFormations();
				base.Mission.PlayerTeam.PlayerOrderController.SetOrder(OrderType.Charge);
			}
			_setChargeOrderNextFrame = false;
		}
		CheckForReinforcement();
		CheckIfAnyAreaIsLostByDefender();
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (affectedAgent.Team.IsDefender)
		{
			FindAgentMachine(affectedAgent)?.Item2.StopUse();
			return;
		}
		_setChargeOrderNextFrame = affectedAgent.IsMainAgent;
		_removedAllyCounter++;
		if (_removedAllyCounter == 5)
		{
			_spawnReinforcements = true;
			_removedAllyCounter = 0;
		}
	}

	private Tuple<int, AreaEntityData> FindAgentMachine(Agent agent)
	{
		Tuple<int, AreaEntityData> tuple = null;
		foreach (KeyValuePair<int, Dictionary<int, AreaData>> item in _dividedAreaDictionary)
		{
			if (tuple != null)
			{
				break;
			}
			foreach (KeyValuePair<int, AreaData> item2 in item.Value)
			{
				AreaEntityData areaEntityData = item2.Value.FindAgentMachine(agent);
				if (areaEntityData != null)
				{
					tuple = new Tuple<int, AreaEntityData>(item.Key, areaEntityData);
					break;
				}
			}
		}
		return tuple;
	}

	private void InitializeMission()
	{
		_areaIndexList = new List<int>();
		_dividedAreaDictionary = new Dictionary<int, Dictionary<int, AreaData>>();
		IOrderedEnumerable<FightAreaMarker> orderedEnumerable = from area in base.Mission.ActiveMissionObjects.FindAllWithType<FightAreaMarker>()
			orderby area.AreaIndex
			select area;
		base.Mission.DeploymentPlan.MakeDefaultDeploymentPlans();
		foreach (FightAreaMarker item in orderedEnumerable)
		{
			if (!_dividedAreaDictionary.ContainsKey(item.AreaIndex))
			{
				_dividedAreaDictionary.Add(item.AreaIndex, new Dictionary<int, AreaData>());
			}
			if (!_dividedAreaDictionary[item.AreaIndex].ContainsKey(item.SubAreaIndex))
			{
				_dividedAreaDictionary[item.AreaIndex].Add(item.SubAreaIndex, new AreaData(new List<FightAreaMarker> { item }));
			}
			else
			{
				_dividedAreaDictionary[item.AreaIndex][item.SubAreaIndex].AddAreaMarker(item);
			}
		}
		_areaIndexList = _dividedAreaDictionary.Keys.ToList();
		_missionSides[0].SpawnTroops(_dividedAreaDictionary, _defenderSideTroopCountMax);
		int numberOfActiveTroops = _missionSides[0].NumberOfActiveTroops;
		_defenderTeams = new Team[2];
		_defenderTeams[0] = Mission.Current.DefenderTeam;
		_defenderTeams[1] = Mission.Current.DefenderAllyTeam;
		int spawnCount = TaleWorlds.Library.MathF.Max(1, TaleWorlds.Library.MathF.Min(_attackerSideTroopCountMax, TaleWorlds.Library.MathF.Round((float)numberOfActiveTroops * _attackerDefenderTroopCountRatio)));
		_missionSides[1].SpawnTroops(spawnCount, isReinforcement: false);
		bool flag = Mission.Current.AttackerTeam == Mission.Current.PlayerTeam || (Mission.Current.AttackerAllyTeam != null && Mission.Current.AttackerAllyTeam == Mission.Current.PlayerTeam);
		_attackerTeams = new Team[2];
		_attackerTeams[0] = Mission.Current.AttackerTeam;
		_attackerTeams[1] = Mission.Current.AttackerAllyTeam;
		Team[] attackerTeams = _attackerTeams;
		foreach (Team team in attackerTeams)
		{
			if (team == null)
			{
				continue;
			}
			foreach (Formation item2 in team.FormationsIncludingEmpty)
			{
				if (item2.CountOfUnits > 0)
				{
					item2.SetArrangementOrder(ArrangementOrder.ArrangementOrderSquare);
					item2.SetFormOrder(FormOrder.FormOrderDeep);
				}
				item2.SetMovementOrder(MovementOrder.MovementOrderCharge);
				item2.SetFiringOrder(FiringOrder.FiringOrderHoldYourFire);
				if (flag)
				{
					item2.PlayerOwner = Mission.Current.MainAgent;
				}
			}
		}
	}

	private void CheckForReinforcement()
	{
		if (_spawnReinforcements)
		{
			_missionSides[1].SpawnTroops(5, isReinforcement: true);
			_spawnReinforcements = false;
		}
	}

	public void StartSpawner(BattleSideEnum side)
	{
		_missionSides[(int)side].SetSpawnTroops(spawnTroops: true);
	}

	public void StopSpawner(BattleSideEnum side)
	{
		_missionSides[(int)side].SetSpawnTroops(spawnTroops: false);
	}

	public bool IsSideSpawnEnabled(BattleSideEnum side)
	{
		return _missionSides[(int)side].TroopSpawningActive;
	}

	public float GetReinforcementInterval(BattleSideEnum side = BattleSideEnum.None)
	{
		return 0f;
	}

	public bool IsSideDepleted(BattleSideEnum side)
	{
		return _missionSides[(int)side].NumberOfActiveTroops == 0;
	}

	public int GetNumberOfPlayerControllableTroops()
	{
		throw new NotImplementedException();
	}

	public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side)
	{
		return _missionSides[(int)side].GetAllTroops();
	}

	public bool GetSpawnHorses(BattleSideEnum side)
	{
		return false;
	}

	private void CheckIfAnyAreaIsLostByDefender()
	{
		int num = -1;
		for (int i = 0; i < _areaIndexList.Count; i++)
		{
			int num2 = _areaIndexList[i];
			if (num2 <= _lastAreaLostByDefender || num >= 0)
			{
				continue;
			}
			foreach (KeyValuePair<int, AreaData> item in _dividedAreaDictionary[num2])
			{
				if (IsAreaLostByDefender(item.Value))
				{
					num = num2;
					break;
				}
			}
		}
		if (num > 0)
		{
			OnAreaLost(num);
		}
	}

	private void OnAreaLost(int areaIndex)
	{
		int num = TaleWorlds.Library.MathF.Min(_areaIndexList.IndexOf(areaIndex) + 1, _areaIndexList.Count - 1);
		for (int i = TaleWorlds.Library.MathF.Max(0, _areaIndexList.IndexOf(_lastAreaLostByDefender)); i < num; i++)
		{
			int key = _areaIndexList[i];
			foreach (KeyValuePair<int, AreaData> item in _dividedAreaDictionary[key])
			{
				StartAreaPullBack(item.Value, _areaIndexList[num]);
			}
		}
		_lastAreaLostByDefender = areaIndex;
	}

	private void StartAreaPullBack(AreaData areaData, int nextAreaIndex)
	{
		foreach (AreaEntityData archerUsablePoint in areaData.ArcherUsablePoints)
		{
			if (archerUsablePoint.InUse)
			{
				Agent userAgent = archerUsablePoint.UserAgent;
				archerUsablePoint.StopUse();
				FindPosition(nextAreaIndex, isArcher: true)?.AssignAgent(userAgent);
			}
		}
		foreach (AreaEntityData infantryUsablePoint in areaData.InfantryUsablePoints)
		{
			if (infantryUsablePoint.InUse)
			{
				Agent userAgent2 = infantryUsablePoint.UserAgent;
				infantryUsablePoint.StopUse();
				FindPosition(nextAreaIndex, isArcher: false)?.AssignAgent(userAgent2);
			}
		}
	}

	private AreaEntityData FindPosition(int nextAreaIndex, bool isArcher)
	{
		int num = SelectBestSubArea(nextAreaIndex, isArcher);
		if (num < 0)
		{
			isArcher = !isArcher;
			num = SelectBestSubArea(nextAreaIndex, isArcher);
		}
		return _dividedAreaDictionary[nextAreaIndex][num].GetAvailableMachines(isArcher).GetRandomElementInefficiently();
	}

	private int SelectBestSubArea(int areaIndex, bool isArcher)
	{
		int result = -1;
		float num = 0f;
		foreach (KeyValuePair<int, AreaData> item in _dividedAreaDictionary[areaIndex])
		{
			float areaAvailabilityRatio = GetAreaAvailabilityRatio(item.Value, isArcher);
			if (areaAvailabilityRatio > num)
			{
				num = areaAvailabilityRatio;
				result = item.Key;
			}
		}
		return result;
	}

	private float GetAreaAvailabilityRatio(AreaData areaData, bool isArcher)
	{
		int num = (isArcher ? areaData.ArcherUsablePoints.Count() : areaData.InfantryUsablePoints.Count());
		int num2 = (isArcher ? areaData.ArcherUsablePoints.Count((AreaEntityData x) => !x.InUse) : areaData.InfantryUsablePoints.Count((AreaEntityData x) => !x.InUse));
		if (num != 0)
		{
			return (float)num2 / (float)num;
		}
		return 0f;
	}

	private bool IsAreaLostByDefender(AreaData areaData)
	{
		int num = 0;
		Team[] defenderTeams = _defenderTeams;
		foreach (Team team in defenderTeams)
		{
			if (team == null)
			{
				continue;
			}
			foreach (Agent activeAgent in team.ActiveAgents)
			{
				if (IsAgentInArea(activeAgent, areaData))
				{
					num++;
				}
			}
		}
		int num2 = TaleWorlds.Library.MathF.Round((float)num * _areaLostRatio);
		bool flag = num2 == 0;
		if (!flag)
		{
			defenderTeams = _attackerTeams;
			foreach (Team team2 in defenderTeams)
			{
				if (team2 == null)
				{
					continue;
				}
				foreach (Agent activeAgent2 in team2.ActiveAgents)
				{
					if (IsAgentInArea(activeAgent2, areaData))
					{
						num2--;
						if (num2 == 0)
						{
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					break;
				}
			}
		}
		return flag;
	}

	private bool IsAgentInArea(Agent agent, AreaData areaData)
	{
		bool result = false;
		Vec3 position = agent.Position;
		foreach (FightAreaMarker area in areaData.AreaList)
		{
			if (area.IsPositionInRange(position))
			{
				result = true;
				break;
			}
		}
		return result;
	}

	private FormationClass GetLordsHallFightTroopClass(BattleSideEnum side, BasicCharacterObject agentCharacter)
	{
		return agentCharacter.GetFormationClass().DismountedClass();
	}
}
