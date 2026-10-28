using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class SallyOutReinforcementSpawnTimer : ICustomReinforcementSpawnTimer
{
	private BasicMissionTimer _besiegedSideTimer;

	private BasicMissionTimer _besiegerSideTimer;

	private float _besiegedInterval;

	private float _besiegerInterval;

	private float _besiegerIntervalChange;

	private int _besiegerRemainingIntervalChanges;

	public SallyOutReinforcementSpawnTimer(float besiegedInterval, float besiegerInterval, float besiegerIntervalChange, int besiegerIntervalChangeCount)
	{
		_besiegedSideTimer = new BasicMissionTimer();
		_besiegedInterval = besiegedInterval;
		_besiegerSideTimer = new BasicMissionTimer();
		_besiegerInterval = besiegerInterval;
		_besiegerIntervalChange = besiegerIntervalChange;
		_besiegerRemainingIntervalChanges = besiegerIntervalChangeCount;
	}

	public bool Check(BattleSideEnum side)
	{
		switch (side)
		{
		case BattleSideEnum.Attacker:
			if (_besiegerSideTimer.ElapsedTime >= _besiegerInterval)
			{
				if (_besiegerRemainingIntervalChanges > 0)
				{
					_besiegerInterval -= _besiegerIntervalChange;
					_besiegerRemainingIntervalChanges--;
				}
				_besiegerSideTimer.Reset();
				return true;
			}
			break;
		case BattleSideEnum.Defender:
			if (_besiegedSideTimer.ElapsedTime >= _besiegedInterval)
			{
				_besiegedSideTimer.Reset();
				return true;
			}
			break;
		}
		return false;
	}

	public void ResetTimer(BattleSideEnum side)
	{
		switch (side)
		{
		case BattleSideEnum.Attacker:
			_besiegerSideTimer.Reset();
			break;
		case BattleSideEnum.Defender:
			_besiegedSideTimer.Reset();
			break;
		}
	}
}
