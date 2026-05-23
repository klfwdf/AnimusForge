using System;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI;

public class CircleActionSelectorWidget : Widget
{
	private int _currentSelectedIndex;

	private const float _mouseMoveMaxDistance = 125f;

	private const float _gamepadDeadzoneLength = 0.391f;

	private const float _mouseMoveMaxDistanceSquared = 15625f;

	private float _centerDistanceAnimationTimer;

	private float _centerDistanceAnimationDuration;

	private float _centerDistanceAnimationInitialValue;

	private float _centerDistanceAnimationTarget;

	private Vec2 _mouseDirection;

	private Vec2 _mouseMoveAccumulated;

	private bool _isRefreshingSelection;

	private bool _allowInvalidSelection;

	private bool _activateOnlyWithController;

	private bool _isCircularInputDisabled;

	private float _distanceFromCenterModifier;

	private float _directionWidgetDistanceMultiplier;

	private Widget _directionWidget;

	public bool AllowInvalidSelection
	{
		get
		{
			return _allowInvalidSelection;
		}
		set
		{
			if (value != _allowInvalidSelection)
			{
				_allowInvalidSelection = value;
				OnPropertyChanged(value, "AllowInvalidSelection");
			}
		}
	}

	public bool ActivateOnlyWithController
	{
		get
		{
			return _activateOnlyWithController;
		}
		set
		{
			if (value != _activateOnlyWithController)
			{
				_activateOnlyWithController = value;
				OnPropertyChanged(value, "ActivateOnlyWithController");
			}
		}
	}

	public bool IsCircularInputEnabled
	{
		get
		{
			return !IsCircularInputDisabled;
		}
		set
		{
			if (value == IsCircularInputDisabled)
			{
				IsCircularInputDisabled = !value;
				OnPropertyChanged(!value, "IsCircularInputEnabled");
			}
		}
	}

	public bool IsCircularInputDisabled
	{
		get
		{
			return _isCircularInputDisabled;
		}
		set
		{
			if (value != _isCircularInputDisabled)
			{
				_isCircularInputDisabled = value;
				OnPropertyChanged(value, "IsCircularInputDisabled");
				if (value)
				{
					OnSelectedIndexChanged(-1);
				}
			}
		}
	}

	public float DistanceFromCenterModifier
	{
		get
		{
			return _distanceFromCenterModifier;
		}
		set
		{
			if (value != _distanceFromCenterModifier)
			{
				_distanceFromCenterModifier = value;
				OnPropertyChanged(value, "DistanceFromCenterModifier");
			}
		}
	}

	public float DirectionWidgetDistanceMultiplier
	{
		get
		{
			return _directionWidgetDistanceMultiplier;
		}
		set
		{
			if (value != _directionWidgetDistanceMultiplier)
			{
				_directionWidgetDistanceMultiplier = value;
				OnPropertyChanged(value, "DirectionWidgetDistanceMultiplier");
			}
		}
	}

	public Widget DirectionWidget
	{
		get
		{
			return _directionWidget;
		}
		set
		{
			if (value != _directionWidget)
			{
				_directionWidget = value;
				OnPropertyChanged(value, "DirectionWidget");
			}
		}
	}

	public CircleActionSelectorWidget(UIContext context)
		: base(context)
	{
		_activateOnlyWithController = false;
		_distanceFromCenterModifier = 300f;
		_directionWidgetDistanceMultiplier = 0.5f;
		_centerDistanceAnimationTimer = -1f;
		_centerDistanceAnimationDuration = -1f;
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		child.boolPropertyChanged += OnChildPropertyChanged;
	}

