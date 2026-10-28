using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class BattleReinforcementsSpawnController : MissionLogic
{
	private IMissionAgentSpawnLogic _missionAgentSpawnLogic;

	private bool[] _sideReinforcementSuspended = new bool[2];

	private bool[] _sideRequiresUpdate = new bool[2];

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_missionAgentSpawnLogic = base.Mission.GetMissionBehavior<IMissionAgentSpawnLogic>();
	}

	public override void AfterStart()
	{
		foreach (Team team in base.Mission.Teams)
		{
			foreach (Formation item in team.FormationsIncludingEmpty)
			{
				item.OnBeforeMovementOrderApplied += OnBeforeFormationMovementOrderApplied;
			}
		}
	}

	public override void OnMissionTick(float dt)
	{
		for (int i = 0; i < 2; i++)
		{
			if (_sideRequiresUpdate[i])
			{
				UpdateSide((BattleSideEnum)i);
				_sideRequiresUpdate[i] = false;
			}
		}
	}

	protected override void OnEndMission()
	{
		foreach (Team team in base.Mission.Teams)
		{
			foreach (Formation item in team.FormationsIncludingEmpty)
			{
				item.OnBeforeMovementOrderApplied -= OnBeforeFormationMovementOrderApplied;
			}
		}
	}

	private void UpdateSide(BattleSideEnum side)
	{
		if (IsBattleSideRetreating(side))
		{
			if (!_sideReinforcementSuspended[(int)side] && _missionAgentSpawnLogic.IsSideSpawnEnabled(side))
			{
				_missionAgentSpawnLogic.StopSpawner(side);
				_sideReinforcementSuspended[(int)side] = true;
			}
		}
		else if (_sideReinforcementSuspended[(int)side])
		{
			_missionAgentSpawnLogic.StartSpawner(side);
			_sideReinforcementSuspended[(int)side] = false;
		}
	}

	private bool IsBattleSideRetreating(BattleSideEnum side)
	{
		bool result = true;
		foreach (Team team in base.Mission.Teams)
		{
			if (team.Side != side)
			{
				continue;
			}
			foreach (Formation item in team.FormationsIncludingEmpty)
			{
				if (item.CountOfUnits > 0 && item.GetReadonlyMovementOrderReference().OrderEnum != MovementOrder.MovementOrderEnum.Retreat)
				{
					result = false;
					break;
				}
			}
		}
		return result;
	}

	private void OnBeforeFormationMovementOrderApplied(Formation formation, MovementOrder.MovementOrderEnum orderEnum)
	{
		if (formation.GetReadonlyMovementOrderReference().OrderEnum == MovementOrder.MovementOrderEnum.Retreat || orderEnum == MovementOrder.MovementOrderEnum.Retreat)
		{
			int side = (int)formation.Team.Side;
			_sideRequiresUpdate[side] = true;
		}
	}
}
