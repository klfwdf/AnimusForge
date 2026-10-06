using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby;

public class MultiplayerLobbyAfterBattlePopupWidget : Widget
{
	private bool _isFinished;

	private float _timePassed;

	private int _currentRewardIndex;

	private bool _isActive;

	private float _animationDelay;

	private float _animationDuration;

	private float _rewardRevealDuration;

	private MultiplayerLobbyAfterBattleExperiencePanelWidget _experiencePanel;

	private TextWidget _clickToContinueTextWidget;

	private ListPanel _rewardsListPanel;

	[Editor(false)]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnPropertyChanged(value, "IsActive");
				IsActiveUpdated();
			}
		}
	}

	[Editor(false)]
	public float AnimationDelay
	{
		get
		{
			return _animationDelay;
		}
		set
		{
			if (value != _animationDelay)
			{
				_animationDelay = value;
				OnPropertyChanged(value, "AnimationDelay");
			}
		}
	}

	[Editor(false)]
	public float AnimationDuration
	{
		get
		{
			return _animationDuration;
		}
		set
		{
			if (value != _animationDuration)
			{
				_animationDuration = value;
				OnPropertyChanged(value, "AnimationDuration");
			}
		}
	}

	[Editor(false)]
	public float RewardRevealDuration
	{
		get
		{
			return _rewardRevealDuration;
		}
		set
		{
			if (value != _rewardRevealDuration)
			{
				_rewardRevealDuration = value;
				OnPropertyChanged(value, "RewardRevealDuration");
			}
		}
	}

	[Editor(false)]
	public MultiplayerLobbyAfterBattleExperiencePanelWidget ExperiencePanel
	{
		get
		{
			return _experiencePanel;
		}
		set
		{
			if (value != _experiencePanel)
			{
				_experiencePanel = value;
				OnPropertyChanged(value, "ExperiencePanel");
			}
		}
	}

	[Editor(false)]
	public TextWidget ClickToContinueTextWidget
	{
		get
		{
			return _clickToContinueTextWidget;
		}
		set
		{
			if (value != _clickToContinueTextWidget)
			{
				_clickToContinueTextWidget = value;
				OnPropertyChanged(value, "ClickToContinueTextWidget");
			}
		}
	}

	[Editor(false)]
	public ListPanel RewardsListPanel
	{
		get
		{
			return _rewardsListPanel;
		}
		set
		{
			if (value != _rewardsListPanel)
			{
				_rewardsListPanel = value;
				OnPropertyChanged(value, "RewardsListPanel");
			}
		}
	}

	public MultiplayerLobbyAfterBattlePopupWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		IsActive = base.IsVisible;
		if (_isActive)
		{
			_timePassed += dt;
		}
		if (!_isFinished)
		{
			if (_timePassed >= AnimationDuration + AnimationDelay + (float)_currentRewardIndex * RewardRevealDuration && _currentRewardIndex < RewardsListPanel.Children.Count)
			{
				(RewardsListPanel.Children[_currentRewardIndex] as MultiplayerLobbyBattleRewardWidget).StartAnimation();
				_currentRewardIndex++;
			}
			if (_timePassed >= AnimationDelay + AnimationDuration + (float)RewardsListPanel.Children.Count * RewardRevealDuration)
			{
				_isFinished = true;
				ClickToContinueTextWidget.IsVisible = true;
			}
		}
	}

	public void StartAnimation()
	{
		foreach (Widget child in RewardsListPanel.Children)
		{
			(child as MultiplayerLobbyBattleRewardWidget).StartPreAnimation();
		}
		_isFinished = false;
		_timePassed = 0f;
		_currentRewardIndex = 0;
		ClickToContinueTextWidget.IsVisible = false;
		ExperiencePanel.StartAnimation(AnimationDelay);
	}

	private void Reset()
	{
		ExperiencePanel.Reset();
	}

	private void IsActiveUpdated()
	{
		if (IsActive)
		{
			StartAnimation();
		}
		else
		{
			Reset();
		}
	}
}
