using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.Deployment;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.MissionLogics;

internal class NavalTeamAgents
{
	private struct TroopCountData
	{
		private int _nonHeroOriginsCount;

		private int _heroOriginsCount;

		private int _nonHeroAgentsCount;

		private int _heroAgentsCount;

		public int NonHeroOriginsCount => _nonHeroOriginsCount;

		public int HeroOriginsCount => _heroOriginsCount;

		public int NonHeroAgentsCount => _nonHeroAgentsCount;

		public int HeroAgentsCount => _heroAgentsCount;

		public int OriginsCount => _nonHeroOriginsCount + _heroOriginsCount;

		public int AgentsCount => _nonHeroAgentsCount + _heroAgentsCount;

		public void Add(in NavalTroopAssignment troop)
		{
			if (troop.HasAgent)
			{
				if (troop.Agent.IsHero)
				{
					_heroAgentsCount++;
				}
				else
				{
					_nonHeroAgentsCount++;
				}
			}
			else if (troop.Origin.Troop.IsHero)
			{
				_heroOriginsCount++;
			}
			else
			{
				_nonHeroOriginsCount++;
			}
		}

		public void Remove(in NavalTroopAssignment troop)
		{
			if (troop.HasAgent)
			{
				if (troop.Agent.IsHero)
				{
					_heroAgentsCount--;
				}
				else
				{
					_nonHeroAgentsCount--;
				}
			}
			else if (troop.Origin.Troop.IsHero)
			{
				_heroOriginsCount--;
			}
			else
			{
				_nonHeroOriginsCount--;
			}
		}

		public bool Equals(in TroopCountData other)
		{
			if (_heroOriginsCount == other.HeroOriginsCount && _nonHeroOriginsCount == other.NonHeroOriginsCount && _heroAgentsCount == other.HeroAgentsCount)
			{
				return _nonHeroAgentsCount == other.NonHeroAgentsCount;
			}
			return false;
		}
	}

	internal readonly BattleSideEnum BattleSide;

	internal readonly TeamSideEnum TeamSide;

	internal readonly NavalAgentsLogic AgentsLogic;

	private readonly HashSet<IAgentOriginBase> _allTroopOrigins;

	private readonly HashSet<IAgentOriginBase> _allHeroOrigins;

	private readonly MBList<NavalShipAgents> _allShipAgents;

	private readonly Dictionary<IAgentOriginBase, NavalTroopAssignment> _unassignedTroops;

	private readonly Dictionary<Agent, NavalShipAgents> _agentToShipAgents;

	private readonly MBSortedMultiList<int, NavalTroopAssignment> _unassignedOrderedTroops;

	private TroopCountData _unassignedTroopCountData;

	private readonly Dictionary<Agent, MissionShip> _unassignedReservedAgents;

	private MBList<Agent> _tempSpawnedAgentsList;

	private MBList<NavalTroopAssignment> _tempUnassignedTroops;

	private MBList<NavalShipAgents> _tempShipsWithMissingTroops;

	private MBList<Agent> _tempIncompatibleAgentsList;

	private MBList<IAgentOriginBase> _tempIncompatibleReservesList;

	private MBList<Agent> _tempAgentsNotUsingMachines;

	private MBList<Agent> _recentlySwappedAgents = new MBList<Agent>();

	internal IReadOnlyCollection<IAgentOriginBase> AllTroopOrigins => _allTroopOrigins;

	internal IReadOnlyCollection<IAgentOriginBase> AllHeroOrigins => _allHeroOrigins;

	internal int NumberOfSpawnedAgents { get; private set; }

	internal int NumberOfActiveTroops => _agentToShipAgents.Count;

	internal int NumberOfUnassignedTroops => _unassignedTroops.Count;

	internal bool SpawnReinforcementsOnTick { get; private set; }

	public bool RestrictRecentlySwappedAgentTransfers { get; private set; }

	internal NavalTeamAgents(NavalAgentsLogic agentsLogic, BattleSideEnum battleSide, TeamSideEnum teamSide)
	{
		AgentsLogic = agentsLogic;
		BattleSide = battleSide;
		TeamSide = teamSide;
		_allTroopOrigins = new HashSet<IAgentOriginBase>();
		_allHeroOrigins = new HashSet<IAgentOriginBase>();
		_unassignedTroops = new Dictionary<IAgentOriginBase, NavalTroopAssignment>();
		_unassignedOrderedTroops = new MBSortedMultiList<int, NavalTroopAssignment>();
		_unassignedTroopCountData = default(TroopCountData);
		_unassignedReservedAgents = new Dictionary<Agent, MissionShip>();
		_allShipAgents = new MBList<NavalShipAgents>();
		_agentToShipAgents = new Dictionary<Agent, NavalShipAgents>();
		_tempSpawnedAgentsList = new MBList<Agent>();
		_tempShipsWithMissingTroops = new MBList<NavalShipAgents>();
		_tempUnassignedTroops = new MBList<NavalTroopAssignment>();
		_tempAgentsNotUsingMachines = new MBList<Agent>();
		_tempIncompatibleAgentsList = new MBList<Agent>();
		_tempIncompatibleReservesList = new MBList<IAgentOriginBase>();
	}

	internal void AddAgentToShip(Agent agent, MissionShip targetShip)
	{
		MissionShip ship;
		bool num = IsAgentOnAnyShip(agent, out ship);
		bool flag = _unassignedTroops.ContainsKey(agent.Origin);
		if (!num && !flag)
		{
			TryGetShipAgents(targetShip, out var shipAgents);
			AddTroopOriginAux(agent.Origin);
			if (AgentsLogic.IsDeploymentMode)
			{
				MakeSpaceForOneAgent(shipAgents);
			}
			AddAgentAux(agent, shipAgents);
		}
	}

	internal void RemoveAgentFromShip(Agent agent, MissionShip ship)
	{
		IsAgentOnAnyShip(agent, out var _);
		TryGetShipAgents(ship, out var shipAgents);
		RemoveAgentAux(agent, shipAgents);
		RemoveTroopOriginAux(agent.Origin);
	}

