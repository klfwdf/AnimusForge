using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace NavalDLC.GauntletUI.Widgets.Widgets;

public class PortUpgradesPanelParentWidget : Widget
{
	private bool _isFirstFrame = true;

	private float _visibilityAnimationTimer;

	private float _fullMarginLeft;

	private bool _visibilityCondition;

	private float _visibilityAnimationDuration;

	[Editor(false)]
	public bool VisibilityCondition
	{
		get
		{
			return _visibilityCondition;
		}
		set
		{
			if (value != _visibilityCondition)
			{
				_visibilityCondition = value;
				OnPropertyChanged(value, "VisibilityCondition");
			}
		}
	}

	[Editor(false)]
	public float VisibilityAnimationDuration
	{
		get
		{
			return _visibilityAnimationDuration;
		}
		set
		{
			if (value != _visibilityAnimationDuration)
			{
				_visibilityAnimationDuration = value;
				OnPropertyChanged(value, "VisibilityAnimationDuration");
			}
		}
	}

	public PortUpgradesPanelParentWidget(UIContext context)
		: base(context)
	{
		base.IsVisible = false;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isFirstFrame)
		{
			_fullMarginLeft = base.MarginLeft;
			_isFirstFrame = false;
		}
		if (VisibilityCondition)
		{
			base.IsVisible = true;
			if (_visibilityAnimationTimer < _visibilityAnimationDuration)
			{
				float ratio = AnimationInterpolation.Ease(AnimationInterpolation.Type.EaseInOut, AnimationInterpolation.Function.Quint, MathF.Clamp(_visibilityAnimationTimer / _visibilityAnimationDuration, 0f, 1f));
				UpdateAnimation(ratio);
				_visibilityAnimationTimer += dt;
			}
			else
			{
				_visibilityAnimationTimer = _visibilityAnimationDuration;
				UpdateAnimation(1f);
			}
		}
		else if (_visibilityAnimationTimer > 0f)
		{
			float ratio2 = AnimationInterpolation.Ease(AnimationInterpolation.Type.EaseInOut, AnimationInterpolation.Function.Quint, MathF.Clamp(_visibilityAnimationTimer / _visibilityAnimationDuration, 0f, 1f));
			UpdateAnimation(ratio2);
			_visibilityAnimationTimer -= dt;
		}
		else
		{
			_visibilityAnimationTimer = 0f;
			UpdateAnimation(0f);
			base.IsVisible = false;
		}
	}

	private void UpdateAnimation(float ratio)
	{
		base.MarginLeft = MathF.Lerp(_fullMarginLeft / 2f, _fullMarginLeft, ratio);
		this.SetGlobalAlphaRecursively(ratio * base.ParentWidget.AlphaFactor);
	}
}
