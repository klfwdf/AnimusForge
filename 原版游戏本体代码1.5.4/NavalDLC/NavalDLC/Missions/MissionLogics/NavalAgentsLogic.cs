using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.MissionLogics;

public class NavalAgentsLogic : MissionLogic
{
	public const float MinReinforcementsDuration = 0.5f;

	public const float MaxReinforcementsDuration = 3f;

	private readonly bool[] _ignoreTroopCapacities;

	private readonly MBList<NavalTeamAgents> _teamAgentsData;

	private bool _isDeploymentMode;

	public NavalShipsLogic NavalShipsLogic { get; private set; }

	public bool IsDeploymentMode => _isDeploymentMode;

	public bool IsDeploymentFinished => base.Mission.IsDeploymentFinished;

	public bool IsMissionEnding => NavalShipsLogic.IsMissionEnding;

	public event Action<IAgentOriginBase, MissionShip> TroopAddedToReserves;

	public event Action<IAgentOriginBase, MissionShip> TroopRemovedFromReserves;

	public event Action<Agent, MissionShip> AgentAddedToShip;

	public event Action<Agent, MissionShip> AgentRemovedFromShip;

	public NavalAgentsLogic()
	{
		_teamAgentsData = new MBList<NavalTeamAgents>();
		_ignoreTroopCapacities = new bool[3];
	}

	public override void OnBehaviorInitialize()
	{
		NavalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		base.Mission.GetAgentTroopClass_Override += GetNavalMissionTroopClass;
		NavalShipsLogic.ShipSpawnedEvent += OnShipSpawned;
		NavalShipsLogic.ShipRemovedEvent += OnShipRemoved;
		NavalShipsLogic.ShipTransferredToFormationEvent += OnShipTransferredToFormation;
		NavalShipsLogic.ShipTransferredToTeamEvent += OnShipTransferredToTeam;
		NavalShipsLogic.ShipCapturedEvent += OnShipCaptured;
		NavalShipsLogic.ShipTeleportedEvent += OnShipTeleported;
		NavalShipsLogic.ShipPreparedForAbandonmentEvent += OnShipPreparedForAbandonment;
		NavalShipsLogic.MissionEndEvent += OnMissionEnd;
	}

	public override void OnAgentCreated(Agent agent)
	{
		base.OnAgentCreated(agent);
		agent.AddComponent(new AgentNavalComponent(agent));
		if (agent.IsHuman)
		{
			agent.AddComponent(new AgentNavalAIComponent(agent));
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (affectedAgent.IsHuman && GetTeamAgents(affectedAgent.Team.TeamSide, out var teamAgents))
		{
			teamAgents.OnAgentRemoved(affectedAgent);
		}
	}

	public override void EarlyStart()
	{
		UpdateTeamAgentsData();
	}

	public void UpdateTeamAgentsData()
	{
		_teamAgentsData.Clear();
		foreach (Team team in base.Mission.Teams)
		{
			_teamAgentsData.Add(new NavalTeamAgents(this, team.Side, team.TeamSide));
		}
	}

	public override void OnMissionTick(float dt)
	{
		foreach (NavalTeamAgents teamAgentsDatum in _teamAgentsData)
		{
			if (teamAgentsDatum.SpawnReinforcementsOnTick)
			{
				teamAgentsDatum.CheckSpawnReinforcements();
			}
		}
	}

	public void SetSpawnReinforcementsOnTick(bool value, bool resetShips = true)
	{
		foreach (NavalTeamAgents teamAgentsDatum in _teamAgentsData)
		{
			teamAgentsDatum.SetSpawnReinforcementsOnTick(value, resetShips);
		}
	}

	private void SetSpawnReinforcementsForShip(MissionShip ship, bool value)
	{
		GetTeamAgents(ship.Team.TeamSide, out var teamAgents);
		teamAgents.SetSpawnReinforcementsForShip(ship, value);
	}

	public override void OnMissionStateFinalized()
	{
		base.Mission.GetAgentTroopClass_Override -= GetNavalMissionTroopClass;
		NavalShipsLogic.ShipSpawnedEvent -= OnShipSpawned;
		NavalShipsLogic.ShipRemovedEvent -= OnShipRemoved;
		NavalShipsLogic.ShipTransferredToFormationEvent -= OnShipTransferredToFormation;
		NavalShipsLogic.ShipTransferredToTeamEvent -= OnShipTransferredToTeam;
		NavalShipsLogic.ShipCapturedEvent -= OnShipCaptured;
		NavalShipsLogic.ShipTeleportedEvent -= OnShipTeleported;
		NavalShipsLogic.ShipPreparedForAbandonmentEvent -= OnShipPreparedForAbandonment;
		NavalShipsLogic.MissionEndEvent -= OnMissionEnd;
	}

	public void SetSpawnReinforcementsOnTick(TeamSideEnum teamSide, bool value, bool resetShips = true)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		teamAgents.SetSpawnReinforcementsOnTick(value, resetShips);
	}