	internal int GetNumberOfReservedTroops(bool spawnableOnly)
	{
		int num = 0;
		foreach (NavalShipAgents allShipAgent in _allShipAgents)
		{
			if (allShipAgent.SpawnReinforcements)
			{
				num += allShipAgent.ReservedTroopsCount;
			}
		}
		return num;
	}

	internal bool AddReservedTroopToShip(IAgentOriginBase troopOrigin, MissionShip ship)
	{
		TryGetShipAgents(ship, out var shipAgents);
		return AddReservedTroopToShipAux(troopOrigin, shipAgents);
	}

	internal int AddReservedTroopsToShip(MBList<IAgentOriginBase> troopOrigins, MissionShip ship)
	{
		int num = 0;
		TryGetShipAgents(ship, out var shipAgents);
		foreach (IAgentOriginBase troopOrigin in troopOrigins)
		{
			if (AddReservedTroopToShipAux(troopOrigin, shipAgents))
			{
				num++;
			}
		}
		return num;
	}

	internal void RemoveReservedTroopFromShip(IAgentOriginBase troopOrigin, MissionShip ship)
	{
		TryGetShipAgents(ship, out var shipAgents);
		RemoveReservedTroopFromShipAux(troopOrigin, shipAgents);
	}

	internal void RemoveReservedTroopsFromShip(MBList<IAgentOriginBase> troopOrigins, MissionShip ship)
	{
		TryGetShipAgents(ship, out var shipAgents);
		foreach (IAgentOriginBase troopOrigin in troopOrigins)
		{
			RemoveReservedTroopFromShipAux(troopOrigin, shipAgents);
		}
	}

	internal int RemoveReservedTroopsFromShip(MissionShip ship, int count)
	{
		int i = 0;
		TryGetShipAgents(ship, out var shipAgents);
		for (count = ((count > 0) ? TaleWorlds.Library.MathF.Min(shipAgents.ReservedTroopsCount, count) : shipAgents.ReservedTroopsCount); i < count; i++)
		{
			if (!RemoveReservedTroopFromShipAux(shipAgents))
			{
				break;
			}
		}
		return i;
	}

	internal void RemoveAllReservedTroopsFromShip(MissionShip ship)
	{
		TryGetShipAgents(ship, out var shipAgents);
		int reservedTroopsCount = shipAgents.ReservedTroopsCount;
		RemoveReservedTroopsFromShip(ship, reservedTroopsCount);
	}

	internal bool TransferAgentToShip(Agent agent, MissionShip targetShip, bool swapAgents)
	{
		_agentToShipAgents.TryGetValue(agent, out var value);
		TryGetShipAgents(targetShip, out var shipAgents);
		bool flag = false;
		if (value == shipAgents)
		{
			flag = true;
		}
		else
		{
			if (swapAgents && AgentsLogic.IsDeploymentMode && shipAgents.ActiveAgents.Count > 0)
			{
				Agent minimumPriorityActiveAgent = shipAgents.GetMinimumPriorityActiveAgent(_recentlySwappedAgents);
				RemoveAgentAux(minimumPriorityActiveAgent, shipAgents);
				MakeSpaceForOneAgent(shipAgents);
				TransferAgentAux(agent, value, shipAgents);
				AddAgentAux(minimumPriorityActiveAgent, value);
				if (RestrictRecentlySwappedAgentTransfers && !_recentlySwappedAgents.Contains(minimumPriorityActiveAgent))
				{
					_recentlySwappedAgents.Add(minimumPriorityActiveAgent);
				}
				flag = true;
			}
			else if (shipAgents.CanAddMoreAgents || AgentsLogic.IsDeploymentMode)
			{
				if (AgentsLogic.IsDeploymentMode)
				{
					MakeSpaceForOneAgent(shipAgents);
				}
				TransferAgentAux(agent, value, shipAgents);
				flag = true;
			}
			if (flag && value.Ship.Formation?.Captain == agent)
			{
				SetManagedCaptainOfFormation(null, value.Ship.Formation);
			}
		}
		return flag;
	}

	internal void AssignCaptainToShip(Agent captainAgent, MissionShip targetShip, bool swapOnTransfer, MissionShip captainsCurrentShip)
	{
		TryGetShipAgents(targetShip, out var shipAgents);
		Formation formation = shipAgents.Ship.Formation;
		if (targetShip.Captain == captainAgent)
		{
			return;
		}
		if (targetShip.Captain != null)
		{
			UnassignCaptainOfShip(targetShip);
		}
		if (captainAgent != null)
		{
			if (captainsCurrentShip == null)
			{
				IsAgentOnAnyShip(captainAgent, out captainsCurrentShip);
			}
			if (captainsCurrentShip != targetShip)
			{
				TransferAgentToShip(captainAgent, targetShip, swapOnTransfer);
			}
			SetManagedCaptainOfFormation(captainAgent, formation);
		}
	}

	internal void UnassignCaptainOfShip(MissionShip targetShip)
	{
		SetManagedCaptainOfFormation(null, targetShip.Formation);
	}

	internal IAgentOriginBase FindTroopOrigin(Predicate<IAgentOriginBase> predicate)
	{
		foreach (IAgentOriginBase allTroopOrigin in _allTroopOrigins)
		{
			if (predicate(allTroopOrigin))
			{
				return allTroopOrigin;
			}
		}
		return null;
	}

	internal int FindTroopOrigins(Predicate<IAgentOriginBase> predicate, ref MBList<IAgentOriginBase> foundOrigins)
	{
		if (foundOrigins == null)
		{
			foundOrigins = new MBList<IAgentOriginBase>();
		}
		foundOrigins.Clear();
		foreach (IAgentOriginBase allTroopOrigin in _allTroopOrigins)
		{
			if (predicate(allTroopOrigin))
			{
				foundOrigins.Add(allTroopOrigin);
			}
		}
		return foundOrigins.Count;
	}

	internal bool IsTroopUnassigned(IAgentOriginBase troopOrigin)
	{
		return _unassignedTroops.ContainsKey(troopOrigin);
	}

