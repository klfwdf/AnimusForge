using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.Library;

namespace NavalDLC.GauntletUI.Widgets.Widgets;

public class ShipHealthWidget : Widget
{
	public float AnimationDelay = 0.2f;

	public float AnimationDuration = 0.8f;

	private float _animationStartHealth;

	private float _smoothedCurrentAmount;

	private float _currentAmountAnimationDelta;

	private int _health;

	private int _maxHealth;

	private FillBarVerticalWidget _healthBar;

	private Widget _changeVisualWidget;

	private Widget _dividerWidget;

	private Widget _dividerVisualWidget;

	[Editor(false)]
	public int Health
	{
		get
		{
			return _health;
		}
		set
		{
			if (_health != value)
			{
				int health = _health;
				_health = value;
				OnPropertyChanged(value, "Health");
				if (_health < health)
				{
					OnHealthDrop(health);
				}
			}
		}
	}

	[Editor(false)]
	public int MaxHealth
	{
		get
		{
			return _maxHealth;
		}
		set
		{
			if (_maxHealth != value)
			{
				_maxHealth = value;
				OnPropertyChanged(value, "MaxHealth");
			}
		}
	}

	[Editor(false)]
	public FillBarVerticalWidget HealthBar
	{
		get
		{
			return _healthBar;
		}
		set
		{
			if (_healthBar != value)
			{
				_healthBar = value;
				OnPropertyChanged(value, "HealthBar");
			}
		}
	}

	[Editor(false)]
	public Widget ChangeVisualWidget
	{
		get
		{
			return _changeVisualWidget;
		}
		set
		{
			if (_changeVisualWidget != value)
			{
				_changeVisualWidget = value;
				OnPropertyChanged(value, "ChangeVisualWidget");
			}
		}
	}

	[Editor(false)]
	public Widget DividerWidget
	{
		get
		{
			return _dividerWidget;
		}
		set
		{
			if (_dividerWidget != value)
			{
				_dividerWidget = value;
				OnPropertyChanged(value, "DividerWidget");
			}
		}
	}

	[Editor(false)]
	public Widget DividerVisualWidget
	{
		get
		{
			return _dividerVisualWidget;
		}
		set
		{
			if (_dividerVisualWidget != value)
			{
				_dividerVisualWidget = value;
				OnPropertyChanged(value, "DividerVisualWidget");
			}
		}
	}

	public ShipHealthWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (HealthBar != null && base.IsVisible)
		{
			HealthBar.MaxAmount = MaxHealth;
			HealthBar.InitialAmount = Health;
			if (ChangeVisualWidget != null && HealthBar.ChangeWidget != null)
			{
				ChangeVisualWidget.PositionYOffset = 0f - HealthBar.ChangeWidget.PositionYOffset;
			}
			if (DividerWidget != null && DividerVisualWidget != null && HealthBar.FillWidget != null)
			{
				DividerWidget.PositionYOffset = DividerWidget.Size.Y * base._inverseScaleToUse - HealthBar.FillWidget.Size.Y * base._inverseScaleToUse;
				DividerVisualWidget.PositionYOffset = 0f - DividerWidget.PositionYOffset;
			}
			AnimateHealthDrop(dt);
		}
	}

	private void OnHealthDrop(int previousValue)
	{
		if (_smoothedCurrentAmount == (float)previousValue)
		{
			_animationStartHealth = previousValue;
		}
		else
		{
			_animationStartHealth = _smoothedCurrentAmount;
		}
		_currentAmountAnimationDelta = 0f;
	}

	private void AnimateHealthDrop(float dt)
	{
		if (_currentAmountAnimationDelta < AnimationDelay + AnimationDuration)
		{
			_currentAmountAnimationDelta += dt;
			float ratio = MathF.Clamp((_currentAmountAnimationDelta - AnimationDelay) / AnimationDuration, 0f, 1f);
			ratio = AnimationInterpolation.Ease(AnimationInterpolation.Type.EaseOut, AnimationInterpolation.Function.Sine, ratio);
			_smoothedCurrentAmount = MathF.Lerp(_animationStartHealth, Health, ratio);
		}
		else
		{
			_smoothedCurrentAmount = Health;
		}
		HealthBar.CurrentAmount = (int)_smoothedCurrentAmount;
	}
}
