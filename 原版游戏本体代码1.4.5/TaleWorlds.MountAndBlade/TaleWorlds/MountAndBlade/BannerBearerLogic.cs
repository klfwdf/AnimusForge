using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace TaleWorlds.MountAndBlade;

public class BannerBearerLogic : MissionLogic
{
	private class FormationBannerController
	{
		public enum BannerState
		{
			Initialized,
			OnAgent,
			OnGround
		}

		public struct BannerInstance
		{
			public readonly Agent BannerBearer;

			public readonly GameEntity Entity;

			private readonly BannerState State;

			public bool IsOnGround => State == BannerState.OnGround;

			public bool IsOnAgent => State == BannerState.OnAgent;

			public BannerInstance(Agent bannerBearer, GameEntity entity, BannerState state)
			{
				BannerBearer = bannerBearer;
				Entity = entity;
				State = state;
			}
		}

		private int _lastActiveBannerBearerCount;

		private bool _requiresAgentStatUpdate;

		private BannerBearerLogic _bannerLogic;

		private Mission _mission;

		private Dictionary<Agent, (GameEntity bannerEntity, float lastDistance)> _bannerSearchers;

		private readonly Dictionary<UIntPtr, BannerInstance> _bannerInstances;

		private MBList<Agent> _nearbyAllyAgentsListCache = new MBList<Agent>();

		public Formation Formation { get; private set; }

		public ItemObject BannerItem { get; private set; }

		public bool HasBanner => BannerItem != null;

		public List<Agent> BannerBearers => (from instance in _bannerInstances.Values
			where instance.IsOnAgent
			select instance.BannerBearer).ToList();

		public List<GameEntity> BannersOnGround => (from instance in _bannerInstances.Values
			where instance.IsOnGround
			select instance.Entity).ToList();

		public int NumberOfBannerBearers => _bannerInstances.Values.Count((BannerInstance instance) => instance.IsOnAgent);

		public int NumberOfBanners => _bannerInstances.Count;

		public static float BannerSearchDistance => 9f;

		public FormationBannerController(Formation formation, ItemObject bannerItem, BannerBearerLogic bannerLogic, Mission mission)
		{
			Formation = formation;
			Formation.OnUnitAdded += OnAgentAdded;
			Formation.OnUnitRemoved += OnAgentRemoved;
			Formation.OnBeforeMovementOrderApplied += OnBeforeFormationMovementOrderApplied;
			Formation.OnAfterArrangementOrderApplied += OnAfterArrangementOrderApplied;
			_bannerInstances = new Dictionary<UIntPtr, BannerInstance>();
			_bannerSearchers = new Dictionary<Agent, (GameEntity, float)>();
			_requiresAgentStatUpdate = false;
			_lastActiveBannerBearerCount = 0;
			_bannerLogic = bannerLogic;
			_mission = mission;
			SetBannerItem(bannerItem);
		}

		public void SetBannerItem(ItemObject bannerItem)
		{
			if (bannerItem == null)
			{
				_ = 1;
			}
			else
				IsBannerItem(bannerItem);
			BannerItem = bannerItem;
		}

		public bool HasBannerEntity(GameEntity bannerEntity)
		{
			if (bannerEntity != null)
			{
				return _bannerInstances.Keys.Contains(bannerEntity.Pointer);
			}
			return false;
		}

		public bool HasBannerOnGround()
		{
			if (HasBanner)
			{
				return _bannerInstances.Any((KeyValuePair<UIntPtr, BannerInstance> instance) => instance.Value.IsOnGround);
			}
			return false;
		}

		public bool HasActiveBannerBearers()
		{
			return GetNumberOfActiveBannerBearers() > 0;
		}

		public bool IsBannerSearchingAgent(Agent agent)
		{
			return _bannerSearchers.Keys.Contains(agent);
		}

		public int GetNumberOfActiveBannerBearers()
		{
			int result = 0;
			if (HasBanner)
			{
				BattleBannerBearersModel bannerBearersModel = MissionGameModels.Current.BattleBannerBearersModel;
				result = _bannerInstances.Values.Count((BannerInstance instance) => instance.IsOnAgent && bannerBearersModel.CanBannerBearerProvideEffectToFormation(instance.BannerBearer, Formation));
			}
			return result;
		}

