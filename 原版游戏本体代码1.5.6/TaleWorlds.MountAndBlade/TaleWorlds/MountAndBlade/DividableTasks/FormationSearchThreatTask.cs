namespace TaleWorlds.MountAndBlade.DividableTasks;

public class FormationSearchThreatTask : DividableTask
{
	private Agent _targetAgent;

	private const float CheckCountRatio = 0.1f;

	private RangedSiegeWeapon _weapon;

	private Formation _formation;

	private int _storedIndex;

	private int _checkCountPerTick;

	private bool _result;

	protected override bool UpdateExtra()
	{
		_result = _formation.HasUnitWithConditionLimitedRandom((Agent agent) => _weapon.CanShootAtAgent(agent), _storedIndex, _checkCountPerTick, out _targetAgent);
		_storedIndex += _checkCountPerTick;
		if (_storedIndex < _formation.CountOfUnits)
		{
			return _result;
		}
		return true;
	}

	public void Prepare(Formation formation, RangedSiegeWeapon weapon)
	{
		ResetTaskStatus();
		_formation = formation;
		_weapon = weapon;
		_storedIndex = 0;
		_checkCountPerTick = (int)((float)_formation.CountOfUnits * 0.1f) + 1;
	}

	public bool GetResult(out Agent targetAgent)
	{
		targetAgent = _targetAgent;
		return _result;
	}
}
