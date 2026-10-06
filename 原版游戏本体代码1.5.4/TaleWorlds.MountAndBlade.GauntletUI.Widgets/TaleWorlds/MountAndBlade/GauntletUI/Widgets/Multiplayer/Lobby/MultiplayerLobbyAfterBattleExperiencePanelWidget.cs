using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Scoreboard;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby;

public class MultiplayerLobbyAfterBattleExperiencePanelWidget : Widget
{
	private int _gainedExperience;

	private MultiplayerScoreboardAnimatedFillBarWidget _experienceFillBar;

	private CounterTextBrushWidget _earnedExperienceCounterTextWidget;

	private TextWidget _currentLevelTextWidget;

	private TextWidget _nextLevelTextWidget;

	[Editor(false)]
	public int GainedExperience
	{
		get
		{
			return _gainedExperience;
		}
		set
		{
			if (value != _gainedExperience)
			{
				_gainedExperience = value;
				OnPropertyChanged(value, "GainedExperience");
			}
		}
	}

	[Editor(false)]
	public MultiplayerScoreboardAnimatedFillBarWidget ExperienceFillBar
	{
		get
		{
			return _experienceFillBar;
		}
		set
		{
			if (value != _experienceFillBar)
			{
				if (_experienceFillBar != null)
				{
					_experienceFillBar.OnFullFillFinished -= OnFillBarFill;
				}
				_experienceFillBar = value;
				if (_experienceFillBar != null)
				{
					_experienceFillBar.OnFullFillFinished += OnFillBarFill;
				}
				OnPropertyChanged(value, "ExperienceFillBar");
				Reset();
			}
		}
	}

	[Editor(false)]
	public CounterTextBrushWidget EarnedExperienceCounterTextWidget
	{
		get
		{
			return _earnedExperienceCounterTextWidget;
		}
		set
		{
			if (value != _earnedExperienceCounterTextWidget)
			{
				_earnedExperienceCounterTextWidget = value;
				OnPropertyChanged(value, "EarnedExperienceCounterTextWidget");
				Reset();
			}
		}
	}

	[Editor(false)]
	public TextWidget CurrentLevelTextWidget
	{
		get
		{
			return _currentLevelTextWidget;
		}
		set
		{
			if (value != _currentLevelTextWidget)
			{
				_currentLevelTextWidget = value;
				OnPropertyChanged(value, "CurrentLevelTextWidget");
			}
		}
	}

	public TextWidget NextLevelTextWidget
	{
		get
		{
			return _nextLevelTextWidget;
		}
		set
		{
			if (value != _nextLevelTextWidget)
			{
				_nextLevelTextWidget = value;
				OnPropertyChanged(value, "NextLevelTextWidget");
			}
		}
	}

	public MultiplayerLobbyAfterBattleExperiencePanelWidget(UIContext context)
		: base(context)
	{
	}

	public void StartAnimation(float animationDelay)
	{
		ExperienceFillBar.StartAnimation(animationDelay);
		EarnedExperienceCounterTextWidget.IntTarget = GainedExperience;
	}

	public void Reset()
	{
		ExperienceFillBar?.Reset();
		EarnedExperienceCounterTextWidget?.SetInitialValue(0f);
	}

	private void OnFillBarFill(bool isPositive)
	{
		CurrentLevelTextWidget.IntText += (isPositive ? 1 : (-1));
		NextLevelTextWidget.IntText += (isPositive ? 1 : (-1));
	}

	protected override void RefreshState()
	{
		if (base.IsHidden)
		{
			ExperienceFillBar?.Reset();
		}
	}
}
