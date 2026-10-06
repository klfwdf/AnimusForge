using System;
using System.Collections.Generic;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.MissionLogics;

internal class NavalShipAgents
{
	private readonly MBList<Agent> _activeAgents = new MBList<Agent>();

	private readonly MBList<Agent> _activeHeroAgents = new MBList<Agent>();

	private readonly MBList<Agent> _activeNonHeroAgents = new MBList<Agent>();

	private readonly MBSortedMultiList<int, NavalTroopAssignment> _reservedOrderedTroops = new MBSortedMultiList<int, NavalTroopAssignment>();

	private readonly Dictionary<IAgentOriginBase, NavalTroopAssignment> _reservedTroops = new Dictionary<IAgentOriginBase, NavalTroopAssignment>();

	private readonly MissionTimer _reinforcementTimer;

	private readonly NavalTeamAgents _teamAgents;

	private TroopTraitsMask _compatibilityTraitsFilter;

	public TroopTraitsMask TroopClassFilter { get; private set; }

	public TroopTraitsMask TroopTraitsFilter { get; private set; }

	internal MBReadOnlyList<Agent> ActiveAgents => _activeAgents;

	internal MBReadOnlyList<Agent> ActiveHeroAgents => _activeHeroAgents;

	internal MBReadOnlyList<Agent> ActiveNonHeroAgents => _activeNonHeroAgents;

	internal MBSortedMultiList<int, NavalTroopAssignment> ReservedTroops => _reservedOrderedTroops;

	internal int ReservedHeroesCount { get; private set; }

	internal int ReservedNonHeroesCount => _reservedTroops.Count - ReservedHeroesCount;

	internal int ReservedTroopsCount => _reservedTroops.Count;

	internal int AllTroopsCount => _reservedTroops.Count + _activeAgents.Count;

	internal MissionShip Ship { get; private set; }

	internal bool CanAddMoreReserves
	{
		get
		{
			if (!IgnoreCapacityChecks)
			{
				return HasMissingTroops;
			}
			return true;
		}
	}

	internal bool CanAddMoreAgents
	{
		get
		{
			if (!IgnoreCapacityChecks)
			{
				if (HasMissingAgentsOnMainDeck)
				{
					return HasMissingTroops;
				}
				return false;
			}
			return true;
		}
	}

	internal bool SpawnReinforcements { get; private set; } = true;

	internal bool IgnoreCapacityChecks { get; private set; }

	internal bool HasPlayerAgent
	{
		get
		{
			if (Agent.Main != null)
			{
				return _activeHeroAgents.Contains(Agent.Main);
			}
			return false;
		}
	}

	internal int DesiredTroopCount { get; private set; }

	internal bool HasMissingTroops => MissingTroopCount > 0;

	internal int MissingTroopCount => TaleWorlds.Library.MathF.Max(0, TaleWorlds.Library.MathF.Min(DesiredTroopCount, Ship.TotalCrewCapacity) - AllTroopsCount);

	internal int MissingAgentCountOnMainDeck => TaleWorlds.Library.MathF.Max(0, TaleWorlds.Library.MathF.Min(DesiredTroopCount, Ship.CrewSizeOnMainDeck) - ActiveAgents.Count);

	internal bool HasMissingAgentsOnMainDeck => MissingAgentCountOnMainDeck > 0;

	internal float TroopFillRatio
	{
		get
		{
			if (DesiredTroopCount <= 0)
			{
				if (AllTroopsCount != 0)
				{
					return float.MaxValue;
				}
				return 0f;
			}
			return (float)AllTroopsCount / (float)DesiredTroopCount;
		}
	}

	internal NavalShipAgents(MissionShip ship, NavalTeamAgents teamAgents)
	{
		Ship = ship;
		_teamAgents = teamAgents;
		DesiredTroopCount = Ship.TotalCrewCapacity;
		_reinforcementTimer = new MissionTimer(0f);
		SetTroopClassFilter(TroopTraitsMask.Melee | TroopTraitsMask.Ranged);
		SetTroopTraitsFilter(TroopTraitsMask.None);
	}

	internal void InitializeReinforcementTimer(bool randomizeTimers)
	{
		if (randomizeTimers)
		{
			_reinforcementTimer.Set(MBRandom.RandomFloat * _reinforcementTimer.GetTimerDuration());
		}
		else
		{
			_reinforcementTimer.Reset();
		}
	}

	internal void SetReinforcementSpawnDuration(float duration = 0f)
	{
		if (duration <= 0f)
		{
			duration = NavalAgentsLogic.ComputeReinforcementSpawnDuration(ReservedTroopsCount);
		}
		_reinforcementTimer.SetDuration(duration);
	}