		public void UpdateAgentStats(bool forceUpdate = false)
		{
			if (forceUpdate || _requiresAgentStatUpdate)
			{
				Formation.ApplyActionOnEachUnit(delegate(Agent agent)
				{
					agent.UpdateAgentProperties();
					agent.MountAgent?.UpdateAgentProperties();
				});
				_requiresAgentStatUpdate = false;
			}
		}

		private void RepositionFormation()
		{
			Formation.SetMovementOrder(Formation.GetReadonlyMovementOrderReference());
			Formation.ApplyActionOnEachUnit(delegate(Agent agent)
			{
				agent.ForceUpdateCachedAndFormationValues(updateOnlyMovement: true, arrangementChangeAllowed: false);
			});
			Formation.SetHasPendingUnitPositions(hasPendingUnitPositions: false);
		}

		public void UpdateBannerSearchers()
		{
			List<GameEntity> bannersOnGround = BannersOnGround;
			if (!_bannerSearchers.IsEmpty())
			{
				List<Agent> list = new List<Agent>();
				foreach (KeyValuePair<Agent, (GameEntity, float)> searcherTuple in _bannerSearchers)
				{
					Agent key = searcherTuple.Key;
					if (key.IsActive())
					{
						if (!bannersOnGround.Any((GameEntity bannerEntity) => bannerEntity.Pointer == searcherTuple.Value.Item1.Pointer))
						{
							list.Add(key);
						}
					}
					else
					{
						list.Add(key);
					}
				}
				foreach (Agent item3 in list)
				{
					RemoveBannerSearcher(item3);
				}
			}
			foreach (GameEntity banner in bannersOnGround)
			{
				bool flag = false;
				if (_bannerSearchers.IsEmpty())
				{
					flag = true;
				}
				else
				{
					KeyValuePair<Agent, (GameEntity, float)> keyValuePair = _bannerSearchers.FirstOrDefault((KeyValuePair<Agent, (GameEntity bannerEntity, float lastDistance)> tuple) => tuple.Value.bannerEntity.Pointer == banner.Pointer);
					if (keyValuePair.Key == null)
					{
						flag = true;
					}
					else
					{
						Agent key2 = keyValuePair.Key;
						if (key2.IsActive())
						{
							GameEntity item = keyValuePair.Value.Item1;
							float item2 = keyValuePair.Value.Item2;
							float num = key2.Position.AsVec2.Distance(item.GlobalPosition.AsVec2);
							if (num <= item2 && num < BannerSearchDistance)
							{
								_bannerSearchers[key2] = (item, num);
							}
							else
							{
								RemoveBannerSearcher(key2);
								flag = true;
							}
						}
						else
						{
							RemoveBannerSearcher(key2);
							flag = true;
						}
					}
				}
				if (flag)
				{
					float distance;
					Agent agent = FindBestSearcherForBanner(banner, out distance);
					if (agent != null)
					{
						AddBannerSearcher(agent, banner, distance);
					}
				}
			}
		}

		public void UpdateBannerBearersForDeployment()
		{
			List<Agent> bannerBearers = BannerBearers;
			List<(Agent, bool)> list = new List<(Agent, bool)>();
			int num = 0;
			BattleBannerBearersModel battleBannerBearersModel = MissionGameModels.Current.BattleBannerBearersModel;
			if (battleBannerBearersModel.CanFormationDeployBannerBearers(Formation))
			{
				num = battleBannerBearersModel.GetDesiredNumberOfBannerBearersForFormation(Formation);
				foreach (Agent item2 in bannerBearers)
				{
					if (num > 0 && item2.Formation == Formation)
					{
						num--;
					}
					else
					{
						list.Add((item2, false));
					}
				}
			}
			else
			{
				foreach (Agent item3 in bannerBearers)
				{
					list.Add((item3, false));
				}
			}
			if (num > 0)
			{
				List<Agent> list2 = FindBannerBearableAgents(num);
				for (int i = 0; i < list2.Count; i++)
				{
					if (num <= 0)
					{
						break;
					}
					Agent item = list2[i];
					list.Add((item, true));
					num--;
				}
			}
			if (!list.IsEmpty())
			{
				BattleSideEnum side = Formation.Team.Side;
				_bannerLogic.AgentSpawnLogic.GetSpawnHorses(side);
				_ = _mission.PlayerTeam.Side;
				foreach (var item4 in list)
				{
					_bannerLogic.UpdateAgent(item4.Item1, item4.Item2);
				}
			}
			UpdateAgentStats();
			RepositionFormation();
			_bannerLogic.OnBannerBearersUpdated?.Invoke(Formation);
		}

