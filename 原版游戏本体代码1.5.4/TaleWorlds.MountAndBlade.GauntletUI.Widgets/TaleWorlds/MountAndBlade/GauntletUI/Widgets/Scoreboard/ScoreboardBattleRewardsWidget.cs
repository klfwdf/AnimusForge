using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Scoreboard;

public class ScoreboardBattleRewardsWidget : Widget
{
	private bool _isAnimationActive;

	private float _animationTimePassed;

	private int _animationLastItemIndex;

	private float _animationDelay = 1f;

	private float _animationInterval = 0.25f;

	private Widget _itemContainer;

	[Editor(false)]
	public float AnimationDelay
	{
		get
		{
			return _animationDelay;
		}
		set
		{
			if (_animationDelay != value)
			{
				_animationDelay = value;
				OnPropertyChanged(value, "AnimationDelay");
			}
		}
	}

	[Editor(false)]
	public float AnimationInterval
	{
		get
		{
			return _animationInterval;
		}
		set
		{
			if (_animationInterval != value)
			{
				_animationInterval = value;
				OnPropertyChanged(value, "AnimationInterval");
			}
		}
	}

	[Editor(false)]
	public Widget ItemContainer
	{
		get
		{
			return _itemContainer;
		}
		set
		{
			if (_itemContainer != value)
			{
				_itemContainer = value;
				OnPropertyChanged(value, "ItemContainer");
			}
		}
	}

	public ScoreboardBattleRewardsWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (_isAnimationActive)
		{
			UpdateAnimation(dt);
		}
	}

	public void StartAnimation()
	{
		_isAnimationActive = true;
		_animationTimePassed = 0f;
		_animationLastItemIndex = -1;
		ItemContainer.SetState("Opened");
		for (int i = 0; i < ItemContainer.ChildCount; i++)
		{
			Widget child = ItemContainer.GetChild(i);
			child.IsVisible = false;
			child.AddState("Opened");
		}
	}

	public void Reset()
	{
		for (int i = 0; i < ItemContainer.ChildCount; i++)
		{
			ItemContainer.GetChild(i).IsVisible = false;
		}
	}

	private void UpdateAnimation(float dt)
	{
		if (_animationTimePassed >= AnimationDelay + AnimationInterval * (float)ItemContainer.ChildCount)
		{
			return;
		}
		if (_animationTimePassed >= AnimationDelay)
		{
			int num = MathF.Floor((_animationTimePassed - AnimationDelay) / AnimationInterval);
			if (num != _animationLastItemIndex && num < ItemContainer.ChildCount)
			{
				for (int i = _animationLastItemIndex + 1; i <= num; i++)
				{
					Widget child = ItemContainer.GetChild(i);
					child.IsVisible = true;
					child.SetState("Opened");
				}
			}
		}
		_animationTimePassed += dt;
	}
}
