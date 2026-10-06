using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.AI.TeamAI;

public class NavalQuerySystem
{
	private readonly MBList<Tuple<Formation, Vec2>> _temporaryFormationPositionTupleContainer = new MBList<Tuple<Formation, Vec2>>();

	private readonly MBList<MissionShip> _temporaryMissionShipContainer = new MBList<MissionShip>();

	private readonly Dictionary<(MissionShip, MissionShip), bool> _shipsInCriticalZoneContainer = new Dictionary<(MissionShip, MissionShip), bool>();

	private readonly QueryData<Vec2> _averageShipPosition;

	private readonly QueryData<Vec2> _averageEnemyShipPosition;

	private readonly QueryData<MBReadOnlyList<Formation>> _formationsInShipsInLeftToRightOrder;

	private readonly QueryData<MBReadOnlyList<MissionShip>> _enemyShipsInLeftToRightOrder;

	private readonly QueryData<MBReadOnlyList<MissionShip>> _enemyShipsWithFormationsInLeftToRightOrder;

	private readonly QueryData<MBReadOnlyList<MissionShip>> _teamShipsWithFormationsInLeftToRightOrder;

	private readonly QueryData<Dictionary<(MissionShip, MissionShip), bool>> _shipInCriticalZoneDictionary;

	private readonly QueryData<float> _closestDistanceSquaredToEnemyShip;

	private NavalShipsLogic _navalShipsLogic;

	private Team _team;

	public Vec2 AverageShipPosition => _averageShipPosition.Value;

	public Vec2 AverageEnemyShipPosition => _averageEnemyShipPosition.Value;

	public MBReadOnlyList<Formation> FormationsInShipsInLeftToRightOrder => _formationsInShipsInLeftToRightOrder.Value.ToMBList();

	public MBReadOnlyList<MissionShip> EnemyShipsInLeftToRightOrder => _enemyShipsInLeftToRightOrder.Value;

	public MBReadOnlyList<MissionShip> EnemyShipsWithFormationsInLeftToRightOrder => _enemyShipsWithFormationsInLeftToRightOrder.Value;

	public MBReadOnlyList<MissionShip> TeamShipsWithFormationsInLeftToRightOrder => _teamShipsWithFormationsInLeftToRightOrder.Value;

	public float ClosestDistanceSquaredToEnemyShip => _closestDistanceSquaredToEnemyShip.Value;