	internal bool IsTroopInShipReserves(IAgentOriginBase origin, out MissionShip ship)
	{
		ship = null;
		foreach (NavalShipAgents allShipAgent in _allShipAgents)
		{
			if (allShipAgent.IsOriginInReserves(origin))
			{
				ship = allShipAgent.Ship;
				return true;
			}
		}
		return false;
	}

	internal bool IsAgentOnAnyShip(IAgentOriginBase origin, out Agent agent, out MissionShip ship)
	{
		agent = null;
		ship = null;
		foreach (NavalShipAgents allShipAgent in _allShipAgents)
		{
			MBReadOnlyList<Agent> source = (origin.Troop.IsHero ? allShipAgent.ActiveHeroAgents : allShipAgent.ActiveNonHeroAgents);
			agent = source.FirstOrDefault((Agent agnt) => agnt.Origin == origin);
			if (agent != null)
			{
				ship = allShipAgent.Ship;
				break;
			}
		}
		return agent != null;
	}

	internal bool IsAgentOnAnyShip(Agent agent, out MissionShip ship)
	{
		if (_agentToShipAgents.TryGetValue(agent, out var value))
		{
			ship = value.Ship;
			return true;
		}
		ship = null;
		return false;
	}

	internal bool IsAgentOnShip(Agent agent, MissionShip ship)
	{
		if (_agentToShipAgents.TryGetValue(agent, out var value))
		{
			return value.Ship == ship;
		}
		return false;
	}

	internal MBReadOnlyList<Agent> GetActiveAgents()
	{
		MBList<Agent> mBList = new MBList<Agent>();
		foreach (NavalShipAgents allShipAgent in _allShipAgents)
		{
			mBList.AddRange(allShipAgent.ActiveAgents);
		}
		return mBList;
	}

	internal int GetActiveTroopsCountOfShip(MissionShip ship)
	{
		return GetActiveAgentsOfShip(ship).Count;
	}

	internal MBReadOnlyList<Agent> GetActiveAgentsOfShip(MissionShip ship)
	{
		TryGetShipAgents(ship, out var shipAgents);
		return shipAgents?.ActiveAgents;
	}

	internal int GetTotalTroopsCountOfShip(MissionShip ship, bool spawnableReservesOnly)
	{
		TryGetShipAgents(ship, out var shipAgents);
		int num = shipAgents.ActiveAgents.Count;
		if (!spawnableReservesOnly || shipAgents.SpawnReinforcements)
		{
			num += shipAgents.ReservedTroopsCount;
		}
		return num;
	}

	internal int GetReservedTroopsCountOfShip(MissionShip ship)
	{
		TryGetShipAgents(ship, out var shipAgents);
		return shipAgents.ReservedTroopsCount;
	}

	internal void FillReservedTroopsOfShip(MissionShip ship, MBList<IAgentOriginBase> reservedTroops)
	{
		TryGetShipAgents(ship, out var shipAgents);
		shipAgents.FillReservedTroops(reservedTroops);
	}

	internal MBReadOnlyList<Agent> GetActiveHeroesOfShip(MissionShip ship)
	{
		TryGetShipAgents(ship, out var shipAgents);
		return shipAgents.ActiveHeroAgents;
	}

	internal void AutoComputeDesiredTroopCountsPerShip(bool loadBalanceShips, int troopLimitFromBattleSize)
	{
		if (loadBalanceShips)
		{
			int num = 0;
			foreach (NavalShipAgents allShipAgent in _allShipAgents)
			{
				num += allShipAgent.Ship.TotalCrewCapacity;
			}
			int num2 = Math.Min(troopLimitFromBattleSize, _allTroopOrigins.Count);
			float num3 = (float)num2 / (float)num;
			float num4 = (float)troopLimitFromBattleSize / (float)_allShipAgents.Count;
			int num5 = 0;
			foreach (NavalShipAgents allShipAgent2 in _allShipAgents)
			{
				float num6 = TaleWorlds.Library.MathF.Min((float)allShipAgent2.Ship.TotalCrewCapacity * num3, (float)allShipAgent2.Ship.TotalCrewCapacity);
				if (num6 < (float)allShipAgent2.Ship.ShipOrigin.SkeletalCrewCapacity)
				{
					num6 = allShipAgent2.Ship.ShipOrigin.SkeletalCrewCapacity;
				}
				if (num6 > num4)
				{
					num6 = num4;
				}
				int num7 = (int)num6;
				allShipAgent2.SetDesiredTroopCount(num7);
				num5 += num7;
			}
			int num8 = Math.Min(num2, num) - num5;
			bool flag = true;
			while (flag && num8 > 0)
			{
				flag = false;
				float num9 = float.MaxValue;
				int num10 = -1;
				for (int i = 0; i < _allShipAgents.Count; i++)
				{
					NavalShipAgents navalShipAgents = _allShipAgents[i];
					if (navalShipAgents.DesiredTroopCount < navalShipAgents.Ship.TotalCrewCapacity)
					{
						float num11 = (float)navalShipAgents.DesiredTroopCount / (float)navalShipAgents.Ship.ShipOrigin.SkeletalCrewCapacity;
						if (num9 > num11)
						{
							num9 = num11;
							num10 = i;
						}
					}
				}
				if (num10 != -1)
				{
					NavalShipAgents navalShipAgents2 = _allShipAgents[num10];
					navalShipAgents2.SetDesiredTroopCount(navalShipAgents2.DesiredTroopCount + 1);
					num5++;
					num8--;
					flag = true;
				}
			}
			return;
		}
		foreach (NavalShipAgents allShipAgent3 in _allShipAgents)
		{
			allShipAgent3.SetDesiredTroopCount(allShipAgent3.Ship.TotalCrewCapacity);
		}
	}

	internal void SetDesiredTroopCountOfShip(MissionShip ship, int desiredTroopCount)
	{
		TryGetShipAgents(ship, out var shipAgents);
		shipAgents.SetDesiredTroopCount(desiredTroopCount);
	}

	internal int GetDesiredTroopCountOfShip(MissionShip ship)
	{
		TryGetShipAgents(ship, out var shipAgents);
		return shipAgents.DesiredTroopCount;
	}