	internal void SetIgnoreCapacityChecks(bool value)
	{
		IgnoreCapacityChecks = value;
	}

	internal void SetSpawnReinforcements(bool value)
	{
		if (SpawnReinforcements != value)
		{
			SpawnReinforcements = value;
		}
	}

	internal void SetTroopClassFilter(TroopTraitsMask troopClassFilter)
	{
		TroopClassFilter = troopClassFilter;
	}

	internal void SetTroopTraitsFilter(TroopTraitsMask troopTraitsFilter)
	{
		TroopTraitsFilter = troopTraitsFilter;
		_compatibilityTraitsFilter = troopTraitsFilter & ~(TroopTraitsMask.LowTier | TroopTraitsMask.HighTier);
	}

	internal bool IsAgentCompatibleWithShip(Agent agent, bool checkDynamicCompatibility = false)
	{
		if (agent == Ship.Captain || agent.IsPlayerTroop)
		{
			return true;
		}
		TroopTraitsMask troopTraitsMask = (checkDynamicCompatibility ? agent.GetTraitsMask() : agent.Origin.GetTraitsMask());
		if (IsTroopCompatibleWithClassFilter(troopTraitsMask))
		{
			return IsTroopCompatibleWithTraitsFilter(troopTraitsMask, agent.Character.GetBattleTier());
		}
		return false;
	}

	internal bool IsTroopCompatibleWithShip(IAgentOriginBase troopOrigin)
	{
		if (troopOrigin.Troop.IsPlayerCharacter)
		{
			return true;
		}
		TroopTraitsMask traitsMask = troopOrigin.GetTraitsMask();
		bool num = IsTroopCompatibleWithClassFilter(traitsMask);
		bool flag = IsTroopCompatibleWithTraitsFilter(traitsMask, troopOrigin.Troop.GetBattleTier());
		return num && flag;
	}

	internal int GetTraitsFilterPriority(NavalTroopAssignment troopAssignment, bool checkDynamicCompatibility = false)
	{
		if (troopAssignment.Agent != null && checkDynamicCompatibility)
		{
			Agent agent = troopAssignment.Agent;
			return TroopFilteringUtilities.GetTroopPriority(agent.GetTraitsMask(), agent.Origin.Troop.GetBattleTier(), TroopTraitsFilter);
		}
		IAgentOriginBase origin = troopAssignment.Origin;
		return TroopFilteringUtilities.GetTroopPriority(origin.GetTraitsMask(), origin.Troop.GetBattleTier(), TroopTraitsFilter);
	}

	internal bool IsTroopCompatibleWithClassFilter(TroopTraitsMask troopClassMask)
	{
		return (troopClassMask & TroopClassFilter) != 0;
	}

	internal bool IsTroopCompatibleWithTraitsFilter(TroopTraitsMask troopTraitsMask, int troopBattleTier)
	{
		if (TroopTraitsFilter == TroopTraitsMask.None)
		{
			return true;
		}
		if ((troopTraitsMask & _compatibilityTraitsFilter) == _compatibilityTraitsFilter)
		{
			float num = troopBattleTier;
			float num2 = 3.5f;
			if ((TroopTraitsFilter & TroopTraitsMask.HighTier) != TroopTraitsMask.None && num >= num2)
			{
				return true;
			}
			if ((TroopTraitsFilter & TroopTraitsMask.LowTier) != TroopTraitsMask.None && num <= num2)
			{
				return true;
			}
		}
		return false;
	}

	public void OnShipCaptured(MissionShip newShip)
	{
		int num = newShip?.TotalCrewCapacity ?? 0;
		int num2 = newShip?.CrewSizeOnMainDeck ?? 0;
		DesiredTroopCount = ((DesiredTroopCount == Ship.TotalCrewCapacity) ? num : Math.Min(DesiredTroopCount, num));
		Ship = newShip;
		int num3 = num - num2;
		while (ReservedTroopsCount > num3)
		{
			DequeueReservedTroop(out var dequeuedTroop);
			_teamAgents.RemoveTroopOriginAux(dequeuedTroop.Origin);
		}
	}

	internal void SetDesiredTroopCount(int value)
	{
		DesiredTroopCount = value;
	}

	internal bool IsOriginInReserves(IAgentOriginBase origin)
	{
		return _reservedTroops.ContainsKey(origin);
	}

