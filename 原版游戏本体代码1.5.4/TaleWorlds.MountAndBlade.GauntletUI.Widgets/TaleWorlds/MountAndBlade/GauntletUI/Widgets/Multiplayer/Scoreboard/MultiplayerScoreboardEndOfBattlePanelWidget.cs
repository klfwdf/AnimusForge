using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Scoreboard;

public class MultiplayerScoreboardEndOfBattlePanelWidget : Widget
{
	private bool _isStarted;

	private bool _isPreStateFinished;

	private bool _isFinished;

	private float _timePassed;

	private string _openedSoundEvent = "panels/scoreboard_flags";

	private bool _isAvailable;

	private float _firstDelay;

	private float _secondDelay;

	[Editor(false)]
	public bool IsAvailable
	{
		get
		{
			return _isAvailable;
		}
		set
		{
			if (value != _isAvailable)
			{
				_isAvailable = value;
				OnPropertyChanged(value, "IsAvailable");
				AvailableUpdated();
			}
		}
	}

	[Editor(false)]
	public float FirstDelay
	{
		get
		{
			return _firstDelay;
		}
		set
		{
			if (value != _firstDelay)
			{
				_firstDelay = value;
				OnPropertyChanged(value, "FirstDelay");
			}
		}
	}

	[Editor(false)]
	public float SecondDelay
	{
		get
		{
			return _secondDelay;
		}
		set
		{
			if (value != _secondDelay)
			{
				_secondDelay = value;
				OnPropertyChanged(value, "SecondDelay");
			}
		}
	}

	public MultiplayerScoreboardEndOfBattlePanelWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!_isFinished && _isStarted)
		{
			_timePassed += dt;
			if (_timePassed >= SecondDelay)
			{
				_isFinished = true;
				SetState("Opened");
				base.Context.TwoDimensionContext.PlaySound(_openedSoundEvent);
			}
			else if (_timePassed >= FirstDelay && !_isPreStateFinished)
			{
				_isPreStateFinished = true;
				SetState("PreOpened");
			}
		}
	}

	public void StartAnimation()
	{
		_isStarted = true;
		_isFinished = false;
		_isPreStateFinished = false;
		_timePassed = 0f;
		AddState("PreOpened");
		AddState("Opened");
	}

	private void Reset()
	{
		_isStarted = false;
		_isPreStateFinished = false;
		_isFinished = false;
		SetState("Default");
	}

	private void AvailableUpdated()
	{
		if (IsAvailable)
		{
			StartAnimation();
		}
		else
		{
			Reset();
		}
	}
}