	internal void SetIgnoreTroopCapacities(bool value)
	{
		foreach (NavalShipAgents allShipAgent in _allShipAgents)
		{
			allShipAgent.SetIgnoreCapacityChecks(value);
		}
	}

	internal void SetIgnoreTroopCapacities(MissionShip ship, bool value)
	{
		TryGetShipAgents(ship, out var shipAgents);
		shipAgents.SetIgnoreCapacityChecks(value);
	}

	internal int SpawnNextBatch(bool isReinforcement, MBList<Agent> spawnedAgents = null)
	{
		int num = 0;
		foreach (NavalShipAgents allShipAgent in _allShipAgents)
		{
			(int spawnedCount, int reassignedCount) tuple = allShipAgent.SpawnNextBatch(isReinforcement, _tempSpawnedAgentsList);
			int item = tuple.spawnedCount;
			int item2 = tuple.reassignedCount;
			num += item + item2;
			NumberOfSpawnedAgents += item;
			foreach (Agent tempSpawnedAgents in _tempSpawnedAgentsList)
			{
				_agentToShipAgents[tempSpawnedAgents] = allShipAgent;
			}
			spawnedAgents?.AddRange(_tempSpawnedAgentsList);
			_tempSpawnedAgentsList.Clear();
		}
		return num;
	}

	internal void SetSpawnReinforcementsOnTick(bool value, bool resetShips)
	{
		SpawnReinforcementsOnTick = value;
		if (!resetShips)
		{
			return;
		}
		foreach (NavalShipAgents allShipAgent in _allShipAgents)
		{
			allShipAgent.SetSpawnReinforcements(value);
		}
	}

	internal void SetSpawnReinforcementsForShip(MissionShip ship, bool value)
	{
		TryGetShipAgents(ship, out var shipAgents);
		shipAgents.SetSpawnReinforcements(value);
	}

	internal bool GetSpawnReinforcementsForShip(MissionShip ship)
	{
		TryGetShipAgents(ship, out var shipAgents);
		return shipAgents.SpawnReinforcements;
	}

	internal int CheckSpawnReinforcements(MBList<Agent> spawnedAgents = null)
	{
		int num = 0;
		foreach (NavalShipAgents allShipAgent in _allShipAgents)
		{
			if (!allShipAgent.SpawnReinforcements)
			{
				continue;
			}
			int num2 = allShipAgent.CheckSpawnReinforcements(_tempSpawnedAgentsList);
			num += num2;
			NumberOfSpawnedAgents += num2;
			foreach (Agent tempSpawnedAgents in _tempSpawnedAgentsList)
			{
				_agentToShipAgents[tempSpawnedAgents] = allShipAgent;
			}
			spawnedAgents?.AddRange(_tempSpawnedAgentsList);
			_tempSpawnedAgentsList.Clear();
		}
		return num;
	}

	internal void InitializeReinforcementTimers(bool randomizeTimers, bool autoComputeDurations)
	{
		if (autoComputeDurations)
		{
			foreach (NavalShipAgents allShipAgent in _allShipAgents)
			{
				allShipAgent.SetReinforcementSpawnDuration();
			}
		}
		foreach (NavalShipAgents allShipAgent2 in _allShipAgents)
		{
			allShipAgent2.InitializeReinforcementTimer(randomizeTimers);
		}
	}

	internal void SetReinforcementSpawnDurationOfShip(MissionShip ship, float duration)
	{
		TryGetShipAgents(ship, out var shipAgents);
		shipAgents.SetReinforcementSpawnDuration(duration);
	}

	internal void AutoComputeReinforcementSpawnDurations()
	{
		foreach (NavalShipAgents allShipAgent in _allShipAgents)
		{
			allShipAgent.SetReinforcementSpawnDuration();
		}
	}

	internal void ClearRecentlySwappedAgents()
	{
		_recentlySwappedAgents.Clear();
	}

	internal void OnAgentRemoved(Agent agent)
	{
		if (_agentToShipAgents.TryGetValue(agent, out var value))
		{
			RemoveAgentAux(agent, value);
			RemoveTroopOriginAux(agent.Origin);
		}
	}

	internal void OnShipSpawned(MissionShip ship, bool ignoreTroopCapacities)
	{
		TryGetShipAgents(ship, out var shipAgents);
		shipAgents = new NavalShipAgents(ship, this);
		shipAgents.SetIgnoreCapacityChecks(ignoreTroopCapacities);
		_allShipAgents.Add(shipAgents);
	}

	internal void OnShipRemoved(MissionShip ship)
	{
		if (!TryGetShipAgents(ship, out var shipAgents))
		{
			return;
		}
		if (AgentsLogic.IsDeploymentMode && !AgentsLogic.IsMissionEnding)
		{
			while (shipAgents.ActiveAgents.Count > 0)
			{
				Agent agent = shipAgents.ActiveAgents.Last();
				UnassignAgentAux(shipAgents, agent);
			}
			while (shipAgents.ReservedTroopsCount > 0)
			{
				EnqueueUnassignedTroop(DequeueReservedTroop(shipAgents));
			}
		}
		else
		{
			while (shipAgents.ActiveAgents.Count > 0)
			{
				Agent agent2 = shipAgents.ActiveAgents.Last();
				RemoveAgentAux(agent2, shipAgents);
				RemoveTroopOriginAux(agent2.Origin);
				if (agent2 != Agent.Main)
				{
					agent2.FadeOut(hideInstantly: true, hideMount: true);
				}
			}
			while (shipAgents.ReservedTroopsCount > 0)
			{
				RemoveTroopOriginAux(DequeueReservedTroop(shipAgents).Origin);
			}
		}
		_allShipAgents.RemoveAll((NavalShipAgents sAgentsData) => sAgentsData.Ship == ship);
	}

	internal void OnShipCaptured(MissionShip ship, MissionShip ship2)
	{
		TryGetShipAgents(ship, out var shipAgents);
		shipAgents.OnShipCaptured(ship2);
	}