	internal void EnqueueReservedTroop(in NavalTroopAssignment troop)
	{
		_reservedOrderedTroops.Add(troop.Priority, troop);
		_reservedTroops.Add(troop.Origin, troop);
		if (troop.Origin.Troop.IsHero)
		{
			ReservedHeroesCount++;
		}
		_teamAgents.AgentsLogic.InvokeTroopAddedToReserves(troop.Origin, Ship);
	}

	internal bool DequeueReservedTroop(out NavalTroopAssignment dequeuedTroop)
	{
		dequeuedTroop = NavalTroopAssignment.Invalid();
		if (_reservedOrderedTroops.Count > 0)
		{
			dequeuedTroop = _reservedOrderedTroops.LastValue;
			_reservedOrderedTroops.RemoveLast();
			_reservedTroops.Remove(dequeuedTroop.Origin);
			if (dequeuedTroop.Origin.Troop.IsHero)
			{
				ReservedHeroesCount--;
			}
		}
		if (dequeuedTroop.IsValid)
		{
			_teamAgents.AgentsLogic.InvokeTroopRemovedFromReserves(dequeuedTroop.Origin, Ship);
		}
		return dequeuedTroop.IsValid;
	}

	internal bool DequeueReservedTroop(IAgentOriginBase origin, out NavalTroopAssignment dequeuedTroop)
	{
		dequeuedTroop = NavalTroopAssignment.Invalid();
		if (_reservedTroops.TryGetValue(origin, out dequeuedTroop))
		{
			int priority = NavalTroopAssignment.GetPriority(dequeuedTroop.Origin, dequeuedTroop.Agent);
			for (int i = _reservedOrderedTroops.FirstIndexOf(priority); i <= _reservedOrderedTroops.Count; i++)
			{
				if (_reservedOrderedTroops[i].Origin == origin)
				{
					_reservedOrderedTroops.RemoveAt(i);
					_reservedTroops.Remove(origin);
					if (origin.Troop.IsHero)
					{
						ReservedHeroesCount--;
					}
					break;
				}
			}
		}
		if (dequeuedTroop.IsValid)
		{
			_teamAgents.AgentsLogic.InvokeTroopRemovedFromReserves(origin, Ship);
		}
		return dequeuedTroop.IsValid;
	}

	internal void FillReservedTroops(MBList<IAgentOriginBase> reservedTroops)
	{
		foreach (KeyValuePair<IAgentOriginBase, NavalTroopAssignment> reservedTroop in _reservedTroops)
		{
			reservedTroops.Add(reservedTroop.Key);
		}
	}

	internal void AddAgent(Agent agent)
	{
		_teamAgents.SetManagedAgentFormation(agent, Ship.Formation);
		_activeAgents.Add(agent);
		if (agent.IsHero)
		{
			_activeHeroAgents.Add(agent);
		}
		else
		{
			_activeNonHeroAgents.Add(agent);
		}
		_teamAgents.AgentsLogic.InvokeAgentAddedToShip(agent, Ship);
	}

	internal void RemoveAgent(Agent agent)
	{
		NavalAgentsLogic.TryStopMachineUseAndReattachAgent(agent);
		agent.TryRemoveAllDetachmentScores();
		if (agent.IsHero)
		{
			_activeHeroAgents.Remove(agent);
		}
		else
		{
			_activeNonHeroAgents.Remove(agent);
		}
		_activeAgents.Remove(agent);
		_teamAgents.SetManagedAgentFormation(agent, null);
		_teamAgents.AgentsLogic.InvokeAgentRemovedFromShip(agent, Ship);
	}

	internal (int spawnedCount, int reassignedCount) SpawnNextBatch(bool isReinforcement, MBList<Agent> spawnedAgents)
	{
		int num = 0;
		int num2 = 0;
		int num3 = TaleWorlds.Library.MathF.Min(ReservedTroopsCount, MissingAgentCountOnMainDeck);
		if (isReinforcement && num3 > 0)
		{
			num3 = Math.Min(Ship.CrewSpawnLocalFrames.Count, num3);
		}
		if (num3 > 0)
		{
			while (num < num3)
			{
				DequeueReservedTroop(out var dequeuedTroop);
				Agent item;
				if (dequeuedTroop.Agent != null)
				{
					item = ReassignFromUnassignedReserves(dequeuedTroop);
				}
				else
				{
					item = SpawnAgentAux(dequeuedTroop.Origin, isReinforcement);
					num2++;
				}
				num++;
				spawnedAgents.Add(item);
			}
		}
		int item2 = num - num2;
		return (spawnedCount: num2, reassignedCount: item2);
	}