		public void AddBannerEntity(GameEntity entity)
		{
			if (!_bannerInstances.ContainsKey(entity.Pointer))
			{
				_bannerInstances.Add(entity.Pointer, new BannerInstance(null, entity, BannerState.Initialized));
			}
		}

		public void RemoveBannerEntity(WeakGameEntity entity)
		{
			_bannerInstances.Remove(entity.Pointer);
			UpdateBannerSearchers();
			CheckRequiresAgentStatUpdate();
		}

		public void OnBannerEntityPickedUp(GameEntity entity, Agent agent)
		{
			_bannerInstances[entity.Pointer] = new BannerInstance(agent, entity, BannerState.OnAgent);
			if (agent.IsAIControlled)
			{
				agent.ResetEnemyCaches();
				agent.Defensiveness = 1f;
			}
			UpdateBannerSearchers();
			CheckRequiresAgentStatUpdate();
		}

		public void OnBannerEntityDropped(GameEntity entity)
		{
			_bannerInstances[entity.Pointer] = new BannerInstance(null, entity, BannerState.OnGround);
			UpdateBannerSearchers();
			CheckRequiresAgentStatUpdate();
		}

		public void OnBeforeFormationMovementOrderApplied(Formation formation, MovementOrder.MovementOrderEnum orderType)
		{
			if (formation == Formation)
			{
				UpdateBannerBearerArrangementPositions();
			}
		}

		public void OnAfterArrangementOrderApplied(Formation formation, ArrangementOrder.ArrangementOrderEnum orderEnum)
		{
			if (formation == Formation)
			{
				UpdateBannerBearerArrangementPositions();
			}
		}

		private Agent FindBestSearcherForBanner(GameEntity banner, out float distance)
		{
			distance = float.MaxValue;
			Agent result = null;
			Vec2 asVec = banner.GlobalPosition.AsVec2;
			_mission.GetNearbyAllyAgents(asVec, BannerSearchDistance, Formation.Team, _nearbyAllyAgentsListCache);
			BattleBannerBearersModel battleBannerBearersModel = MissionGameModels.Current.BattleBannerBearersModel;
			foreach (Agent item in _nearbyAllyAgentsListCache)
			{
				if (item.Formation == Formation && battleBannerBearersModel.CanAgentPickUpAnyBanner(item))
				{
					float num = item.Position.AsVec2.Distance(asVec);
					if (num < distance && !_bannerSearchers.ContainsKey(item))
					{
						result = item;
						distance = num;
					}
				}
			}
			return result;
		}

		private List<Agent> FindBannerBearableAgents(int count)
		{
			List<Agent> list = new List<Agent>();
			if (count > 0)
			{
				BattleBannerBearersModel bannerBearerModel = MissionGameModels.Current.BattleBannerBearersModel;
				foreach (IFormationUnit unitsWithoutLooseDetachedOne in Formation.UnitsWithoutLooseDetachedOnes)
				{
					if (unitsWithoutLooseDetachedOne is Agent agent && (agent.Banner == null || agent.Banner != BannerItem) && bannerBearerModel.CanAgentBecomeBannerBearer(agent))
					{
						list.Add(agent);
					}
				}
				list = list.OrderByDescending((Agent agent2) => bannerBearerModel.GetAgentBannerBearingPriority(agent2)).ToList();
			}
			return list;
		}