	private void OnChildPropertyChanged(PropertyOwnerObject widget, string propertyName, bool value)
	{
		if (propertyName == "IsSelected" && base.EventManager.IsControllerActive && !_isRefreshingSelection)
		{
			_mouseDirection = Vec2.Zero;
			_mouseMoveAccumulated = Vec2.Zero;
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!AllowInvalidSelection)
		{
			_currentSelectedIndex = -1;
		}
		if (IsRecursivelyVisible())
		{
			UpdateItemPlacement();
			AnimateDistanceFromCenter(dt);
			bool flag = IsCircularInputEnabled && (!ActivateOnlyWithController || base.EventManager.IsControllerActive);
			if (DirectionWidget != null)
			{
				DirectionWidget.IsVisible = flag;
			}
			if (flag)
			{
				UpdateAverageMouseDirection();
				UpdateCircularInput();
			}
		}
		else
		{
			if (_mouseDirection.X != 0f || _mouseDirection.Y != 0f)
			{
				_mouseDirection = default(Vec2);
			}
			if (DirectionWidget != null)
			{
				DirectionWidget.IsVisible = false;
			}
			_mouseMoveAccumulated = Vec2.Zero;
		}
	}

	private void AnimateDistanceFromCenter(float dt)
	{
		if (_centerDistanceAnimationTimer != -1f && _centerDistanceAnimationDuration != -1f && _centerDistanceAnimationTarget != -1f)
		{
			if (_centerDistanceAnimationTimer < _centerDistanceAnimationDuration)
			{
				DistanceFromCenterModifier = TaleWorlds.Library.MathF.Lerp(_centerDistanceAnimationInitialValue, _centerDistanceAnimationTarget, _centerDistanceAnimationTimer / _centerDistanceAnimationDuration);
				_centerDistanceAnimationTimer += dt;
				return;
			}
			DistanceFromCenterModifier = _centerDistanceAnimationTarget;
			_centerDistanceAnimationTimer = -1f;
			_centerDistanceAnimationDuration = -1f;
			_centerDistanceAnimationTarget = -1f;
		}
	}

	public void AnimateDistanceFromCenterTo(float distanceFromCenter, float animationDuration)
	{
		_centerDistanceAnimationTimer = 0f;
		_centerDistanceAnimationInitialValue = DistanceFromCenterModifier;
		_centerDistanceAnimationDuration = animationDuration;
		_centerDistanceAnimationTarget = distanceFromCenter;
	}

	private void UpdateAverageMouseDirection()
	{
		bool isMouseActive = base.Context.InputContext.GetIsMouseActive();
		Vector2 vector = (isMouseActive ? base.Context.InputContext.GetMouseMovement() : base.Context.InputContext.GetControllerRightStickState());
		if (isMouseActive)
		{
			_mouseMoveAccumulated += (Vec2)vector;
			if (_mouseMoveAccumulated.LengthSquared > 15625f)
			{
				_mouseMoveAccumulated.Normalize();
				_mouseMoveAccumulated *= 125f;
			}
			_mouseDirection = new Vec2(_mouseMoveAccumulated.X, 0f - _mouseMoveAccumulated.Y);
		}
		else
		{
			_mouseDirection = new Vec2(vector.X, vector.Y);
		}
	}

	private void UpdateItemPlacement()
	{
		if (base.ChildCount > 0)
		{
			int childCount = base.ChildCount;
			float num = 360f / (float)childCount;
			float num2 = 0f - num / 2f;
			if (num2 < 0f)
			{
				num2 += 360f;
			}
			for (int i = 0; i < base.ChildCount; i++)
			{
				float angle = num * (float)i;
				float angle2 = AddAngle(num2, angle);
				angle2 = AddAngle(angle2, num / 2f);
				Vec2 vec = DirFromAngle(angle2 * (System.MathF.PI / 180f));
				Widget child = GetChild(i);
				child.PositionXOffset = vec.X * DistanceFromCenterModifier;
				child.PositionYOffset = vec.Y * DistanceFromCenterModifier * -1f;
			}
		}
	}

	public bool TrySetSelectedIndex(int index)
	{
		if (index >= 0 && index < base.ChildCount)
		{
			OnSelectedIndexChanged(index);
			return true;
		}
		return false;
	}