	internal void OnShipTransferredToFormation(MissionShip ship, Formation oldFormation)
	{
		TryGetShipAgents(ship, out var shipAgents);
		foreach (Agent activeAgent in shipAgents.ActiveAgents)
		{
			bool num = activeAgent == oldFormation.Captain;
			SetManagedAgentFormation(activeAgent, ship.Formation);
			if (num)
			{
				SetManagedCaptainOfFormation(activeAgent, ship.Formation);
			}
		}
	}

	internal void OnEndDeploymentMode()
	{
		int num = 0;
		while (NumberOfUnassignedTroops > 0)
		{
			DequeueUnassignedTroop(out var dequeuedTroop);
			IAgentOriginBase origin = dequeuedTroop.Origin;
			if (dequeuedTroop.HasAgent)
			{
				dequeuedTroop.Agent.FadeOut(hideInstantly: true, hideMount: true);
				num++;
			}
			RemoveTroopOriginAux(origin);
		}
		foreach (KeyValuePair<Agent, MissionShip> unassignedReservedAgent in _unassignedReservedAgents)
		{
			unassignedReservedAgent.Key.FadeOut(hideInstantly: true, hideMount: true);
			num++;
		}
		_unassignedReservedAgents.Clear();
		NumberOfSpawnedAgents -= num;
		foreach (NavalShipAgents allShipAgent in _allShipAgents)
		{
			allShipAgent.OnEndDeploymentMode();
		}
	}

	internal void SetManagedAgentFormation(Agent agent, Formation formation)
	{
		Formation formation2 = agent.Formation;
		if (formation2 != formation)
		{
			if (formation2 != null && formation2.Captain == agent)
			{
				SetManagedCaptainOfFormation(null, formation2);
			}
			agent.Formation = formation;
		}
	}

	internal void SetManagedCaptainOfFormation(Agent captain, Formation formation)
	{
		if (formation.Captain != captain)
		{
			formation.Captain = captain;
		}
	}

	internal void AddTroopOrigin(IAgentOriginBase origin)
	{
		AddTroopOriginAux(origin);
		EnqueueUnassignedTroop(NavalTroopAssignment.Create(origin));
	}

	internal bool SpawnExistingHero(IAgentOriginBase heroOrigin, MissionShip ship, out Agent spawnedHero)
	{
		spawnedHero = null;
		if (IsAgentOnAnyShip(heroOrigin, out var _, out var _))
		{
			return false;
		}
		TryGetShipAgents(ship, out var shipAgents);
		if (AgentsLogic.IsDeploymentMode)
		{
			MakeSpaceForOneAgent(shipAgents);
		}
		NavalTroopAssignment value;
		bool flag = _unassignedTroops.TryGetValue(heroOrigin, out value);
		if (flag && value.HasAgent)
		{
			spawnedHero = ReassignAgentAux(shipAgents, value.Agent);
		}
		else
		{
			bool flag2 = false;
			NavalTroopAssignment dequeuedTroop = NavalTroopAssignment.Invalid();
			if (flag)
			{
				DequeueUnassignedTroop(value.Origin, out dequeuedTroop);
				flag2 = true;
			}
			else
			{
				NavalShipAgents navalShipAgents = null;
				foreach (NavalShipAgents allShipAgent in _allShipAgents)
				{
					if (allShipAgent.IsOriginInReserves(heroOrigin))
					{
						navalShipAgents = allShipAgent;
						break;
					}
				}
				DequeueReservedTroop(heroOrigin, navalShipAgents, out dequeuedTroop);
				if (navalShipAgents != shipAgents)
				{
					NavalTroopAssignment dequeuedTroop2;
					if (shipAgents.ReservedTroopsCount > 0)
					{
						TransferReservedTroop(shipAgents, navalShipAgents);
					}
					else if (AgentsLogic.IsDeploymentMode && DequeueUnassignedTroop(out dequeuedTroop2))
					{
						EnqueueReservedTroop(in dequeuedTroop2, navalShipAgents);
					}
				}
				flag2 = true;
			}
			if (flag2)
			{
				EnqueueReservedTroop(in dequeuedTroop, shipAgents);
				spawnedHero = shipAgents.SpawnHeroFromReserve(heroOrigin, out var isReassigned);
				_agentToShipAgents[spawnedHero] = shipAgents;
				if (!isReassigned)
				{
					NumberOfSpawnedAgents++;
				}
			}
		}
		return spawnedHero != null;
	}

	internal void AssignAndTeleportCrewToShipMachines(MissionShip targetShip)
	{
		TryGetShipAgents(targetShip, out var shipAgents);
		shipAgents.AssignAndTeleportCrewToShipMachines();
	}

	internal void AssignAndTeleportCrewToShipMachines()
	{
		foreach (NavalShipAgents allShipAgent in _allShipAgents)
		{
			allShipAgent.AssignAndTeleportCrewToShipMachines();
		}
	}

	internal void UnassignTroops()
	{
		UnassignIncompatibleTroops();
		UnassignExcessTroopsFromShips();
	}

	internal void SetTroopTraitsFilter(MissionShip ship, TroopTraitsMask troopTraitsFilter)
	{
		TryGetShipAgents(ship, out var shipAgents);
		shipAgents.SetTroopTraitsFilter(troopTraitsFilter);
	}

	private void UnassignIncompatibleTroops()
	{
		foreach (NavalShipAgents allShipAgent in _allShipAgents)
		{
			foreach (Agent activeAgent in allShipAgent.ActiveAgents)
			{
				if (!allShipAgent.IsAgentCompatibleWithShip(activeAgent))
				{
					_tempIncompatibleAgentsList.Add(activeAgent);
					activeAgent.Formation.OnBatchUnitRemovalStart();
				}
			}
			foreach (Agent tempIncompatibleAgents in _tempIncompatibleAgentsList)
			{
				UnassignAgentAux(allShipAgent, tempIncompatibleAgents);
			}
			foreach (Team team in Mission.Current.Teams)
			{
				foreach (Formation item in team.FormationsIncludingSpecialAndEmpty)
				{
					item.OnBatchUnitRemovalEnd();
				}
			}
			_tempIncompatibleAgentsList.Clear();
			foreach (NavalTroopAssignment reservedTroop in allShipAgent.ReservedTroops)
			{
				IAgentOriginBase origin = reservedTroop.Origin;
				if (!allShipAgent.IsTroopCompatibleWithShip(origin))
				{
					_tempIncompatibleReservesList.Add(origin);
				}
			}
			foreach (IAgentOriginBase tempIncompatibleReserves in _tempIncompatibleReservesList)
			{
				DequeueReservedTroop(tempIncompatibleReserves, allShipAgent, out var dequeuedTroop);
				EnqueueUnassignedTroop(in dequeuedTroop);
			}
			_tempIncompatibleReservesList.Clear();
		}
	}

