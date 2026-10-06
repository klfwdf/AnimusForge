using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets.Hideout;

public class MissionStealthFailCounterVM : ViewModel
{
	private TextObject _countDownTextObject;

	private float _failCounterElapsedTime;

	private string _countDownText;

	private float _failCounterMaxTime;

	private bool _isCounterActive;

	[DataSourceProperty]
	public string CountDownText
	{
		get
		{
			return _countDownText;
		}
		set
		{
			if (value != _countDownText)
			{
				_countDownText = value;
				OnPropertyChangedWithValue(value, "CountDownText");
			}
		}
	}

	[DataSourceProperty]
	public float FailCounterElapsedTime
	{
		get
		{
			return _failCounterElapsedTime;
		}
		set
		{
			if (value != _failCounterElapsedTime)
			{
				_failCounterElapsedTime = value;
				OnPropertyChangedWithValue(value, "FailCounterElapsedTime");
			}
		}
	}

	[DataSourceProperty]
	public float FailCounterMaxTime
	{
		get
		{
			return _failCounterMaxTime;
		}
		set
		{
			if (value != _failCounterMaxTime)
			{
				_failCounterMaxTime = value;
				OnPropertyChangedWithValue(value, "FailCounterMaxTime");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCounterActive
	{
		get
		{
			return _isCounterActive;
		}
		set
		{
			if (value != _isCounterActive)
			{
				_isCounterActive = value;
				OnPropertyChangedWithValue(value, "IsCounterActive");
			}
		}
	}

	public MissionStealthFailCounterVM()
	{
		_countDownTextObject = new TextObject("{=pY8lnL11}Mission will fail in: {SEC}");
	}

	public void UpdateFailCounter(float failCounterElapsedTime, float failCounterMaxTime, bool isStealthFailCounterMissionLogicActive)
	{
		IsCounterActive = !BannerlordConfig.HideBattleUI && !MBCommon.IsPaused && isStealthFailCounterMissionLogicActive && failCounterElapsedTime > 0f;
		FailCounterMaxTime = failCounterMaxTime;
		if (IsCounterActive)
		{
			FailCounterElapsedTime = FailCounterMaxTime - failCounterElapsedTime;
			_countDownTextObject.SetTextVariable("SEC", MathF.Ceiling(FailCounterElapsedTime));
			CountDownText = _countDownTextObject.ToString();
		}
	}
}