		private void UpdateBannerBearerArrangementPositions()
		{
			List<Agent> list = (from instance in _bannerInstances.Values
				where instance.IsOnAgent && instance.BannerBearer.Formation == Formation
				select instance.BannerBearer).ToList();
			List<FormationArrangementModel.ArrangementPosition> bannerBearerPositions = MissionGameModels.Current.FormationArrangementsModel.GetBannerBearerPositions(Formation, list.Count);
			if (bannerBearerPositions == null || bannerBearerPositions.IsEmpty())
			{
				return;
			}
			int num = 0;
			foreach (Agent item in list)
			{
				if (item == null || !item.IsAIControlled || item.Formation != Formation)
				{
					continue;
				}
				item.GetFormationFileAndRankInfo(out var fileIndex, out var rankIndex);
				for (; num < bannerBearerPositions.Count; num++)
				{
					FormationArrangementModel.ArrangementPosition arrangementPosition = bannerBearerPositions[num];
					int fileIndex2 = arrangementPosition.FileIndex;
					int rankIndex2 = arrangementPosition.RankIndex;
					bool flag = fileIndex == fileIndex2 && rankIndex == rankIndex2;
					if (!flag)
					{
						IFormationUnit unit = Formation.Arrangement.GetUnit(fileIndex2, rankIndex2);
						if (unit != null && unit is Agent agent)
						{
							if (agent == item)
							{
								flag = true;
							}
							else if (agent != Formation.Captain)
							{
								Formation.SwitchUnitLocations(item, agent);
								flag = true;
							}
						}
					}
					if (flag)
					{
						num++;
						break;
					}
				}
			}
		}

		private void OnAgentAdded(Formation formation, Agent agent)
		{
			if (Formation != formation)
			{
				return;
			}
			if (!_bannerLogic._isMissionEnded && _mission.Mode == MissionMode.Deployment && formation.Team.IsPlayerTeam && MissionGameModels.Current.BattleInitializationModel.CanPlayerSideDeployWithOrderOfBattle())
			{
				int minimumFormationTroopCountToBearBanners = MissionGameModels.Current.BattleBannerBearersModel.GetMinimumFormationTroopCountToBearBanners();
				if (formation.CountOfUnits == minimumFormationTroopCountToBearBanners && !_bannerLogic._playerFormationsRequiringUpdate.Contains(this))
				{
					_bannerLogic._playerFormationsRequiringUpdate.Add(this);
				}
			}
			else
			{
				UpdateBannerSearchers();
			}
		}

		private void OnAgentRemoved(Formation formation, Agent agent)
		{
			if (Formation != formation)
			{
				return;
			}
			if (!_bannerLogic._isMissionEnded && _mission.Mode == MissionMode.Deployment && formation.Team.IsPlayerTeam && MissionGameModels.Current.BattleInitializationModel.CanPlayerSideDeployWithOrderOfBattle())
			{
				int minimumFormationTroopCountToBearBanners = MissionGameModels.Current.BattleBannerBearersModel.GetMinimumFormationTroopCountToBearBanners();
				if (formation.CountOfUnits == minimumFormationTroopCountToBearBanners - 1 && !_bannerLogic._playerFormationsRequiringUpdate.Contains(this))
				{
					_bannerLogic._playerFormationsRequiringUpdate.Add(this);
				}
			}
			else
			{
				UpdateBannerSearchers();
			}
		}

		private void CheckRequiresAgentStatUpdate()
		{
			if (!_requiresAgentStatUpdate)
			{
				int numberOfActiveBannerBearers = GetNumberOfActiveBannerBearers();
				if ((numberOfActiveBannerBearers > 0 && _lastActiveBannerBearerCount == 0) || (numberOfActiveBannerBearers == 0 && _lastActiveBannerBearerCount > 0))
				{
					_requiresAgentStatUpdate = true;
					_lastActiveBannerBearerCount = numberOfActiveBannerBearers;
				}
			}
		}

		private void AddBannerSearcher(Agent searcher, GameEntity banner, float distance)
		{
			_bannerSearchers.Add(searcher, (banner, distance));
			searcher.HumanAIComponent?.DisablePickUpForAgentIfNeeded();
		}

		private void RemoveBannerSearcher(Agent searcher)
		{
			_bannerSearchers.Remove(searcher);
			if (searcher.IsActive())
			{
				searcher.HumanAIComponent?.DisablePickUpForAgentIfNeeded();
			}
		}
	}

	public const float DefaultBannerBearerAgentDefensiveness = 1f;

	public const float BannerSearcherUpdatePeriod = 3f;

	private readonly Dictionary<UIntPtr, FormationBannerController> _bannerToFormationMap = new Dictionary<UIntPtr, FormationBannerController>();

	private readonly Dictionary<Formation, FormationBannerController> _formationBannerData = new Dictionary<Formation, FormationBannerController>();

	private readonly Dictionary<Agent, Equipment> _initialSpawnEquipments = new Dictionary<Agent, Equipment>();

	private readonly BasicMissionTimer _bannerSearcherUpdateTimer;