	internal int CheckSpawnReinforcements(MBList<Agent> spawnedAgents)
	{
		int num = 0;
		if (_reinforcementTimer.Check(reset: true) && Ship?.Team != null && !Ship.IsShipNavmeshDisabled)
		{
			num = SpawnNextBatch(isReinforcement: true, spawnedAgents).spawnedCount;
			if (num > 0)
			{
				float duration = NavalAgentsLogic.ComputeReinforcementSpawnDuration(ReservedTroopsCount);
				_reinforcementTimer.SetDuration(duration);
			}
		}
		return num;
	}

	internal Agent SpawnHeroFromReserve(IAgentOriginBase heroOrigin, out bool isReassigned)
	{
		DequeueReservedTroop(heroOrigin, out var dequeuedTroop);
		isReassigned = false;
		Agent result;
		if (dequeuedTroop.Agent != null)
		{
			result = ReassignFromUnassignedReserves(dequeuedTroop);
			isReassigned = true;
		}
		else
		{
			result = SpawnAgentAux(heroOrigin, isReinforcement: false);
		}
		return result;
	}

	internal void AssignAndTeleportCrewToShipMachines()
	{
		Mission mission = _teamAgents.AgentsLogic.Mission;
		ShipControllerMachine shipControllerMachine = Ship.ShipControllerMachine;
		Ship.ShipOrder.ManageShipDetachments();
		if (Ship.ShipPlacementDetachment.IsUsedByFormation(Ship.Formation))
		{
			do
			{
				Ship.ShipPlacementDetachment.Tick();
			}
			while (Ship.ShipPlacementDetachment.IsTickRequired);
		}
		bool isTeleportingAgents = mission.IsTeleportingAgents;
		mission.IsTeleportingAgents = true;
		if (shipControllerMachine.PilotStandingPoint.HasAIMovingTo)
		{
			shipControllerMachine.PilotStandingPoint.MovingAgent.UseGameObject(shipControllerMachine.PilotStandingPoint);
			shipControllerMachine.OnPilotAssignedDuringSpawn();
		}
		if (Ship.Captain != null && shipControllerMachine.PilotAgent != Ship.Captain && (Ship.Captain != Agent.Main || !Ship.HasPlayerStandingPointEntity) && (Ship.Captain.IsAIControlled ? (!shipControllerMachine.IsDisabledForAI) : (!shipControllerMachine.PilotStandingPoint.IsDisabledForPlayers)))
		{
			Ship.Captain.UseGameObject(shipControllerMachine.PilotStandingPoint);
			shipControllerMachine.OnPilotAssignedDuringSpawn();
		}
		foreach (ShipOarMachine shipOarMachine in Ship.ShipOarMachines)
		{
			if (shipOarMachine.PilotStandingPoint.HasAIMovingTo)
			{
				shipOarMachine.PilotStandingPoint.MovingAgent.UseGameObject(shipOarMachine.PilotStandingPoint);
				shipOarMachine.OnPilotAssignedDuringSpawn();
			}
		}
		if (Ship.ShipSiegeWeapon != null)
		{
			RangedSiegeWeapon shipSiegeWeapon = Ship.ShipSiegeWeapon;
			if (shipSiegeWeapon.PilotStandingPoint.HasAIMovingTo)
			{
				shipSiegeWeapon.PilotStandingPoint.MovingAgent.UseGameObject(shipSiegeWeapon.PilotStandingPoint);
				shipSiegeWeapon.OnPilotAssignedDuringSpawn();
			}
		}
		Agent main;
		if (Ship.HasPlayerStandingPointEntity && Ship.IsPlayerShip && (main = Agent.Main) != null)
		{
			MatrixFrame globalFrame = Ship.PlayerStandingPointEntity.GetGlobalFrame();
			Vec3 origin = globalFrame.origin;
			Vec2 direction = globalFrame.rotation.f.AsVec2.Normalized();
			main.TeleportToPosition(origin);
			main.SetMovementDirection(in direction);
		}
		foreach (Agent activeAgent in _activeAgents)
		{
			if (activeAgent.IsAIControlled && !activeAgent.InteractingWithAnyGameObject())
			{
				activeAgent.ForceUpdateCachedAndFormationValues(updateOnlyMovement: true, arrangementChangeAllowed: false);
			}
		}
		mission.IsTeleportingAgents = isTeleportingAgents;
	}