	private void UnassignExcessTroopsFromShips()
	{
		int num = 0;
		foreach (NavalShipAgents allShipAgent in _allShipAgents)
		{
			num += allShipAgent.MissingTroopCount;
		}
		int num2 = 0;
		bool flag = true;
		while (num2 < num && flag)
		{
			flag = false;
			float num3 = 0f;
			NavalShipAgents navalShipAgents = null;
			foreach (NavalShipAgents allShipAgent2 in _allShipAgents)
			{
				if (allShipAgent2.TroopFillRatio >= num3)
				{
					num3 = allShipAgent2.TroopFillRatio;
					navalShipAgents = allShipAgent2;
				}
			}
			if (navalShipAgents == null)
			{
				continue;
			}
			if (navalShipAgents.ActiveAgents.Count > 0)
			{
				Agent agent = null;
				if (!navalShipAgents.ActiveNonHeroAgents.IsEmpty())
				{
					agent = navalShipAgents.ActiveNonHeroAgents.MinBy((Agent a2) => NavalAgentsLogic.GetAgentPriority(a2));
				}
				if (agent == null)
				{
					agent = navalShipAgents.ActiveHeroAgents.MinBy((Agent a) => NavalAgentsLogic.GetAgentPriority(a));
				}
				if (!agent.IsMainAgent && !agent.IsPlayerTroop && agent != navalShipAgents.Ship.Formation.Captain)
				{
					UnassignAgentAux(navalShipAgents, agent);
					num2++;
					flag = true;
				}
			}
			if (!flag && navalShipAgents.ReservedTroopsCount > 0)
			{
				EnqueueUnassignedTroop(DequeueReservedTroop(navalShipAgents));
				num2++;
				flag = true;
			}
		}
	}

	internal void SetTroopClassFilter(MissionShip ship, TroopTraitsMask troopClassFilter)
	{
		TryGetShipAgents(ship, out var shipAgents);
		shipAgents.SetTroopClassFilter(troopClassFilter);
	}

	private void AddTroopOriginAux(IAgentOriginBase troopOrigin)
	{
		_allTroopOrigins.Add(troopOrigin);
		if (troopOrigin.Troop.IsHero)
		{
			_allHeroOrigins.Add(troopOrigin);
		}
	}

	public void RemoveTroopOriginAux(IAgentOriginBase troopOrigin)
	{
		_allTroopOrigins.Remove(troopOrigin);
		if (troopOrigin.Troop.IsHero)
		{
			_allHeroOrigins.Remove(troopOrigin);
		}
	}

	private bool AddReservedTroopToShipAux(IAgentOriginBase agentOrigin, NavalShipAgents shipAgentsData)
	{
		if (shipAgentsData.IsOriginInReserves(agentOrigin))
		{
			return true;
		}
		if (AgentsLogic.IsDeploymentMode || shipAgentsData.CanAddMoreReserves)
		{
			NavalTroopAssignment dequeuedTroop = NavalTroopAssignment.Invalid();
			if (shipAgentsData.CanAddMoreReserves && !_allTroopOrigins.Contains(agentOrigin))
			{
				AddTroopOriginAux(agentOrigin);
				dequeuedTroop = NavalTroopAssignment.Create(agentOrigin);
			}
			else if (AgentsLogic.IsDeploymentMode)
			{
				DequeueUnassignedTroop(agentOrigin, out dequeuedTroop);
			}
			if (dequeuedTroop.IsValid)
			{
				EnqueueReservedTroop(in dequeuedTroop, shipAgentsData);
				return true;
			}
		}
		return false;
	}

	private bool RemoveReservedTroopFromShipAux(NavalShipAgents shipAgentsData)
	{
		if (shipAgentsData.ReservedTroopsCount > 0)
		{
			NavalTroopAssignment troop = DequeueReservedTroop(shipAgentsData);
			if (AgentsLogic.IsDeploymentMode)
			{
				EnqueueUnassignedTroop(in troop);
			}
			else
			{
				RemoveTroopOriginAux(troop.Origin);
			}
			return true;
		}
		return false;
	}

	private void UpdateTemporaryShipsWithMissingTroopsAux(int shipIndex, NavalShipAgents shipAgentsData)
	{
		if (shipAgentsData.HasMissingTroops)
		{
			int num = shipIndex;
			while (num > 0 && _tempShipsWithMissingTroops[num - 1].TroopFillRatio < shipAgentsData.TroopFillRatio)
			{
				_tempShipsWithMissingTroops[num] = _tempShipsWithMissingTroops[num - 1];
				num--;
			}
			if (num != shipIndex)
			{
				_tempShipsWithMissingTroops[num] = shipAgentsData;
			}
		}
		else
		{
			_tempShipsWithMissingTroops.RemoveAt(shipIndex);
		}
	}

	private bool TryGetShipAgents(MissionShip ship, out NavalShipAgents shipAgents)
	{
		shipAgents = null;
		foreach (NavalShipAgents allShipAgent in _allShipAgents)
		{
			if (allShipAgent.Ship == ship)
			{
				shipAgents = allShipAgent;
				return true;
			}
		}
		return false;
	}

	private void EnqueueUnassignedTroop(in NavalTroopAssignment troop)
	{
		_unassignedTroops.Add(troop.Origin, troop);
		_unassignedOrderedTroops.Add(troop.Priority, troop);
		_unassignedTroopCountData.Add(in troop);
	}