	private readonly List<FormationBannerController> _playerFormationsRequiringUpdate = new List<FormationBannerController>();

	private bool _isMissionEnded;

	public IMissionAgentSpawnLogic AgentSpawnLogic { get; private set; }

	public event Action<Formation> OnBannerBearersUpdated;

	public event Action<Agent, bool> OnBannerBearerAgentUpdated;

	public BannerBearerLogic()
	{
		_bannerSearcherUpdateTimer = new BasicMissionTimer();
	}

	public bool IsFormationBanner(Formation formation, SpawnedItemEntity spawnedItem)
	{
		if (!IsBannerItem(spawnedItem.WeaponCopy.Item))
		{
			return false;
		}
		FormationBannerController formationControllerFromBannerEntity = GetFormationControllerFromBannerEntity(spawnedItem.GameEntity);
		if (formationControllerFromBannerEntity != null)
		{
			return formationControllerFromBannerEntity.Formation == formation;
		}
		return false;
	}

	public bool HasBannerOnGround(Formation formation)
	{
		return GetFormationControllerFromFormation(formation)?.HasBannerOnGround() ?? false;
	}

	public BannerComponent GetActiveBanner(Formation formation)
	{
		FormationBannerController formationControllerFromFormation = GetFormationControllerFromFormation(formation);
		if (formationControllerFromFormation != null)
		{
			if (!formationControllerFromFormation.HasActiveBannerBearers())
			{
				return null;
			}
			return formationControllerFromFormation.BannerItem.BannerComponent;
		}
		return null;
	}

	public List<Agent> GetFormationBannerBearers(Formation formation)
	{
		FormationBannerController formationControllerFromFormation = GetFormationControllerFromFormation(formation);
		if (formationControllerFromFormation != null)
		{
			return formationControllerFromFormation.BannerBearers;
		}
		return new List<Agent>();
	}

	public ItemObject GetFormationBanner(Formation formation)
	{
		ItemObject result = null;
		FormationBannerController formationControllerFromFormation = GetFormationControllerFromFormation(formation);
		if (formationControllerFromFormation != null)
		{
			result = formationControllerFromFormation.BannerItem;
		}
		return result;
	}

	public bool IsBannerSearchingAgent(Agent agent)
	{
		if (agent.Formation != null)
		{
			FormationBannerController formationControllerFromFormation = GetFormationControllerFromFormation(agent.Formation);
			if (formationControllerFromFormation != null)
			{
				return formationControllerFromFormation.IsBannerSearchingAgent(agent);
			}
		}
		return false;
	}

	public int GetMissingBannerCount(Formation formation)
	{
		FormationBannerController formationControllerFromFormation = GetFormationControllerFromFormation(formation);
		if (formationControllerFromFormation == null || formationControllerFromFormation.BannerItem == null)
		{
			return 0;
		}
		int num = MissionGameModels.Current.BattleBannerBearersModel.GetDesiredNumberOfBannerBearersForFormation(formation) - formationControllerFromFormation.NumberOfBanners;
		if (num <= 0)
		{
			return 0;
		}
		return num;
	}

	public Formation GetFormationFromBanner(SpawnedItemEntity spawnedItem)
	{
		WeakGameEntity gameEntity = spawnedItem.GameEntity;
		gameEntity = ((!gameEntity.IsValid) ? spawnedItem.GameEntityWithWorldPosition.GameEntity : gameEntity);
		return GetFormationControllerFromBannerEntity(gameEntity)?.Formation;
	}

	public void SetFormationBanner(Formation formation, ItemObject newBanner)
	{
		if (newBanner == null)
		{
			_ = 1;
		}
		else
			IsBannerItem(newBanner);
		FormationBannerController formationBannerController = GetFormationControllerFromFormation(formation);
		if (formationBannerController != null)
		{
			if (formationBannerController.BannerItem != newBanner)
			{
				formationBannerController.SetBannerItem(newBanner);
			}
		}
		else
		{
			formationBannerController = new FormationBannerController(formation, newBanner, this, base.Mission);
			_formationBannerData.Add(formation, formationBannerController);
		}
		formationBannerController.UpdateBannerBearersForDeployment();
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		MissionGameModels.Current.BattleBannerBearersModel.InitializeModel(this);
		AgentSpawnLogic = base.Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
		base.Mission.OnItemPickUp += OnItemPickup;
		base.Mission.OnItemDrop += OnItemDrop;
		_initialSpawnEquipments.Clear();
	}

