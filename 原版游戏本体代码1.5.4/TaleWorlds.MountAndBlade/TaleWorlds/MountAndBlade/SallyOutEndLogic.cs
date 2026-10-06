using System.Linq;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class SallyOutEndLogic : MissionLogic
{
	private enum EndConditionCheckState
	{
		Deactive,
		Active,
		Idle
	}

	private EndConditionCheckState _checkState;

	private float _nextCheckTime;

	private float _dtSum;

	public bool IsSallyOutOver { get; private set; }

	public override void OnMissionTick(float dt)
	{
		if (!CheckTimer(dt))
		{
			return;
		}
		if (_checkState == EndConditionCheckState.Deactive)
		{
			foreach (Team item in base.Mission.Teams.Where((Team t) => t.Side == BattleSideEnum.Defender))
			{
				foreach (Formation item2 in item.FormationsIncludingSpecialAndEmpty)
				{
					if (item2.CountOfUnits > 0 && item2.CountOfUnits > 0 && !TeamAISiegeComponent.IsFormationInsideCastle(item2, includeOnlyPositionedUnits: true, 0.1f))
					{
						_checkState = EndConditionCheckState.Active;
						return;
					}
				}
			}
			return;
		}
		if (_checkState == EndConditionCheckState.Idle)
		{
			_checkState = EndConditionCheckState.Active;
		}
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		if (IsSallyOutOver)
		{
			missionResult = MissionResult.CreateSuccessful(base.Mission);
			return true;
		}
		if (_checkState != EndConditionCheckState.Active)
		{
			return false;
		}
		foreach (Team team in base.Mission.Teams)
		{
			switch (team.Side)
			{
			case BattleSideEnum.Attacker:
				if (TeamAISiegeComponent.IsFormationGroupInsideCastle(team.FormationsIncludingSpecialAndEmpty, includeOnlyPositionedUnits: false, 0.1f))
				{
					_checkState = EndConditionCheckState.Idle;
					return false;
				}
				break;
			case BattleSideEnum.Defender:
				if (team.FormationsIncludingEmpty.Any((Formation f) => f.CountOfUnits > 0 && !TeamAISiegeComponent.IsFormationInsideCastle(f, includeOnlyPositionedUnits: false, 0.9f)))
				{
					_checkState = EndConditionCheckState.Idle;
					return false;
				}
				break;
			}
		}
		IsSallyOutOver = true;
		missionResult = MissionResult.CreateSuccessful(base.Mission);
		return true;
	}

	private bool CheckTimer(float dt)
	{
		_dtSum += dt;
		if (_dtSum < _nextCheckTime)
		{
			return false;
		}
		_dtSum = 0f;
		_nextCheckTime = 0.8f + MBRandom.RandomFloat * 0.4f;
		return true;
	}
}