	private bool DequeueUnassignedTroop(IAgentOriginBase troopOrigin, out NavalTroopAssignment dequeuedTroop)
	{
		dequeuedTroop = NavalTroopAssignment.Invalid();
		if (_unassignedOrderedTroops.Count > 0)
		{
			int num = _unassignedOrderedTroops.FindIndex((KeyValuePair<int, NavalTroopAssignment> tuple) => tuple.Value.Origin == troopOrigin, !troopOrigin.Troop.IsHero);
			if (num >= 0)
			{
				dequeuedTroop = _unassignedOrderedTroops[num];
				_unassignedOrderedTroops.RemoveAt(num);
				_unassignedTroops.Remove(dequeuedTroop.Origin);
				_unassignedTroopCountData.Remove(in dequeuedTroop);
				return true;
			}
		}
		return false;
	}

	private bool DequeueUnassignedTroop(out NavalTroopAssignment dequeuedTroop)
	{
		dequeuedTroop = NavalTroopAssignment.Invalid();
		if (_unassignedOrderedTroops.Count > 0)
		{
			dequeuedTroop = _unassignedOrderedTroops.LastValue;
			_unassignedOrderedTroops.RemoveLast();
			_unassignedTroops.Remove(dequeuedTroop.Origin);
			_unassignedTroopCountData.Remove(in dequeuedTroop);
			return true;
		}
		return false;
	}

	internal void AssignTroops(bool useDynamicTroopTraits = false)
	{
		_tempShipsWithMissingTroops.Clear();
		foreach (NavalShipAgents allShipAgent in _allShipAgents)
		{
			if (allShipAgent.HasMissingTroops)
			{
				int i;
				for (i = 0; i < _tempShipsWithMissingTroops.Count && allShipAgent.TroopFillRatio < _tempShipsWithMissingTroops[i].TroopFillRatio; i++)
				{
				}
				_tempShipsWithMissingTroops.Insert(i, allShipAgent);
			}
		}
		while (NumberOfUnassignedTroops > 0)
		{
			int num = -1;
			int num2 = -1;
			DequeueUnassignedTroop(out var dequeuedTroop);
			TroopTraitsMask troopTraitsMask = TroopTraitsMask.None;
			troopTraitsMask = ((!useDynamicTroopTraits || dequeuedTroop.Agent == null) ? dequeuedTroop.Origin.GetTraitsMask() : dequeuedTroop.Agent.GetTraitsMask());
			for (int num3 = _tempShipsWithMissingTroops.Count - 1; num3 >= 0; num3--)
			{
				NavalShipAgents navalShipAgents = _tempShipsWithMissingTroops[num3];
				if (navalShipAgents.IsTroopCompatibleWithClassFilter(troopTraitsMask))
				{
					int traitsFilterPriority = navalShipAgents.GetTraitsFilterPriority(dequeuedTroop);
					if (traitsFilterPriority > num)
					{
						num = traitsFilterPriority;
						num2 = num3;
					}
				}
			}
			if (num2 >= 0)
			{
				NavalShipAgents shipAgentsData = _tempShipsWithMissingTroops[num2];
				EnqueueReservedTroop(in dequeuedTroop, shipAgentsData);
				UpdateTemporaryShipsWithMissingTroopsAux(num2, shipAgentsData);
			}
			else
			{
				_tempUnassignedTroops.Add(dequeuedTroop);
			}
		}
		for (int j = 0; j < _tempUnassignedTroops.Count; j++)
		{
			if (_tempShipsWithMissingTroops.Count <= 0)
			{
				break;
			}
			NavalTroopAssignment troop = _tempUnassignedTroops[j];
			int num4 = _tempShipsWithMissingTroops.Count - 1;
			NavalShipAgents shipAgentsData2 = _tempShipsWithMissingTroops[num4];
			EnqueueReservedTroop(in troop, shipAgentsData2);
			UpdateTemporaryShipsWithMissingTroopsAux(num4, shipAgentsData2);
			_tempUnassignedTroops[j] = NavalTroopAssignment.Invalid();
		}
		if (_tempUnassignedTroops.Count <= 0)
		{
			return;
		}
		foreach (NavalTroopAssignment tempUnassignedTroop in _tempUnassignedTroops)
		{
			NavalTroopAssignment troop2 = tempUnassignedTroop;
			if (troop2.IsValid)
			{
				EnqueueUnassignedTroop(in troop2);
			}
		}
		_tempUnassignedTroops.Clear();
	}

	private bool DequeueUnassignedAgent(out NavalTroopAssignment dequeuedTroop)
	{
		dequeuedTroop = NavalTroopAssignment.Invalid();
		if (_unassignedOrderedTroops.Count > 0)
		{
			int num = _unassignedOrderedTroops.FindIndex((KeyValuePair<int, NavalTroopAssignment> tuple) => tuple.Value.HasAgent, searchForward: false);
			if (num >= 0)
			{
				dequeuedTroop = _unassignedOrderedTroops[num];
				_unassignedOrderedTroops.RemoveAt(num);
				_unassignedTroops.Remove(dequeuedTroop.Origin);
				_unassignedTroopCountData.Remove(in dequeuedTroop);
				return true;
			}
		}
		return false;
	}

	private void EnqueueReservedTroop(in NavalTroopAssignment troop, NavalShipAgents shipAgentsData)
	{
		shipAgentsData.EnqueueReservedTroop(in troop);
		if (troop.HasAgent)
		{
			_unassignedReservedAgents.Add(troop.Agent, shipAgentsData.Ship);
		}
	}

	private bool RemoveReservedTroopFromShipAux(IAgentOriginBase troopOrigin, NavalShipAgents shipAgentsData)
	{
		if (shipAgentsData.ReservedTroopsCount > 0 && DequeueReservedTroop(troopOrigin, shipAgentsData, out var dequeuedTroop))
		{
			if (AgentsLogic.IsDeploymentMode)
			{
				EnqueueUnassignedTroop(in dequeuedTroop);
			}
			else
			{
				RemoveTroopOriginAux(dequeuedTroop.Origin);
			}
			return true;
		}
		return false;
	}

	private NavalTroopAssignment DequeueReservedTroop(NavalShipAgents shipAgentsData)
	{
		shipAgentsData.DequeueReservedTroop(out var dequeuedTroop);
		if (dequeuedTroop.HasAgent)
		{
			_unassignedReservedAgents.Remove(dequeuedTroop.Agent);
		}
		return dequeuedTroop;
	}