	protected override void OnEndMission()
	{
		base.OnEndMission();
		MissionGameModels.Current.BattleBannerBearersModel.FinalizeModel();
		base.Mission.OnItemPickUp -= OnItemPickup;
		base.Mission.OnItemDrop -= OnItemDrop;
		AgentSpawnLogic = null;
		_isMissionEnded = true;
	}

	public override void OnDeploymentFinished()
	{
		_initialSpawnEquipments.Clear();
		_isMissionEnded = false;
	}

	public override void OnMissionTick(float dt)
	{
		if (_bannerSearcherUpdateTimer.ElapsedTime >= 3f)
		{
			foreach (FormationBannerController value in _formationBannerData.Values)
			{
				value.UpdateBannerSearchers();
			}
			_bannerSearcherUpdateTimer.Reset();
		}
		if (base.Mission.Mode != MissionMode.Deployment || _playerFormationsRequiringUpdate.IsEmpty())
		{
			return;
		}
		foreach (FormationBannerController item in _playerFormationsRequiringUpdate)
		{
			item.UpdateBannerBearersForDeployment();
		}
		_playerFormationsRequiringUpdate.Clear();
	}

	public void OnItemPickup(Agent agent, SpawnedItemEntity spawnedItem)
	{
		if (IsBannerItem(spawnedItem.WeaponCopy.Item))
		{
			WeakGameEntity gameEntity = spawnedItem.GameEntity;
			FormationBannerController formationControllerFromBannerEntity = GetFormationControllerFromBannerEntity(gameEntity);
			if (formationControllerFromBannerEntity != null)
			{
				formationControllerFromBannerEntity.OnBannerEntityPickedUp(GameEntity.CreateFromWeakEntity(gameEntity), agent);
				formationControllerFromBannerEntity.UpdateAgentStats();
			}
		}
	}