	public NavalQuerySystem(Team team)
	{
		_ = Mission.Current;
		_team = team;
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_averageShipPosition = new QueryData<Vec2>(delegate
		{
			Vec2 vec = new Vec2(0f, 0f);
			int num = 0;
			foreach (Formation item in _team.FormationsIncludingEmpty)
			{
				if (item.CountOfUnits > 0)
				{
					_navalShipsLogic.GetShip(_team.TeamSide, item.FormationIndex, out var ship);
					vec += ship.GameEntity.GlobalPosition.AsVec2;
					num++;
				}
			}
			return (num <= 0) ? vec : (vec / num);
		}, 1f);
		_averageEnemyShipPosition = new QueryData<Vec2>(delegate
		{
			Vec2 vec = new Vec2(0f, 0f);
			int num = 0;
			foreach (Team team2 in Mission.Current.Teams)
			{
				if (_team.IsEnemyOf(team2))
				{
					foreach (Formation item2 in team2.FormationsIncludingEmpty)
					{
						if (item2.CountOfUnits > 0)
						{
							_navalShipsLogic.GetShip(team2.TeamSide, item2.FormationIndex, out var ship);
							vec += ship.GameEntity.GlobalPosition.AsVec2;
							num++;
						}
					}
				}
			}
			return (num <= 0) ? vec : (vec / num);
		}, 1f);
		_formationsInShipsInLeftToRightOrder = new QueryData<MBReadOnlyList<Formation>>(delegate
		{
			_temporaryFormationPositionTupleContainer.Clear();
			foreach (Formation item3 in _team.FormationsIncludingEmpty)
			{
				if (item3.CountOfUnits > 0 && _navalShipsLogic.GetShip(_team.TeamSide, item3.FormationIndex, out var ship))
				{
					_temporaryFormationPositionTupleContainer.Add(new Tuple<Formation, Vec2>(item3, ship.GameEntity.GlobalPosition.AsVec2));
				}
			}
			return (from fst in _temporaryFormationPositionTupleContainer
				orderby (fst.Item2 - AverageShipPosition).DotProduct((AverageEnemyShipPosition - AverageShipPosition).LeftVec()) descending
				select fst.Item1).ToMBList();
		}, 5f);
		_enemyShipsInLeftToRightOrder = new QueryData<MBReadOnlyList<MissionShip>>(delegate
		{
			_temporaryMissionShipContainer.Clear();
			foreach (Team team3 in Mission.Current.Teams)
			{
				if (_team.Side.IsOpponentOf(team3.Side))
				{
					_navalShipsLogic.FillTeamShips(team3.TeamSide, _temporaryMissionShipContainer);
				}
			}
			return _temporaryMissionShipContainer.OrderByDescending((MissionShip sl) => (sl.GameEntity.GlobalPosition.AsVec2 - AverageEnemyShipPosition).DotProduct((AverageShipPosition - AverageEnemyShipPosition).LeftVec())).ToMBList();
		}, 5f);
		_enemyShipsWithFormationsInLeftToRightOrder = new QueryData<MBReadOnlyList<MissionShip>>(delegate
		{
			_temporaryMissionShipContainer.Clear();
			foreach (Team team4 in Mission.Current.Teams)
			{
				if (_team.Side.IsOpponentOf(team4.Side))
				{
					foreach (Formation item4 in team4.FormationsIncludingEmpty)
					{
						if (item4.CountOfUnits > 0 && _navalShipsLogic.GetShip(team4.TeamSide, item4.FormationIndex, out var ship))
						{
							_temporaryMissionShipContainer.Add(ship);
						}
					}
				}
			}
			return _temporaryMissionShipContainer.OrderByDescending((MissionShip sl) => (sl.GameEntity.GlobalPosition.AsVec2 - AverageEnemyShipPosition).DotProduct((AverageShipPosition - AverageEnemyShipPosition).LeftVec())).ToMBList();
		}, 5f);
		_teamShipsWithFormationsInLeftToRightOrder = new QueryData<MBReadOnlyList<MissionShip>>(delegate
		{
			_temporaryMissionShipContainer.Clear();
			foreach (Formation item5 in _team.FormationsIncludingEmpty)
			{
				if (item5.CountOfUnits > 0 && _navalShipsLogic.GetShip(_team.TeamSide, item5.FormationIndex, out var ship))
				{
					_temporaryMissionShipContainer.Add(ship);
				}
			}
			return _temporaryMissionShipContainer.OrderByDescending((MissionShip sl) => (sl.GameEntity.GlobalPosition.AsVec2 - AverageShipPosition).DotProduct((AverageShipPosition - AverageShipPosition).LeftVec())).ToMBList();
		}, 5f);
		_shipInCriticalZoneDictionary = new QueryData<Dictionary<(MissionShip, MissionShip), bool>>(delegate
		{
			MBReadOnlyList<MissionShip> allShips = _navalShipsLogic.AllShips;
			foreach (MissionShip item6 in allShips)
			{
				foreach (MissionShip connectedShip in item6.GetConnectedShips())
				{
					(MissionShip, MissionShip) key = ((item6.GetHashCode() < connectedShip.GetHashCode()) ? (item6, connectedShip) : (connectedShip, item6));
					if (item6.IsShipInCriticalZoneBetween(connectedShip, allShips))
					{
						_shipsInCriticalZoneContainer[key] = true;
					}
					else
					{
						_shipsInCriticalZoneContainer[key] = false;
					}
				}
			}
			return _shipsInCriticalZoneContainer;
		}, 5f);
		_closestDistanceSquaredToEnemyShip = new QueryData<float>(delegate
		{
			float num = float.MaxValue;
			foreach (Formation item7 in FormationsInShipsInLeftToRightOrder)
			{
				if (item7.CountOfUnits > 0 && item7.CachedClosestEnemyFormationDistanceSquared < num)
				{
					num = item7.CachedClosestEnemyFormationDistanceSquared;
				}
			}
			return num;
		}, 1f);
		InitializeTelemetryScopeNames();
	}

	public void ForceExpireSameSideShipLists()
	{
		_teamShipsWithFormationsInLeftToRightOrder.Expire();
		_formationsInShipsInLeftToRightOrder.Expire();
	}

	public void ForceExpireAll()
	{
		_averageShipPosition.Expire();
		_averageEnemyShipPosition.Expire();
		_formationsInShipsInLeftToRightOrder.Expire();
		_enemyShipsInLeftToRightOrder.Expire();
		_enemyShipsWithFormationsInLeftToRightOrder.Expire();
		_teamShipsWithFormationsInLeftToRightOrder.Expire();
		_shipInCriticalZoneDictionary.Expire();
		_closestDistanceSquaredToEnemyShip.Expire();
	}

	public bool IsAnyShipInCriticalZoneBetween(MissionShip ship1, MissionShip ship2)
	{
		if (_shipInCriticalZoneDictionary == null || _shipInCriticalZoneDictionary.Value == null)
		{
			return false;
		}
		Dictionary<(MissionShip, MissionShip), bool> value = _shipInCriticalZoneDictionary.Value;
		(MissionShip, MissionShip) key = ((ship1.GetHashCode() < ship2.GetHashCode()) ? (ship1, ship2) : (ship2, ship1));
		bool value2;
		return value.TryGetValue(key, out value2) && value2;
	}

	private void InitializeTelemetryScopeNames()
	{
	}
}
