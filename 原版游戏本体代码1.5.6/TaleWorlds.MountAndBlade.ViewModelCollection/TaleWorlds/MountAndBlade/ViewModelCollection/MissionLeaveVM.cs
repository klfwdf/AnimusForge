using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection;

public class MissionLeaveVM : ViewModel
{
	private Func<float> _getMissionEndTimer;

	private Func<float> _getMissionEndTimeInSeconds;

	private float _maxTime;

	private float _currentTime;

	private string _leaveText;

	[DataSourceProperty]
	public string LeaveText
	{
		get
		{
			return _leaveText;
		}
		set
		{
			if (value != _leaveText)
			{
				_leaveText = value;
				OnPropertyChangedWithValue(value, "LeaveText");
			}
		}
	}

	[DataSourceProperty]
	public float MaxTime
	{
		get
		{
			return _maxTime;
		}
		set
		{
			if (value != _maxTime)
			{
				_maxTime = value;
				OnPropertyChangedWithValue(value, "MaxTime");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentTime
	{
		get
		{
			return _currentTime;
		}
		set
		{
			if (value != _currentTime)
			{
				_currentTime = value;
				OnPropertyChangedWithValue(value, "CurrentTime");
			}
		}
	}

	public MissionLeaveVM(Func<float> getMissionEndTimer, Func<float> getMissionEndTimeInSeconds)
	{
		_getMissionEndTimer = getMissionEndTimer;
		_getMissionEndTimeInSeconds = getMissionEndTimeInSeconds;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		LeaveText = GameTexts.FindText("str_leaving").ToString();
	}

	public void Tick(float dt)
	{
		CurrentTime = _getMissionEndTimer();
		MaxTime = _getMissionEndTimeInSeconds();
	}
}
