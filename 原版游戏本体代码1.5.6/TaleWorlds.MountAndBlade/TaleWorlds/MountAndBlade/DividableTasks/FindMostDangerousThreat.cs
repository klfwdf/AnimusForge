using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.DividableTasks;

public class FindMostDangerousThreat : DividableTask
{
	private Agent _targetAgent;

	private FormationSearchThreatTask _formationSearchThreatTask;

	private List<Threat> _threats;

	private RangedSiegeWeapon _weapon;

	private Threat _currentThreat;

	private bool _hasOngoingThreatTask;

	public FindMostDangerousThreat(DividableTask continueToTask = null)
		: base(continueToTask)
	{
		SetTaskFinished();
		_formationSearchThreatTask = new FormationSearchThreatTask();
	}

	protected override bool UpdateExtra()
	{
		bool flag = false;
		if (_hasOngoingThreatTask)
		{
			if (_formationSearchThreatTask.Update())
			{
				_hasOngoingThreatTask = false;
				if (!(flag = _formationSearchThreatTask.GetResult(out _targetAgent)))
				{
					_threats.Remove(_currentThreat);
					_currentThreat = null;
				}
			}
		}
		else
		{
			int num = 5;
			do
			{
				num--;
				flag = true;
				int num2 = -1;
				float num3 = float.MinValue;
				bool flag2 = false;
				for (int i = 0; i < _threats.Count; i++)
				{
					Threat threat = _threats[i];
					if (!flag2 || threat.ForceTarget)
					{
						if (!flag2 && threat.ForceTarget)
						{
							flag2 = true;
							num3 = threat.ThreatValue;
							num2 = i;
						}
						else if (threat.ThreatValue > num3)
						{
							num3 = threat.ThreatValue;
							num2 = i;
						}
					}
				}
				if (num2 < 0)
				{
					continue;
				}
				_currentThreat = _threats[num2];
				if (_currentThreat.Formation != null)
				{
					_formationSearchThreatTask.Prepare(_currentThreat.Formation, _weapon);
					_hasOngoingThreatTask = true;
					flag = false;
					break;
				}
				if ((_currentThreat.TargetableObject == null && _currentThreat.Agent == null) || !_weapon.CanShootAtThreat(_currentThreat))
				{
					if (!_currentThreat.ForceTarget)
					{
						_threats.RemoveAt(num2);
					}
					_currentThreat = null;
					flag = false;
				}
			}
			while (!flag && num > 0);
		}
		if (!flag)
		{
			return _threats.Count == 0;
		}
		return true;
	}

	public void Prepare(List<Threat> threats, RangedSiegeWeapon weapon)
	{
		ResetTaskStatus();
		_hasOngoingThreatTask = false;
		_weapon = weapon;
		_threats = threats;
		foreach (Threat threat in _threats)
		{
			threat.ThreatValue *= 0.9f + MBRandom.RandomFloat * 0.2f;
		}
		if (_currentThreat != null)
		{
			_currentThreat = _threats.SingleOrDefault((Threat t) => t.Equals(_currentThreat));
			if (_currentThreat != null)
			{
				_currentThreat.ThreatValue *= 2f;
			}
		}
	}

	public Threat GetResult(out Agent targetAgent)
	{
		targetAgent = _targetAgent;
		return _currentThreat;
	}
}
