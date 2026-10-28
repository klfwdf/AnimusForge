using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Scoreboard;

public class ScoreboardScreenWidget : Widget
{
	private bool _isAnimationActive;

	private bool _showScoreboard;

	private bool _isOver;

	private int _battleResult;

	private bool _isMainCharacterDead;

	private bool _isSimulation;

	private bool _isMouseEnabled;

	private ScrollablePanel _scrollablePanel;

	private Widget _scrollGradient;

	private Widget _controlButtonsPanel;

	private Widget _showMouseIconWidget;

	private ListPanel _inputKeysPanel;

	private Widget _fastForwardWidget;

	private ButtonWidget _showScoreboardToggle;

	private ButtonWidget _quitButton;

	private ScoreboardBattleRewardsWidget _battleRewardsWidget;

	private DelayedStateChanger _flagsSuccess;

	private DelayedStateChanger _flagsDefeat;

	private DelayedStateChanger _flagsRetreat;

	private DelayedStateChanger _shieldStateChanger;

	private DelayedStateChanger _shipsStateChanger;

	private DelayedStateChanger _titleStateChanger;

	private DelayedStateChanger _titleBackgroundStateChanger;

	[Editor(false)]
	public bool ShowScoreboard
	{
		get
		{
			return _showScoreboard;
		}
		set
		{
			if (_showScoreboard != value)
			{
				_showScoreboard = value;
				OnPropertyChanged(value, "ShowScoreboard");
				UpdateControlButtonsPanel();
				UpdateControlButtons();
			}
		}
	}

	[Editor(false)]
	public bool IsOver
	{
		get
		{
			return _isOver;
		}
		set
		{
			if (_isOver != value)
			{
				_isOver = value;
				OnPropertyChanged(value, "IsOver");
				UpdateControlButtons();
			}
		}
	}

	[Editor(false)]
	public int BattleResult
	{
		get
		{
			return _battleResult;
		}
		set
		{
			if (_battleResult != value)
			{
				_battleResult = value;
				OnPropertyChanged(value, "BattleResult");
			}
		}
	}

	[Editor(false)]
	public bool IsMainCharacterDead
	{
		get
		{
			return _isMainCharacterDead;
		}
		set
		{
			if (_isMainCharacterDead != value)
			{
				_isMainCharacterDead = value;
				OnPropertyChanged(value, "IsMainCharacterDead");
				UpdateControlButtonsPanel();
				UpdateControlButtons();
			}
		}
	}

	[Editor(false)]
	public bool IsSimulation
	{
		get
		{
			return _isSimulation;
		}
		set
		{
			if (_isSimulation != value)
			{
				_isSimulation = value;
				OnPropertyChanged(value, "IsSimulation");
				UpdateControlButtonsPanel();
				UpdateControlButtons();
			}
		}
	}

	[Editor(false)]
	public bool IsMouseEnabled
	{
		get
		{
			return _isMouseEnabled;
		}
		set
		{
			if (_isMouseEnabled != value)
			{
				_isMouseEnabled = value;
				OnPropertyChanged(value, "IsMouseEnabled");
				UpdateControlButtonsPanel();
			}
		}
	}

	[Editor(false)]
	public ScrollablePanel ScrollablePanel
	{
		get
		{
			return _scrollablePanel;
		}
		set
		{
			if (_scrollablePanel != value)
			{
				_scrollablePanel = value;
				OnPropertyChanged(value, "ScrollablePanel");
			}
		}
	}

	[Editor(false)]
	public Widget ScrollGradient
	{
		get
		{
			return _scrollGradient;
		}
		set
		{
			if (_scrollGradient != value)
			{
				_scrollGradient = value;
				OnPropertyChanged(value, "ScrollGradient");
			}
		}
	}

	[Editor(false)]
	public Widget ControlButtonsPanel
	{
		get
		{
			return _controlButtonsPanel;
		}
		set
		{
			if (_controlButtonsPanel != value)
			{
				_controlButtonsPanel = value;
				OnPropertyChanged(value, "ControlButtonsPanel");
			}
		}
	}

	[Editor(false)]
	public ListPanel InputKeysPanel
	{
		get
		{
			return _inputKeysPanel;
		}
		set
		{
			if (_inputKeysPanel != value)
			{
				_inputKeysPanel = value;
				OnPropertyChanged(value, "InputKeysPanel");
			}
		}
	}

	[Editor(false)]
	public Widget ShowMouseIconWidget
	{
		get
		{
			return _showMouseIconWidget;
		}
		set
		{
			if (_showMouseIconWidget != value)
			{
				_showMouseIconWidget = value;
				OnPropertyChanged(value, "ShowMouseIconWidget");
			}
		}
	}

