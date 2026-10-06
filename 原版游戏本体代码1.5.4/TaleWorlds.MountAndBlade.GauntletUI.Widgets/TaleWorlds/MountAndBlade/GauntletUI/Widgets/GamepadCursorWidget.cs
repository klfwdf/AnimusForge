using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class GamepadCursorWidget : BrushWidget
{
	private Widget _targetWidget;

	private bool _targetChangedThisFrame;

	private bool _targetPositionChangedThisFrame;

	private float _animationRatio;

	private float _animationRatioTimer;

	protected bool _isPressing;

	protected bool _areBrushesValidated;

	protected float _additionalOffset;

	protected float _additionalOffsetBeforeStateChange;

	protected float _leftOffset;

	protected float _rightOffset;

	protected float _topOffset;

	protected float _bottomOffset;

	private GamepadCursorParentWidget _cursorParentWidget;

	private GamepadCursorMarkerWidget _topLeftMarker;

	private GamepadCursorMarkerWidget _topRightMarker;

	private GamepadCursorMarkerWidget _bottomLeftMarker;

	private GamepadCursorMarkerWidget _bottomRightMarker;

	private bool _hasTarget;

	private bool _targetHasAction;

	private float _defaultOffset;

	private float _hoverOffset;

	private float _defaultTargetlessOffset;

	private float _pressOffset;

	private float _defaultSizeX;

	private float _defaultSizeY;

	private float _actionAnimationTime;

	protected float TransitionTimer { get; private set; }

	public GamepadCursorParentWidget CursorParentWidget
	{
		get
		{
			return _cursorParentWidget;
		}
		set
		{
			if (value != _cursorParentWidget)
			{
				_cursorParentWidget = value;
				OnPropertyChanged(value, "CursorParentWidget");
			}
		}
	}

	public GamepadCursorMarkerWidget TopLeftMarker
	{
		get
		{
			return _topLeftMarker;
		}
		set
		{
			if (value != _topLeftMarker)
			{
				_topLeftMarker = value;
				OnPropertyChanged(value, "TopLeftMarker");
			}
		}
	}

	public GamepadCursorMarkerWidget TopRightMarker
	{
		get
		{
			return _topRightMarker;
		}
		set
		{
			if (value != _topRightMarker)
			{
				_topRightMarker = value;
				OnPropertyChanged(value, "TopRightMarker");
			}
		}
	}

	public GamepadCursorMarkerWidget BottomLeftMarker
	{
		get
		{
			return _bottomLeftMarker;
		}
		set
		{
			if (value != _bottomLeftMarker)
			{
				_bottomLeftMarker = value;
				OnPropertyChanged(value, "BottomLeftMarker");
			}
		}
	}

	public GamepadCursorMarkerWidget BottomRightMarker
	{
		get
		{
			return _bottomRightMarker;
		}
		set
		{
			if (value != _bottomRightMarker)
			{
				_bottomRightMarker = value;
				OnPropertyChanged(value, "BottomRightMarker");
			}
		}
	}

	public bool HasTarget
	{
		get
		{
			return _hasTarget;
		}
		set
		{
			if (value != _hasTarget)
			{
				_hasTarget = value;
				OnPropertyChanged(value, "HasTarget");
				ResetAnimations();
				_animationRatioTimer = 0f;
			}
		}
	}

	public bool TargetHasAction
	{
		get
		{
			return _targetHasAction;
		}
		set
		{
			if (value != _targetHasAction)
			{
				_targetHasAction = value;
				OnPropertyChanged(value, "TargetHasAction");
				ResetAnimations();
			}
		}
	}

	public float DefaultOffset
	{
		get
		{
			return _defaultOffset;
		}
		set
		{
			if (value != _defaultOffset)
			{
				_defaultOffset = value;
				OnPropertyChanged(value, "DefaultOffset");
				ResetAnimations();
			}
		}
	}

	public float HoverOffset
	{
		get
		{
			return _hoverOffset;
		}
		set
		{
			if (value != _hoverOffset)
			{
				_hoverOffset = value;
				OnPropertyChanged(value, "HoverOffset");
				ResetAnimations();
			}
		}
	}

	public float DefaultTargetlessOffset
	{
		get
		{
			return _defaultTargetlessOffset;
		}
		set
		{
			if (value != _defaultTargetlessOffset)
			{
				_defaultTargetlessOffset = value;
				OnPropertyChanged(value, "DefaultTargetlessOffset");
				ResetAnimations();
			}
		}
	}

	public float PressOffset
	{
		get
		{
			return _pressOffset;
		}
		set
		{
			if (value != _pressOffset)
			{
				_pressOffset = value;
				OnPropertyChanged(value, "PressOffset");
				ResetAnimations();
			}
		}
	}

	public float DefaultSizeX
	{
		get
		{
			return _defaultSizeX;
		}
		set
		{
			if (value != _defaultSizeX)
			{
				_defaultSizeX = value;
				OnPropertyChanged(value, "DefaultSizeX");
				ResetAnimations();
			}
		}
	}

	public float DefaultSizeY
	{
		get
		{
			return _defaultSizeY;
		}
		set
		{
			if (value != _defaultSizeY)
			{
				_defaultSizeY = value;
				OnPropertyChanged(value, "DefaultSizeY");
				ResetAnimations();
			}
		}
	}

	public float ActionAnimationTime
	{
		get
		{
			return _actionAnimationTime;
		}
		set
		{
			if (value != _actionAnimationTime)
			{
				_actionAnimationTime = value;
				OnPropertyChanged(value, "ActionAnimationTime");
				ResetAnimations();
			}
		}
	}

	public GamepadCursorWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		if (base.IsVisible)
		{
			RefreshTarget();
			bool flag = Input.IsKeyDown(InputKey.ControllerRDown);
			if (flag != _isPressing)
			{
				_animationRatioTimer = 0f;
				TransitionTimer = 0f;
				_additionalOffsetBeforeStateChange = _additionalOffset;
			}
			_isPressing = flag;
			if (_animationRatioTimer < 1.4f)
			{
				_animationRatioTimer = MathF.Min(_animationRatioTimer + dt, 1.4f);
			}
			bool num = !_targetChangedThisFrame && _targetPositionChangedThisFrame;
			_animationRatio = ((HasTarget && !_isPressing) ? MathF.Clamp(17f * dt, 0f, 1f) : MathF.Lerp(_animationRatio, 1f, _animationRatioTimer / 1.4f));
			UpdateAdditionalOffsets();
			AnimateToTarget(_animationRatio);
			if (!num)
			{
				TransitionTimer += dt;
			}
		}
		_targetChangedThisFrame = false;
		_targetPositionChangedThisFrame = false;
	}

	private void AnimateToTarget(float ratio)
	{
		float end;
		float num3;
		float num4;
		float num;
		float num2;
		if (HasTarget)
		{
			Rectangle2D gamepadCursorAreaRect = _targetWidget.GamepadCursorAreaRect;
			num = gamepadCursorAreaRect.LocalScale.X;
			num2 = gamepadCursorAreaRect.LocalScale.Y;
			num3 = gamepadCursorAreaRect.GetCachedOrigin().X - base.EventManager.LeftUsableAreaStart;
			num4 = gamepadCursorAreaRect.GetCachedOrigin().Y - base.EventManager.TopUsableAreaStart;
			end = _targetWidget.GlobalRotation;
		}
		else
		{
			num = DefaultSizeX * base._scaleToUse;
			num2 = DefaultSizeY * base._scaleToUse;
			num3 = CursorParentWidget.XOffset - num * 0.5f;
			num4 = CursorParentWidget.YOffset - num2 * 0.5f;
			end = 0f;
		}
		num3 -= _additionalOffset;
		num4 -= _additionalOffset;
		num += _additionalOffset * 2f;
		num2 += _additionalOffset * 2f;
		base.ScaledPositionXOffset = Mathf.Lerp(base.ScaledPositionXOffset, num3, ratio);
		base.ScaledPositionYOffset = Mathf.Lerp(base.ScaledPositionYOffset, num4, ratio);
		base.ScaledSuggestedWidth = Mathf.Lerp(base.ScaledSuggestedWidth, num, ratio);
		base.ScaledSuggestedHeight = Mathf.Lerp(base.ScaledSuggestedHeight, num2, ratio);
		base.Rotation = Mathf.Lerp(base.Rotation, end, ratio);
	}

	private void RefreshTarget()
	{
		Widget widget = GauntletGamepadNavigationManager.Instance?.LastTargetedWidget;
		_targetChangedThisFrame = _targetWidget != widget;
		_targetWidget = widget;
		TargetHasAction = GauntletGamepadNavigationManager.Instance.TargetedWidgetHasAction;
		HasTarget = _targetWidget != null;
		CursorParentWidget.HasTarget = HasTarget;
	}

	private void UpdateAdditionalOffsets()
	{
		float num;
		if (TargetHasAction && !_isPressing)
		{
			float amount = (MathF.Sin(TransitionTimer / ActionAnimationTime * 1.6f) + 1f) / 2f;
			num = MathF.Lerp(DefaultOffset, HoverOffset, amount) - DefaultOffset;
		}
		else
		{
			num = 0f;
		}
		float num2 = (_isPressing ? (HasTarget ? PressOffset : (DefaultTargetlessOffset * 0.7f)) : ((!HasTarget) ? DefaultTargetlessOffset : DefaultOffset));
		_additionalOffset = (num2 + num) * base._scaleToUse;
	}

	private void ResetAnimations()
	{
		if (!_isPressing)
		{
			TransitionTimer = 0f;
			_additionalOffsetBeforeStateChange = _additionalOffset;
		}
	}
}