	protected virtual void OnSelectedIndexChanged(int selectedIndex)
	{
		for (int i = 0; i < base.ChildCount; i++)
		{
			Widget child = GetChild(i);
			if (child is ButtonWidget buttonWidget)
			{
				buttonWidget.IsSelected = !child.IsDisabled && i == selectedIndex;
			}
		}
	}

	private void UpdateCircularInput()
	{
		int currentSelectedIndex = _currentSelectedIndex;
		if (_mouseDirection.Length > 0.391f)
		{
			if (base.ChildCount > 0)
			{
				float mouseDirectionAngle = AngleFromDir(_mouseDirection);
				_currentSelectedIndex = GetIndexOfSelectedItemByAngle(mouseDirectionAngle);
			}
		}
		else if (AllowInvalidSelection)
		{
			_currentSelectedIndex = -1;
		}
		if (currentSelectedIndex != _currentSelectedIndex)
		{
			_isRefreshingSelection = true;
			OnSelectedIndexChanged(_currentSelectedIndex);
			_isRefreshingSelection = false;
		}
		if (DirectionWidget != null)
		{
			if (_mouseDirection.LengthSquared > 0f)
			{
				Vec2 vec = _mouseDirection.Normalized();
				DirectionWidget.PositionXOffset = vec.X * (DistanceFromCenterModifier * DirectionWidgetDistanceMultiplier);
				DirectionWidget.PositionYOffset = (0f - vec.Y) * (DistanceFromCenterModifier * DirectionWidgetDistanceMultiplier);
			}
			else
			{
				DirectionWidget.PositionXOffset = 0f;
				DirectionWidget.PositionYOffset = 0f;
			}
		}
	}

	private int GetIndexOfSelectedItemByAngle(float mouseDirectionAngle)
	{
		int childCount = base.ChildCount;
		float num = 360f / (float)childCount;
		float num2 = 0f - num / 2f;
		if (num2 < 0f)
		{
			num2 += 360f;
		}
		for (int i = 0; i < childCount; i++)
		{
			float angle = num * (float)i;
			float angle2 = num * (float)(i + 1);
			float minAngle = AddAngle(num2, angle) * (System.MathF.PI / 180f);
			float maxAngle = AddAngle(num2, angle2) * (System.MathF.PI / 180f);
			if (IsAngleBetweenAngles(mouseDirectionAngle * (System.MathF.PI / 180f), minAngle, maxAngle))
			{
				return i;
			}
		}
		return -1;
	}

	private float AddAngle(float angle1, float angle2)
	{
		float num = angle1 + angle2;
		if (num < 0f)
		{
			num += 360f;
		}
		return num % 360f;
	}

	private bool IsAngleBetweenAngles(float angle, float minAngle, float maxAngle)
	{
		float num = angle - System.MathF.PI;
		float num2 = minAngle - System.MathF.PI;
		float num3 = maxAngle - System.MathF.PI;
		if (num2 == num3)
		{
			return true;
		}
		float num4 = TaleWorlds.Library.MathF.Abs(MBMath.GetSmallestDifferenceBetweenTwoAngles(num3, num2));
		if (num4.ApproximatelyEqualsTo(System.MathF.PI))
		{
			return num < num3;
		}
		float num5 = TaleWorlds.Library.MathF.Abs(MBMath.GetSmallestDifferenceBetweenTwoAngles(num, num2));
		float num6 = TaleWorlds.Library.MathF.Abs(MBMath.GetSmallestDifferenceBetweenTwoAngles(num, num3));
		if (num5 < num4)
		{
			return num6 < num4;
		}
		return false;
	}

	private float AngleFromDir(Vec2 directionVector)
	{
		if (directionVector.X < 0f)
		{
			return 360f - (float)Math.Atan2(directionVector.X, directionVector.Y) * 57.29578f * -1f;
		}
		return (float)Math.Atan2(directionVector.X, directionVector.Y) * 57.29578f;
	}

	private Vec2 DirFromAngle(float angle)
	{
		return new Vec2(TaleWorlds.Library.MathF.Sin(angle), TaleWorlds.Library.MathF.Cos(angle));
	}
}