	[Editor(false)]
	public Widget FastForwardWidget
	{
		get
		{
			return _fastForwardWidget;
		}
		set
		{
			if (_fastForwardWidget != value)
			{
				_fastForwardWidget = value;
				OnPropertyChanged(value, "FastForwardWidget");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget QuitButton
	{
		get
		{
			return _quitButton;
		}
		set
		{
			if (_quitButton != value)
			{
				_quitButton = value;
				OnPropertyChanged(value, "QuitButton");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget ShowScoreboardToggle
	{
		get
		{
			return _showScoreboardToggle;
		}
		set
		{
			if (_showScoreboardToggle != value)
			{
				_showScoreboardToggle = value;
				OnPropertyChanged(value, "ShowScoreboardToggle");
			}
		}
	}

	[Editor(false)]
	public ScoreboardBattleRewardsWidget BattleRewardsWidget
	{
		get
		{
			return _battleRewardsWidget;
		}
		set
		{
			if (_battleRewardsWidget != value)
			{
				_battleRewardsWidget = value;
				OnPropertyChanged(value, "BattleRewardsWidget");
			}
		}
	}

	[Editor(false)]
	public DelayedStateChanger FlagsSuccess
	{
		get
		{
			return _flagsSuccess;
		}
		set
		{
			if (_flagsSuccess != value)
			{
				_flagsSuccess = value;
				OnPropertyChanged(value, "FlagsSuccess");
			}
		}
	}

	[Editor(false)]
	public DelayedStateChanger FlagsRetreat
	{
		get
		{
			return _flagsRetreat;
		}
		set
		{
			if (_flagsRetreat != value)
			{
				_flagsRetreat = value;
				OnPropertyChanged(value, "FlagsRetreat");
			}
		}
	}

	[Editor(false)]
	public DelayedStateChanger FlagsDefeat
	{
		get
		{
			return _flagsDefeat;
		}
		set
		{
			if (_flagsDefeat != value)
			{
				_flagsDefeat = value;
				OnPropertyChanged(value, "FlagsDefeat");
			}
		}
	}

	[Editor(false)]
	public DelayedStateChanger ShieldStateChanger
	{
		get
		{
			return _shieldStateChanger;
		}
		set
		{
			if (_shieldStateChanger != value)
			{
				_shieldStateChanger = value;
				OnPropertyChanged(value, "ShieldStateChanger");
			}
		}
	}

	[Editor(false)]
	public DelayedStateChanger ShipsStateChanger
	{
		get
		{
			return _shipsStateChanger;
		}
		set
		{
			if (_shipsStateChanger != value)
			{
				_shipsStateChanger = value;
				OnPropertyChanged(value, "ShipsStateChanger");
			}
		}
	}

	[Editor(false)]
	public DelayedStateChanger TitleStateChanger
	{
		get
		{
			return _titleStateChanger;
		}
		set
		{
			if (_titleStateChanger != value)
			{
				_titleStateChanger = value;
				OnPropertyChanged(value, "TitleStateChanger");
			}
		}
	}

	[Editor(false)]
	public DelayedStateChanger TitleBackgroundStateChanger
	{
		get
		{
			return _titleBackgroundStateChanger;
		}
		set
		{
			if (_titleBackgroundStateChanger != value)
			{
				_titleBackgroundStateChanger = value;
				OnPropertyChanged(value, "TitleBackgroundStateChanger");
			}
		}
	}

	public ScoreboardScreenWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (ScrollablePanel != null && ScrollGradient != null)
		{
			ScrollGradient.IsVisible = ScrollablePanel.InnerPanel.Size.Y > ScrollablePanel.ClipRect.Size.Y;
		}
		if (!_isAnimationActive && ShowScoreboard && IsOver)
		{
			StartBattleResultAnimation();
		}
	}

	private void UpdateControlButtonsPanel()
	{
		_controlButtonsPanel.IsVisible = ShowScoreboard || IsMainCharacterDead;
		InputKeysPanel.IsVisible = !ShowScoreboard && !IsSimulation && IsMainCharacterDead;
		ShowMouseIconWidget.IsVisible = !IsMouseEnabled && ShowScoreboard && !IsSimulation && !IsMainCharacterDead;
	}

	private void UpdateControlButtons()
	{
		_fastForwardWidget.IsVisible = (IsMainCharacterDead || IsSimulation) && ShowScoreboard;
		_quitButton.IsVisible = ShowScoreboard;
	}

	private void StartBattleResultAnimation()
	{
		_isAnimationActive = true;
		BattleRewardsWidget?.StartAnimation();
		ShieldStateChanger?.Start();
		ShipsStateChanger?.Start();
		TitleStateChanger?.Start();
		TitleBackgroundStateChanger?.Start();
		if (BattleResult == 0)
		{
			if (FlagsDefeat != null)
			{
				FlagsDefeat.IsVisible = true;
				FlagsDefeat.Start();
			}
		}
		else if (BattleResult == 1)
		{
			if (FlagsSuccess != null)
			{
				FlagsSuccess.IsVisible = true;
				FlagsSuccess.Start();
			}
		}
		else if (BattleResult == 2 && FlagsRetreat != null)
		{
			FlagsRetreat.IsVisible = true;
			FlagsRetreat.Start();
		}
	}
}