	private bool DequeueReservedTroop(IAgentOriginBase troopOrigin, NavalShipAgents shipAgentsData, out NavalTroopAssignment dequeuedTroop)
	{
		dequeuedTroop = NavalTroopAssignment.Invalid();
		if (shipAgentsData.DequeueReservedTroop(troopOrigin, out dequeuedTroop))
		{
			if (dequeuedTroop.HasAgent)
			{
				_unassignedReservedAgents.Remove(dequeuedTroop.Agent);
			}
			return true;
		}
		return false;
	}

	private void TransferReservedTroop(NavalShipAgents fromShipAgentsData, NavalShipAgents toShipAgentsData, IAgentOriginBase troopOrigin = null)
	{
		NavalTroopAssignment dequeuedTroop = NavalTroopAssignment.Invalid();
		if (troopOrigin != null)
		{
			fromShipAgentsData.DequeueReservedTroop(troopOrigin, out dequeuedTroop);
		}
		else
		{
			fromShipAgentsData.DequeueReservedTroop(out dequeuedTroop);
		}
		toShipAgentsData.EnqueueReservedTroop(in dequeuedTroop);
		if (dequeuedTroop.HasAgent)
		{
			_unassignedReservedAgents[dequeuedTroop.Agent] = toShipAgentsData.Ship;
		}
	}

	private void UnassignAgentAux(NavalShipAgents shipAgentsData, Agent agent)
	{
		RemoveAgentAux(agent, shipAgentsData);
		_ = shipAgentsData.Ship;
		agent.SetDetachableFromFormation(value: false);
		agent.SetRenderCheckEnabled(value: false);
		agent.AgentVisuals.SetVisible(value: false);
		agent.SetIsPhysicsForceClosed(isPhysicsForceClosed: true);
		AgentNavalComponent component = agent.GetComponent<AgentNavalComponent>();
		agent.RemoveComponent(component);
		AgentNavalAIComponent component2 = agent.GetComponent<AgentNavalAIComponent>();
		agent.RemoveComponent(component2);
		Mission.Current.GetDeploymentPlan<NavalMissionDeploymentPlanningLogic>(out var deploymentPlan);
		deploymentPlan.GetMeanBoundaryPosition(agent.Team, out var meanPosition);
		agent.TeleportToPosition(meanPosition.ToVec3(500f));
		EnqueueUnassignedTroop(NavalTroopAssignment.Create(agent.Origin, agent));
	}

	internal Agent ReassignAgentAux(NavalShipAgents shipAgentsData, Agent agent = null)
	{
		NavalTroopAssignment dequeuedTroop = NavalTroopAssignment.Invalid();
		if (agent == null)
		{
			DequeueUnassignedAgent(out dequeuedTroop);
		}
		else if (_unassignedReservedAgents.ContainsKey(agent))
		{
			_unassignedReservedAgents.Remove(agent);
			dequeuedTroop = NavalTroopAssignment.Create(agent.Origin, agent);
		}
		else
		{
			DequeueUnassignedTroop(agent.Origin, out dequeuedTroop);
		}
		agent = dequeuedTroop.Agent;
		AgentNavalComponent component = agent.GetComponent<AgentNavalComponent>();
		AgentNavalAIComponent component2 = agent.GetComponent<AgentNavalAIComponent>();
		component = new AgentNavalComponent(agent);
		agent.AddComponent(component);
		component2 = new AgentNavalAIComponent(agent);
		agent.AddComponent(component2);
		component.Initialize();
		agent.AgentVisuals.SetVisible(value: true);
		agent.SetRenderCheckEnabled(value: true);
		agent.SetIsPhysicsForceClosed(isPhysicsForceClosed: false);
		if (!agent.IsPlayerTroop)
		{
			agent.SetDetachableFromFormation(value: true);
		}
		AddAgentAux(agent, shipAgentsData);
		return agent;
	}

	internal void SetRestrictRecentlySwappedAgentTransfers(bool value)
	{
		if (RestrictRecentlySwappedAgentTransfers && !value)
		{
			ClearRecentlySwappedAgents();
		}
		RestrictRecentlySwappedAgentTransfers = value;
	}

	private void AddAgentAux(Agent agent, NavalShipAgents shipAgentsData)
	{
		shipAgentsData.AddAgent(agent);
		_agentToShipAgents[agent] = shipAgentsData;
	}

	private void RemoveAgentAux(Agent agent, NavalShipAgents targetShipAgentsData)
	{
		targetShipAgentsData.RemoveAgent(agent);
		_agentToShipAgents.Remove(agent);
		if (_recentlySwappedAgents.Count > 0)
		{
			_recentlySwappedAgents.Remove(agent);
		}
	}

	private void TransferAgentAux(Agent agent, NavalShipAgents originShipAgentsData, NavalShipAgents targetShipAgentsData)
	{
		originShipAgentsData?.RemoveAgent(agent);
		targetShipAgentsData.AddAgent(agent);
		_agentToShipAgents[agent] = targetShipAgentsData;
	}

	private void MakeSpaceForOneAgent(NavalShipAgents shipAgentsData, bool ignorePlayerTroop = true)
	{
		while (shipAgentsData.MissingAgentCountOnMainDeck == 0 && shipAgentsData.ActiveAgents.Count > 0)
		{
			Agent minimumPriorityActiveAgent = shipAgentsData.GetMinimumPriorityActiveAgent();
			if (ignorePlayerTroop && minimumPriorityActiveAgent.IsPlayerTroop)
			{
				break;
			}
			UnassignAgentAux(shipAgentsData, minimumPriorityActiveAgent);
		}
		MakeSpaceInReserves(shipAgentsData);
	}

	private void MakeSpaceInReserves(NavalShipAgents shipAgentsData)
	{
		while (shipAgentsData.MissingTroopCount == 0 && shipAgentsData.ReservedTroopsCount > 0)
		{
			RemoveReservedTroopFromShipAux(shipAgentsData);
		}
	}
}
