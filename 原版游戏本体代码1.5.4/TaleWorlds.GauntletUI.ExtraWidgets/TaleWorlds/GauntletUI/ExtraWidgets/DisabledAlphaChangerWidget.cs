using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class DisabledAlphaChangerWidget : Widget
{
	private float _animationTimer = -1f;

	private bool _latestIsDisabled;

	private bool _latestIsVisible;

	private float _fromAlpha;

	private float _disabledAlpha = 0.3f;

	private float _animationDuration = 0.25f;

	private bool _updateChildrenAlphas;

	[Editor(false)]
	public float DisabledAlpha
	{
		get
		{
			return _disabledAlpha;
		}
		set
		{
			if (value != _disabledAlpha)
			{
				_disabledAlpha = value;
				OnPropertyChanged(value, "DisabledAlpha");
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
	public bool UpdateChildrenAlphas
	{
		get
		{
			return _updateChildrenAlphas;
		}
		set
		{
			if (value != _updateChildrenAlphas)
			{
				_updateChildrenAlphas = value;
				OnPropertyChanged(value, "UpdateChildrenAlphas");
			}
		}
	}

	public DisabledAlphaChangerWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_latestIsDisabled != base.IsDisabled)
		{
			_animationTimer = 0f;
			_latestIsDisabled = base.IsDisabled;
			_fromAlpha = base.AlphaFactor;
		}
		if (_latestIsVisible != base.IsVisible)
		{
			_animationTimer = AnimationDuration;
			_latestIsVisible = base.IsVisible;
		}
		float num = (base.IsDisabled ? DisabledAlpha : 1f);
		if (_animationTimer >= 0f && _animationTimer < AnimationDuration)
		{
			num = MathF.Lerp(_fromAlpha, num, _animationTimer / AnimationDuration);
			_animationTimer += dt;
		}
		if (UpdateChildrenAlphas)
		{
			this.SetGlobalAlphaRecursively(num);
		}
		else
		{
			this.SetAlpha(num);
		}
	}
}
