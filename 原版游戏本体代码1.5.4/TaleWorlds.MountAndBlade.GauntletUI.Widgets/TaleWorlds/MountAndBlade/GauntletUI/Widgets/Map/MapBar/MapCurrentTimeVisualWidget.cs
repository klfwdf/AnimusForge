using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.MapBar;

public class MapCurrentTimeVisualWidget : Widget
{
	private int _currenTimeState;

	private ButtonWidget _fastForwardButton;

	private ButtonWidget _playButton;

	private ButtonWidget _pauseButton;

	[Editor(false)]
	public int CurrentTimeState
	{
		get
		{
			return _currenTimeState;
		}
		set
		{
			if (_currenTimeState != value)
			{
				_currenTimeState = value;
				OnPropertyChanged(value, "CurrentTimeState");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget FastForwardButton
	{
		get
		{
			return _fastForwardButton;
		}
		set
		{
			if (_fastForwardButton != value)
			{
				_fastForwardButton = value;
				OnPropertyChanged(value, "FastForwardButton");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget PlayButton
	{
		get
		{
			return _playButton;
		}
		set
		{
			if (_playButton != value)
			{
				_playButton = value;
				OnPropertyChanged(value, "PlayButton");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget PauseButton
	{
		get
		{
			return _pauseButton;
		}
		set
		{
			if (_pauseButton != value)
			{
				_pauseButton = value;
				OnPropertyChanged(value, "PauseButton");
			}
		}
	}

	public MapCurrentTimeVisualWidget(UIContext context)
		: base(context)
	{
		AddState("Disabled");
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (base.IsDisabled)
		{
			SetState("Disabled");
			return;
		}
		SetState("Default");
		bool isSelected = false;
		bool isSelected2 = false;
		bool isSelected3 = false;
		switch (CurrentTimeState)
		{
		case 2:
		case 4:
		case 5:
			isSelected2 = true;
			break;
		case 1:
		case 3:
			isSelected = true;
			break;
		case 0:
		case 6:
			isSelected3 = true;
			break;
		}
		PlayButton.IsSelected = isSelected;
		FastForwardButton.IsSelected = isSelected2;
		PauseButton.IsSelected = isSelected3;
	}
}