	public void OnItemDrop(Agent agent, SpawnedItemEntity spawnedItem)
	{
		if (IsBannerItem(spawnedItem.WeaponCopy.Item))
		{
			FormationBannerController formationControllerFromBannerEntity = GetFormationControllerFromBannerEntity(spawnedItem.GameEntity);
			if (formationControllerFromBannerEntity != null)
			{
				formationControllerFromBannerEntity.OnBannerEntityDropped(GameEntity.CreateFromWeakEntity(spawnedItem.GameEntity));
				formationControllerFromBannerEntity.UpdateAgentStats();
			}
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (affectedAgent.Banner != null && agentState == AgentState.Routed)
		{
			RemoveBannerOfAgent(affectedAgent);
		}
	}

	public override void OnAgentPanicked(Agent affectedAgent)
	{
		if (affectedAgent.Banner != null)
		{
			affectedAgent.Mission.AddTickAction(Mission.MissionTickAction.DropItem, affectedAgent, 4, 0);
		}
	}

	public void UpdateAgent(Agent agent, bool willBecomeBannerBearer)
	{
		if (willBecomeBannerBearer)
		{
			Formation formation = agent.Formation;
			FormationBannerController formationControllerFromFormation = GetFormationControllerFromFormation(formation);
			ItemObject bannerItem = formationControllerFromFormation.BannerItem;
			if (agent.Banner != null)
			{
				RemoveBannerOfAgent(agent);
			}
			Equipment newSpawnEquipment = CreateBannerEquipmentForAgent(agent, bannerItem);
			agent.UpdateSpawnEquipmentAndRefreshVisuals(newSpawnEquipment);
			GameEntity gameEntity = GameEntity.CreateFromWeakEntity(agent.GetWeaponEntityFromEquipmentSlot(EquipmentIndex.ExtraWeaponSlot));
			AddBannerEntity(formationControllerFromFormation, gameEntity);
			formationControllerFromFormation.OnBannerEntityPickedUp(gameEntity, agent);
		}
		else if (agent.Banner != null)
		{
			RemoveBannerOfAgent(agent);
			agent.UpdateSpawnEquipmentAndRefreshVisuals(_initialSpawnEquipments[agent]);
		}
		agent.ForceUpdateCachedAndFormationValues(updateOnlyMovement: false, arrangementChangeAllowed: false);
		agent.SetIsAIPaused(isPaused: true);
		this.OnBannerBearerAgentUpdated?.Invoke(agent, willBecomeBannerBearer);
	}

	public Agent SpawnBannerBearer(IAgentOriginBase troopOrigin, bool isPlayerSide, Formation formation, bool spawnWithHorse, bool isReinforcement, int formationTroopCount, int formationTroopIndex, bool isAlarmed, bool wieldInitialWeapons, Vec3? initialPosition, Vec2? initialDirection, string specialActionSetSuffix = null, bool useTroopClassForSpawn = false)
	{
		FormationBannerController formationControllerFromFormation = GetFormationControllerFromFormation(formation);
		ItemObject bannerItem = formationControllerFromFormation.BannerItem;
		Agent agent = base.Mission.SpawnTroop(troopOrigin, isPlayerSide, hasFormation: true, spawnWithHorse, isReinforcement, formationTroopCount, formationTroopIndex, isAlarmed, wieldInitialWeapons, initialPosition, initialDirection, specialActionSetSuffix, bannerItem, formationControllerFromFormation.Formation.FormationIndex, useTroopClassForSpawn);
		agent.ForceUpdateCachedAndFormationValues(updateOnlyMovement: false, arrangementChangeAllowed: false);
		GameEntity gameEntity = GameEntity.CreateFromWeakEntity(agent.GetWeaponEntityFromEquipmentSlot(EquipmentIndex.ExtraWeaponSlot));
		AddBannerEntity(formationControllerFromFormation, gameEntity);
		formationControllerFromFormation.OnBannerEntityPickedUp(gameEntity, agent);
		return agent;
	}

	public static bool IsBannerItem(ItemObject item)
	{
		if (item != null && item.IsBannerItem)
		{
			return item.BannerComponent != null;
		}
		return false;
	}

	private void AddBannerEntity(FormationBannerController formationBannerController, GameEntity bannerEntity)
	{
		_bannerToFormationMap.Add(bannerEntity.Pointer, formationBannerController);
		formationBannerController.AddBannerEntity(bannerEntity);
	}

	private void RemoveBannerEntity(FormationBannerController formationBannerController, WeakGameEntity bannerEntity)
	{
		_bannerToFormationMap.Remove(bannerEntity.Pointer);
		formationBannerController.RemoveBannerEntity(bannerEntity);
	}

	private FormationBannerController GetFormationControllerFromFormation(Formation formation)
	{
		if (!_formationBannerData.TryGetValue(formation, out var value))
		{
			return null;
		}
		return value;
	}

	private FormationBannerController GetFormationControllerFromBannerEntity(WeakGameEntity bannerEntity)
	{
		if (_bannerToFormationMap.TryGetValue(bannerEntity.Pointer, out var value))
		{
			return value;
		}
		return null;
	}

	private Equipment CreateBannerEquipmentForAgent(Agent agent, ItemObject bannerItem)
	{
		Equipment spawnEquipment = agent.SpawnEquipment;
		if (!_initialSpawnEquipments.ContainsKey(agent))
		{
			_initialSpawnEquipments[agent] = spawnEquipment;
		}
		Equipment equipment = new Equipment(spawnEquipment);
		ItemObject bannerBearerReplacementWeapon = MissionGameModels.Current.BattleBannerBearersModel.GetBannerBearerReplacementWeapon(agent.Character);
		equipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(bannerBearerReplacementWeapon);
		for (int i = 1; i < 4; i++)
		{
			equipment[i] = default(EquipmentElement);
		}
		equipment[EquipmentIndex.ExtraWeaponSlot] = new EquipmentElement(bannerItem);
		return equipment;
	}

	private void RemoveBannerOfAgent(Agent agent)
	{
		WeakGameEntity weaponEntityFromEquipmentSlot = agent.GetWeaponEntityFromEquipmentSlot(EquipmentIndex.ExtraWeaponSlot);
		FormationBannerController formationControllerFromBannerEntity = GetFormationControllerFromBannerEntity(weaponEntityFromEquipmentSlot);
		if (formationControllerFromBannerEntity != null)
		{
			RemoveBannerEntity(formationControllerFromBannerEntity, weaponEntityFromEquipmentSlot);
			formationControllerFromBannerEntity.UpdateAgentStats();
		}
	}
}