	internal void OnEndDeploymentMode()
	{
		List<KeyValuePair<int, NavalTroopAssignment>> list = new List<KeyValuePair<int, NavalTroopAssignment>>();
		foreach (NavalTroopAssignment reservedOrderedTroop in _reservedOrderedTroops)
		{
			if (reservedOrderedTroop.HasAgent)
			{
				KeyValuePair<int, NavalTroopAssignment> item = new KeyValuePair<int, NavalTroopAssignment>(reservedOrderedTroop.Priority, NavalTroopAssignment.Create(reservedOrderedTroop.Origin));
				list.Add(item);
			}
		}
		_reservedOrderedTroops.RemoveAll((KeyValuePair<int, NavalTroopAssignment> tuple) => tuple.Value.HasAgent);
		_reservedOrderedTroops.AddRange(list);
		foreach (KeyValuePair<int, NavalTroopAssignment> item2 in list)
		{
			IAgentOriginBase origin = item2.Value.Origin;
			_reservedTroops[origin] = NavalTroopAssignment.Create(origin);
		}
	}

	private Agent SpawnAgentAux(IAgentOriginBase agentOrigin, bool isReinforcement)
	{
		MatrixFrame crewSpawnGlobalFrame;
		if (isReinforcement)
		{
			Ship.GetNextCrewSpawnGlobalFrame(out crewSpawnGlobalFrame);
		}
		else
		{
			crewSpawnGlobalFrame = Ship.GetNextOuterInnerSpawnGlobalFrame();
		}
		Vec2 value = crewSpawnGlobalFrame.rotation.f.AsVec2.Normalized();
		bool isPlayerSide = Ship.BattleSide == Mission.Current.PlayerTeam.Side;
		Agent agent = Mission.Current.SpawnTroop(agentOrigin, isPlayerSide, hasFormation: true, spawnWithHorse: false, isReinforcement: false, 0, 0, isAlarmed: true, wieldInitialWeapons: true, crewSpawnGlobalFrame.origin, value, null, null, Ship.FormationIndex);
		bool flag = false;
		if (!Mission.Current.IsNavalRaidBattle)
		{
			foreach (ShipVisualSlotInfo shipVisualSlotInfo in Ship.ShipOrigin.GetShipVisualSlotInfos())
			{
				if (shipVisualSlotInfo.VisualSlotTag == "side" && shipVisualSlotInfo.VisualPieceId == "brazier")
				{
					flag = true;
				}
			}
		}
		if (agent != null && flag)
		{
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.ExtraWeaponSlot; equipmentIndex++)
			{
				EquipmentElement equipmentElement = agent.SpawnEquipment[equipmentIndex];
				ItemObject item = equipmentElement.Item;
				if (item != null && item.ItemType == ItemObject.ItemTypeEnum.Arrows)
				{
					ItemObject item2 = Game.Current.ObjectManager.GetObject<ItemObject>("burning_arrows");
					EquipmentElement value2 = new EquipmentElement(item2);
					agent.SpawnEquipment[equipmentIndex] = value2;
					MissionWeapon weapon = new MissionWeapon(value2.Item, value2.ItemModifier, agentOrigin.Banner);
					agent.RemoveEquippedWeapon(equipmentIndex);
					agent.EquipWeaponWithNewEntity(equipmentIndex, ref weapon);
					continue;
				}
				ItemObject item3 = equipmentElement.Item;
				if (item3 != null && item3.ItemType == ItemObject.ItemTypeEnum.Bolts)
				{
					ItemObject item4 = Game.Current.ObjectManager.GetObject<ItemObject>("burning_bolts");
					EquipmentElement value3 = new EquipmentElement(item4);
					agent.SpawnEquipment[equipmentIndex] = value3;
					MissionWeapon weapon2 = new MissionWeapon(value3.Item, value3.ItemModifier, agentOrigin.Banner);
					agent.RemoveEquippedWeapon(equipmentIndex);
					agent.EquipWeaponWithNewEntity(equipmentIndex, ref weapon2);
				}
			}
		}
		agent.GetComponent<HumanAIComponent>()?.ForceDisablePickUpForAgent();
		AddAgent(agent);
		return agent;
	}

	private Agent ReassignFromUnassignedReserves(NavalTroopAssignment suppliedTroop)
	{
		_teamAgents.ReassignAgentAux(this, suppliedTroop.Agent);
		return suppliedTroop.Agent;
	}

	internal Agent GetMinimumPriorityActiveAgent(MBList<Agent> agentsToIgnore = null)
	{
		Agent result = null;
		float num = float.MaxValue;
		bool flag = agentsToIgnore != null && agentsToIgnore.Count > 0;
		foreach (Agent activeAgent in ActiveAgents)
		{
			if (!flag || !agentsToIgnore.Contains(activeAgent))
			{
				float agentPriority = NavalAgentsLogic.GetAgentPriority(activeAgent);
				if (agentPriority <= num)
				{
					result = activeAgent;
					num = agentPriority;
				}
			}
		}
		return result;
	}
}
