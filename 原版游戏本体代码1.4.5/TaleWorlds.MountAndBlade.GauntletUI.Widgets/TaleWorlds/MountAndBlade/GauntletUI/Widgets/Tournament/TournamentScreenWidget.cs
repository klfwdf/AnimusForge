using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Scoreboard;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Tournament;

public class TournamentScreenWidget : Widget
{
	private bool _isAnimationActive;

	private bool _isOver;

	private DelayedStateChanger _flagsSuccess;

	private DelayedStateChanger _shieldStateChanger;

	private DelayedStateChanger _winnerTextContainer1;

	private DelayedStateChanger _characterContainer;

	private DelayedStateChanger _rewardsContainer;

	private ScoreboardBattleRewardsWidget _scoreboardBattleRewardsWidget;

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
	public DelayedStateChanger WinnerTextContainer1
	{
		get
		{
			return _winnerTextContainer1;
		}
		set
		{
			if (_winnerTextContainer1 != value)
			{
				_winnerTextContainer1 = value;
				OnPropertyChanged(value, "WinnerTextContainer1");
			}
		}
	}

	[Editor(false)]
	public DelayedStateChanger CharacterContainer
	{
		get
		{
			return _characterContainer;
		}
		set
		{
			if (_characterContainer != value)
			{
				_characterContainer = value;
				OnPropertyChanged(value, "CharacterContainer");
			}
		}
	}

	[Editor(false)]
	public DelayedStateChanger RewardsContainer
	{
		get
		{
			return _rewardsContainer;
		}
		set
		{
			if (_rewardsContainer != value)
			{
				_rewardsContainer = value;
				OnPropertyChanged(value, "RewardsContainer");
			}
		}
	}

	[Editor(false)]
	public ScoreboardBattleRewardsWidget ScoreboardBattleRewardsWidget
	{
		get
		{
			return _scoreboardBattleRewardsWidget;
		}
		set
		{
			if (_scoreboardBattleRewardsWidget != value)
			{
				_scoreboardBattleRewardsWidget = value;
				OnPropertyChanged(value, "ScoreboardBattleRewardsWidget");
			}
		}
	}

	public TournamentScreenWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!_isAnimationActive && IsOver)
		{
			StartBattleResultAnimation();
		}
	}

	private void StartBattleResultAnimation()
	{
		_isAnimationActive = true;
		ShieldStateChanger?.Start();
		WinnerTextContainer1?.Start();
		CharacterContainer?.Start();
		RewardsContainer?.Start();
		FlagsSuccess?.Start();
		ScoreboardBattleRewardsWidget?.StartAnimation();
	}
}