	public bool GetSpawnReinforcementsOnTick(TeamSideEnum teamSide)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		return teamAgents.SpawnReinforcementsOnTick;
	}

	public void SetIgnoreTroopCapacities(bool value)
	{
		foreach (NavalTeamAgents teamAgentsDatum in _teamAgentsData)
		{
			_ignoreTroopCapacities[(int)teamAgentsDatum.TeamSide] = value;
			teamAgentsDatum.SetIgnoreTroopCapacities(value);
		}
	}

	public void SetIgnoreTroopCapacities(MissionShip ship, bool value)
	{
		GetTeamAgents(ship.Team.TeamSide, out var teamAgents);
		teamAgents.SetIgnoreTroopCapacities(ship, value);
	}

	public void SetRestrictRecentlySwappedAgentTransfers(TeamSideEnum teamSide, bool value)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		teamAgents.SetRestrictRecentlySwappedAgentTransfers(value);
	}

	public bool GetRestrictRecentlySwappedAgentTransfers(TeamSideEnum teamSide)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		return teamAgents.RestrictRecentlySwappedAgentTransfers;
	}

	public void ClearRecentlySwappedAgentsData(TeamSideEnum teamSide)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		teamAgents.ClearRecentlySwappedAgents();
	}

	public IAgentOriginBase FindTroopOrigin(TeamSideEnum teamSide, Predicate<IAgentOriginBase> predicate)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		return teamAgents.FindTroopOrigin(predicate);
	}

	public int FindTroopOrigins(TeamSideEnum teamSide, Predicate<IAgentOriginBase> predicate, ref MBList<IAgentOriginBase> foundOrigins)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		return teamAgents.FindTroopOrigins(predicate, ref foundOrigins);
	}

	public IReadOnlyCollection<IAgentOriginBase> GetTeamTroopOrigins(TeamSideEnum teamSide)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		return teamAgents.AllTroopOrigins;
	}

	public IReadOnlyCollection<IAgentOriginBase> GetTeamHeroOrigins(TeamSideEnum teamSide)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		return teamAgents.AllHeroOrigins;
	}

	public int GetNumberOfSpawnedAgents(BattleSideEnum side)
	{
		int num = 0;
		foreach (NavalTeamAgents teamAgentsDatum in _teamAgentsData)
		{
			if (teamAgentsDatum.BattleSide == side)
			{
				num += teamAgentsDatum.NumberOfSpawnedAgents;
			}
		}
		return num;
	}

	public int GetNumberOfActiveAgents(BattleSideEnum side)
	{
		int num = 0;
		foreach (NavalTeamAgents teamAgentsDatum in _teamAgentsData)
		{
			if (teamAgentsDatum.BattleSide == side)
			{
				num += teamAgentsDatum.NumberOfActiveTroops;
			}
		}
		return num;
	}

	internal int GetNumberOfReservedTroops(BattleSideEnum side, bool spawnableOnly = false)
	{
		int num = 0;
		foreach (NavalTeamAgents teamAgentsDatum in _teamAgentsData)
		{
			if (teamAgentsDatum.BattleSide == side)
			{
				num += teamAgentsDatum.GetNumberOfReservedTroops(spawnableOnly);
			}
		}
		return num;
	}

	public MBReadOnlyList<Agent> GetActiveAgentsOfShip(MissionShip ship)
	{
		if (ship.Team != null)
		{
			GetTeamAgents(ship.Team.TeamSide, out var teamAgents);
			return teamAgents.GetActiveAgentsOfShip(ship);
		}
		return null;
	}

	public int GetReservedTroopsCountOfShip(MissionShip ship)
	{
		if (ship.Team != null)
		{
			GetTeamAgents(ship.Team.TeamSide, out var teamAgents);
			return teamAgents.GetReservedTroopsCountOfShip(ship);
		}
		return 0;
	}

	internal int GetTotalTroopCountOfShip(MissionShip ship, bool spawnableReservesOnly = false)
	{
		if (ship.Team != null)
		{
			GetTeamAgents(ship.Team.TeamSide, out var teamAgents);
			return teamAgents.GetTotalTroopsCountOfShip(ship, spawnableReservesOnly);
		}
		return 0;
	}

	public FormationClass GetNavalMissionTroopClass(BattleSideEnum battleSide, BasicCharacterObject agentCharacter)
	{
		return agentCharacter.GetFormationClass().DismountedClass();
	}

	public void FillReservedTroopsOfShip(MissionShip ship, MBList<IAgentOriginBase> reservedTroops)
	{
		GetTeamAgents(ship.Team.TeamSide, out var teamAgents);
		teamAgents.FillReservedTroopsOfShip(ship, reservedTroops);
	}

	public MBReadOnlyList<Agent> GetActiveHeroesOfShip(MissionShip ship)
	{
		if (ship.Team != null)
		{
			GetTeamAgents(ship.Team.TeamSide, out var teamAgents);
			return teamAgents.GetActiveHeroesOfShip(ship);
		}
		return null;
	}

	public bool IsAgentOnAnyShip(Agent agent, out MissionShip onShip, TeamSideEnum teamSide = TeamSideEnum.None)
	{
		if (teamSide == TeamSideEnum.None)
		{
			foreach (NavalTeamAgents teamAgentsDatum in _teamAgentsData)
			{
				if (teamAgentsDatum.IsAgentOnAnyShip(agent, out onShip))
				{
					return true;
				}
			}
			onShip = null;
			return false;
		}
		GetTeamAgents(teamSide, out var teamAgents);
		return teamAgents.IsAgentOnAnyShip(agent, out onShip);
	}

	public int GetActiveHeroCountOfShip(MissionShip ship)
	{
		return GetActiveHeroesOfShip(ship).Count;
	}

	public bool IsTroopOriginInShipReserves(TeamSideEnum teamSide, IAgentOriginBase troopOrigin, out MissionShip onShip)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		return teamAgents.IsTroopInShipReserves(troopOrigin, out onShip);
	}

	public void AddAgentToShip(Agent agent, MissionShip targetShip)
	{
		TeamSideEnum teamSide = targetShip.Team.TeamSide;
		GetTeamAgents(teamSide, out var teamAgents);
		teamAgents.AddAgentToShip(agent, targetShip);
	}

	public void RemoveAgentFromShip(Agent agent, MissionShip ship)
	{
		TeamSideEnum teamSide = ship.Team.TeamSide;
		GetTeamAgents(teamSide, out var teamAgents);
		teamAgents.RemoveAgentFromShip(agent, ship);
	}

	public bool AddReservedTroopToShip(IAgentOriginBase troopOrigin, MissionShip ship)
	{
		TeamSideEnum teamSide = ship.Team.TeamSide;
		GetTeamAgents(teamSide, out var teamAgents);
		return teamAgents.AddReservedTroopToShip(troopOrigin, ship);
	}

	public bool IsAgentOnAnyShip(IAgentOriginBase agentOrigin, out Agent foundAgent, out MissionShip onShip, TeamSideEnum teamSide = TeamSideEnum.None)
	{
		if (teamSide == TeamSideEnum.None)
		{
			foreach (NavalTeamAgents teamAgentsDatum in _teamAgentsData)
			{
				if (teamAgentsDatum.IsAgentOnAnyShip(agentOrigin, out foundAgent, out onShip))
				{
					return true;
				}
			}
			foundAgent = null;
			onShip = null;
			return false;
		}
		GetTeamAgents(teamSide, out var teamAgents);
		return teamAgents.IsAgentOnAnyShip(agentOrigin, out foundAgent, out onShip);
	}

	public int GetActiveAgentCountOfShip(MissionShip ship)
	{
		if (ship.Team != null)
		{
			GetTeamAgents(ship.Team.TeamSide, out var teamAgents);
			return teamAgents.GetActiveTroopsCountOfShip(ship);
		}
		return 0;
	}

	public void RemoveAllReservedTroopsFromShip(MissionShip ship)
	{
		TeamSideEnum teamSide = ship.Team.TeamSide;
		GetTeamAgents(teamSide, out var teamAgents);
		teamAgents.RemoveAllReservedTroopsFromShip(ship);
	}

	public bool TransferAgentToShip(Agent agent, MissionShip ship)
	{
		TeamSideEnum teamSide = ship.Team.TeamSide;
		GetTeamAgents(teamSide, out var teamAgents);
		return teamAgents.TransferAgentToShip(agent, ship, swapAgents: false);
	}

	public int SpawnNextBatch(TeamSideEnum teamSide, bool isReinforcement = false, MBList<Agent> spawnedAgents = null)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		return teamAgents.SpawnNextBatch(isReinforcement, spawnedAgents);
	}

	public int CheckSpawnReinforcements(TeamSideEnum teamSide, MBList<Agent> spawnedAgents = null)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		return teamAgents.CheckSpawnReinforcements(spawnedAgents);
	}

	public void InitializeReinforcementTimers(TeamSideEnum teamSide, bool randomizeTimers = true, bool autoComputeDurations = true)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		teamAgents.InitializeReinforcementTimers(randomizeTimers, autoComputeDurations);
	}

	internal void AssignCaptainToShip(Agent agent, MissionShip ship, MissionShip captainsCurrentShip = null)
	{
		TeamSideEnum teamSide = ship.Team.TeamSide;
		GetTeamAgents(teamSide, out var teamAgents);
		teamAgents.AssignCaptainToShip(agent, ship, swapOnTransfer: false, captainsCurrentShip);
	}

	internal void AssignCaptainToShipForDeploymentMode(Agent agent, MissionShip targetShip, MissionShip captainsCurrentShip = null)
	{
		TeamSideEnum teamSide = targetShip.Team.TeamSide;
		MissionShip formationShip = agent.GetComponent<AgentNavalComponent>().FormationShip;
		GetTeamAgents(teamSide, out var teamAgents);
		teamAgents.AssignCaptainToShip(agent, targetShip, swapOnTransfer: true, captainsCurrentShip);
		teamAgents.AssignAndTeleportCrewToShipMachines(targetShip);
		if (formationShip != targetShip)
		{
			teamAgents.AssignAndTeleportCrewToShipMachines(formationShip);
		}
	}

	internal void UnassignCaptainOfShip(MissionShip targetShip)
	{
		TeamSideEnum teamSide = targetShip.Team.TeamSide;
		GetTeamAgents(teamSide, out var teamAgents);
		teamAgents.UnassignCaptainOfShip(targetShip);
	}

	internal void UnassignCaptainOfShipForDeploymentMode(MissionShip targetShip)
	{
		TeamSideEnum teamSide = targetShip.Team.TeamSide;
		GetTeamAgents(teamSide, out var teamAgents);
		teamAgents.UnassignCaptainOfShip(targetShip);
		teamAgents.AssignAndTeleportCrewToShipMachines(targetShip);
	}

	public void SetDeploymentMode(bool value)
	{
		if (_isDeploymentMode == value)
		{
			return;
		}
		if (!value)
		{
			foreach (NavalTeamAgents teamAgentsDatum in _teamAgentsData)
			{
				teamAgentsDatum.OnEndDeploymentMode();
			}
		}
		_isDeploymentMode = value;
	}

	public void AddTroopOrigin(TeamSideEnum teamSide, IAgentOriginBase troopOrigin)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		teamAgents.AddTroopOrigin(troopOrigin);
	}

	public void AddTroopOrigins(TeamSideEnum teamSide, MBList<IAgentOriginBase> troopOrigins)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		foreach (IAgentOriginBase troopOrigin in troopOrigins)
		{
			teamAgents.AddTroopOrigin(troopOrigin);
		}
	}

	public void UnassignTroops(TeamSideEnum teamSide)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		teamAgents.UnassignTroops();
	}

	public bool SpawnExistingHero(IAgentOriginBase heroOrigin, MissionShip ship, out Agent spawnedHero)
	{
		TeamSideEnum teamSide = ship.Team.TeamSide;
		GetTeamAgents(teamSide, out var teamAgents);
		return teamAgents.SpawnExistingHero(heroOrigin, ship, out spawnedHero);
	}

	public void AutoComputeDesiredTroopCountsPerShip(TeamSideEnum teamSide, bool loadBalanceShips = true)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		int troopLimitFromBattleSize = ComputeTeamTroopLimitAccordingToBattleSize(teamSide);
		teamAgents.AutoComputeDesiredTroopCountsPerShip(loadBalanceShips, troopLimitFromBattleSize);
	}

	public void AssignTroops(TeamSideEnum teamSide, bool useDynamicTroopTraits = false)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		teamAgents.AssignTroops(useDynamicTroopTraits);
	}

	internal void InvokeAgentRemovedFromShip(Agent agent, MissionShip ship)
	{
		ship.InvalidateActiveFormationTroopOnShipCache();
		this.AgentRemovedFromShip?.Invoke(agent, ship);
	}

	internal void InvokeAgentAddedToShip(Agent agent, MissionShip ship)
	{
		ship.InvalidateActiveFormationTroopOnShipCache();
		this.AgentAddedToShip?.Invoke(agent, ship);
	}

	internal void InvokeTroopRemovedFromReserves(IAgentOriginBase troop, MissionShip ship)
	{
		this.TroopRemovedFromReserves?.Invoke(troop, ship);
	}

	internal void InvokeTroopAddedToReserves(IAgentOriginBase troop, MissionShip ship)
	{
		this.TroopAddedToReserves?.Invoke(troop, ship);
	}

	public bool IsAgentUnassigned(Agent agent, TeamSideEnum teamSide = TeamSideEnum.None)
	{
		if (teamSide == TeamSideEnum.None)
		{
			foreach (NavalTeamAgents teamAgentsDatum in _teamAgentsData)
			{
				if (teamAgentsDatum.IsTroopUnassigned(agent.Origin))
				{
					return true;
				}
			}
			return false;
		}
		GetTeamAgents(teamSide, out var teamAgents);
		return teamAgents.IsTroopUnassigned(agent.Origin);
	}

	private int ComputeTeamTroopLimitAccordingToBattleSize(TeamSideEnum teamSide)
	{
		int realBattleSizeForNaval = BannerlordConfig.GetRealBattleSizeForNaval();
		int num = 0;
		int num2 = 0;
		foreach (NavalTeamAgents teamAgentsDatum in _teamAgentsData)
		{
			num += teamAgentsDatum.AllTroopOrigins.Count;
			if (teamAgentsDatum.TeamSide == teamSide)
			{
				num2 = teamAgentsDatum.AllTroopOrigins.Count;
			}
		}
		float num3 = (float)num2 * (float)realBattleSizeForNaval / (float)num;
		int num4 = (int)num3;
		float num5 = (float)(num - num2) * (float)realBattleSizeForNaval / (float)num;
		int num6 = (int)num5;
		if (num4 + num6 < realBattleSizeForNaval)
		{
			float num7 = num3 - (float)num4;
			float num8 = num5 - (float)num6;
			if (num7 > num8)
			{
				num4++;
			}
		}
		return num4;
	}

	public void SetDesiredTroopCountOfShip(MissionShip ship, int desiredTroopCount)
	{
		GetTeamAgents(ship.Team.TeamSide, out var teamAgents);
		teamAgents.SetDesiredTroopCountOfShip(ship, desiredTroopCount);
	}

	public void SetTroopClassFilter(MissionShip ship, TroopTraitsMask troopClassFilter)
	{
		GetTeamAgents(ship.Team.TeamSide, out var teamAgents);
		teamAgents.SetTroopClassFilter(ship, troopClassFilter);
	}

	public void SetTroopTraitsFilter(MissionShip ship, TroopTraitsMask troopTraitsFilter)
	{
		GetTeamAgents(ship.Team.TeamSide, out var teamAgents);
		teamAgents.SetTroopTraitsFilter(ship, troopTraitsFilter);
	}

	internal void AssignAndTeleportCrewToShipMachines(MissionShip ship)
	{
		TeamSideEnum teamSide = ship.Team.TeamSide;
		GetTeamAgents(teamSide, out var teamAgents);
		teamAgents.AssignAndTeleportCrewToShipMachines(ship);
	}

	internal void AssignAndTeleportCrewToShipMachines(TeamSideEnum teamSide)
	{
		GetTeamAgents(teamSide, out var teamAgents);
		teamAgents.AssignAndTeleportCrewToShipMachines();
	}

	private bool GetTeamAgents(TeamSideEnum teamSide, out NavalTeamAgents teamAgents)
	{
		teamAgents = _teamAgentsData.FirstOrDefault((NavalTeamAgents mts) => mts.TeamSide == teamSide);
		return teamAgents != null;
	}

	internal void OnAgentSteppedShipChanged(Agent agent, MissionShip newShip)
	{
		agent.UpdateAgentStats();
	}

	private void OnShipSpawned(MissionShip ship)
	{
		if (GetTeamAgents(ship.Team.TeamSide, out var teamAgents))
		{
			teamAgents.OnShipSpawned(ship, _ignoreTroopCapacities[(int)teamAgents.TeamSide]);
		}
	}

	public static float ComputeReinforcementSpawnDuration(int reservedTroopCount)
	{
		return 0.5f + 2.5f / (float)(1 + reservedTroopCount / 50);
	}

	private void OnShipRemoved(MissionShip ship)
	{
		if (ship.Team != null && GetTeamAgents(ship.Team.TeamSide, out var teamAgents))
		{
			teamAgents.OnShipRemoved(ship);
		}
	}

	private void OnShipTransferredToFormation(MissionShip ship, Formation oldFormation)
	{
		GetTeamAgents(ship.Team.TeamSide, out var teamAgents);
		teamAgents.OnShipTransferredToFormation(ship, oldFormation);
	}

	private void OnShipTransferredToTeam(MissionShip ship, Team oldTeam, Formation oldFormation)
	{
		if (oldTeam != null)
		{
			GetTeamAgents(oldTeam.TeamSide, out var teamAgents);
			teamAgents.OnShipRemoved(ship);
		}
		GetTeamAgents(ship.Team.TeamSide, out var teamAgents2);
		teamAgents2.OnShipSpawned(ship, _ignoreTroopCapacities[(int)teamAgents2.TeamSide]);
	}

	private void OnShipCaptured(MissionShip ship, MissionShip ship2, Formation formation, Formation formation2)
	{
		if (formation != null)
		{
			GetTeamAgents(formation.Team.TeamSide, out var teamAgents);
			teamAgents.OnShipCaptured(ship, ship2);
			formation.ApplyActionOnEachUnit(delegate(Agent agent)
			{
				agent.GetComponent<AgentNavalComponent>().OnShipCaptured();
			});
		}
		GetTeamAgents(formation2.Team.TeamSide, out var teamAgents2);
		teamAgents2.OnShipCaptured(ship2, ship);
		formation2.ApplyActionOnEachUnit(delegate(Agent agent)
		{
			agent.GetComponent<AgentNavalComponent>().OnShipCaptured();
		});
	}

	private void OnShipTeleported(MissionShip ship, MatrixFrame oldFrame, MatrixFrame targetFrame)
	{
		MBReadOnlyList<Agent> activeAgentsOfShip = GetActiveAgentsOfShip(ship);
		if (activeAgentsOfShip == null)
		{
			return;
		}
		foreach (Agent item in activeAgentsOfShip)
		{
			TeleportAgentToFrame(item, targetFrame.TransformToParent(oldFrame.TransformToLocal(item.GetWorldFrame().ToGroundMatrixFrame())));
		}
	}

	private void OnShipPreparedForAbandonment(MissionShip ship)
	{
		SetSpawnReinforcementsForShip(ship, value: false);
	}

	internal static float GetAgentPriority(Agent agent)
	{
		if (agent.IsMainAgent)
		{
			return 500f;
		}
		if (agent.IsPlayerTroop)
		{
			return 400f;
		}
		if (agent.Formation != null && agent == agent.Formation.Captain)
		{
			return 300f;
		}
		return (agent.IsHero ? 100f : 0f) + agent.Origin.Troop.GetPower();
	}

	private void OnMissionEnd()
	{
		SetDeploymentMode(value: false);
	}

	internal static void TeleportAgentToFrame(Agent agent, in MatrixFrame teleportFrame)
	{
		if (!agent.Position.NearlyEquals(in teleportFrame.origin, 0.001f) || !agent.GetMovementDirection().NearlyEquals(teleportFrame.rotation.f.AsVec2, 0.001f))
		{
			agent.TeleportToPosition(teleportFrame.origin);
			agent.LookDirection = teleportFrame.rotation.f;
			agent.SetMovementDirection(teleportFrame.rotation.f.AsVec2.Normalized());
		}
	}

	internal static void TeleportAndAssignAgentToMachine(Agent agent, NavalShipAgents agentShip, UsableMachine shipMachine)
	{
		TryStopMachineUseAndReattachAgent(agent);
		TryUseMachineAndDetachAgent(agent, agentShip, shipMachine, teleportAndUseInstantly: true, out var _);
	}

	internal static void TryStopMachineUseAndReattachAgent(Agent agent)
	{
		if (agent.IsDetachedFromFormation)
		{
			agent.TryAttachToFormation();
		}
		if (agent.InteractingWithAnyGameObject() && !(agent.CurrentlyUsedGameObject is SpawnedItemEntity))
		{
			agent.StopUsingGameObjectMT();
		}
	}

	internal static bool TryUseMachineAndDetachAgent(Agent agent, NavalShipAgents ownerShipAgents, UsableMachine machine, bool teleportAndUseInstantly, out bool isDetached)
	{
		isDetached = false;
		if (machine.PilotAgent == null && !machine.PilotStandingPoint.IsAIMovingTo(agent))
		{
			if (agent.IsAIControlled && agent.IsDetachableFromFormation && ownerShipAgents.Ship.Formation != null && ownerShipAgents.Ship.Formation == agent.Formation)
			{
				machine.AddAgentAtSlotIndex(agent, 0);
				isDetached = true;
			}
			agent.UseGameObject(machine.PilotStandingPoint);
			if (teleportAndUseInstantly)
			{
				machine.OnPilotAssignedDuringSpawn();
			}
			return true;
		}
		return false;
	}
}
