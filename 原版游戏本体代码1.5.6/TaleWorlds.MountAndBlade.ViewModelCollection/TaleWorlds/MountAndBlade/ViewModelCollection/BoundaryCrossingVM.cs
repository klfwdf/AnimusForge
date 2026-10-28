using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection;

public class BoundaryCrossingVM : ViewModel
{
	private readonly Mission _mission;

	private readonly MissionBoundaryCrossingHandler _missionBoundaryCrossingHandler;

	private readonly Action<bool> _onEscapeMenuToggled;

	private float _duration;

	private bool _show = true;

	private string _warningText;

	private double _warningProgress = -1.0;

	private int _warningIntProgress = -1;

	private int _countdown;

	[DataSourceProperty]
	public bool Show
	{
		get
		{
			return _show;
		}
		set
		{
			if (value != _show)
			{
				_show = value;
				OnPropertyChangedWithValue(value, "Show");
				_onEscapeMenuToggled(value);
			}
		}
	}

	[DataSourceProperty]
	public string WarningText
	{
		get
		{
			return _warningText;
		}
		set
		{
			if (value != _warningText)
			{
				_warningText = value;
				OnPropertyChangedWithValue(value, "WarningText");
			}
		}
	}

	[DataSourceProperty]
	public double WarningProgress
	{
		get
		{
			return _warningProgress;
		}
		set
		{
			if (value != _warningProgress)
			{
				_warningProgress = value;
				OnPropertyChangedWithValue(value, "WarningProgress");
				WarningIntProgress = (int)(value * 100.0);
			}
		}
	}

	[DataSourceProperty]
	public int WarningIntProgress
	{
		get
		{
			return _warningIntProgress;
		}
		set
		{
			if (value != _warningIntProgress)
			{
				_warningIntProgress = value;
				OnPropertyChangedWithValue(value, "WarningIntProgress");
			}
		}
	}

	[DataSourceProperty]
	public int Countdown
	{
		get
		{
			return _countdown;
		}
		set
		{
			if (value != _countdown)
			{
				_countdown = value;
				OnPropertyChangedWithValue(value, "Countdown");
			}
		}
	}

	public BoundaryCrossingVM(Mission mission, Action<bool> onEscapeMenuToggled)
	{
		_onEscapeMenuToggled = onEscapeMenuToggled;
		_mission = mission;
		_missionBoundaryCrossingHandler = _mission.GetMissionBehavior<MissionBoundaryCrossingHandler>();
		_missionBoundaryCrossingHandler.StartTime += OnStartTime;
		_missionBoundaryCrossingHandler.StopTime += OnStopTime;
		_missionBoundaryCrossingHandler.TimeCount += OnTimeCount;
		_show = false;
		WarningText = "";
		WarningProgress = 0.0;
	}

	private void OnStartTime(float duration, float progress)
	{
		TextObject text = new TextObject("{=eGuQKRhb}You are leaving the area!");
		switch (_mission.Mode)
		{
		case MissionMode.CutScene:
			text = TextObject.GetEmpty();
			break;
		case MissionMode.Conversation:
			text = TextObject.GetEmpty();
			break;
		case MissionMode.Barter:
			text = TextObject.GetEmpty();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case MissionMode.StartUp:
		case MissionMode.Battle:
		case MissionMode.Duel:
		case MissionMode.Stealth:
		case MissionMode.Deployment:
			break;
		}
		MBTextManager.SetTextVariable("MISSION_MODE", text);
		WarningText = GameTexts.FindText("str_out_of_mission_bound").ToString();
		WarningProgress = 0.0;
		_duration = duration;
		Show = true;
	}

	private void OnStopTime()
	{
		Show = false;
	}

	private void OnTimeCount(float progress)
	{
		WarningProgress = progress;
		Countdown = TaleWorlds.Library.MathF.Ceiling((1f - progress) * _duration);
	}
}
