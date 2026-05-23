using System.Numerics;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes;

public class SliderWidget : ImageWidget
{
	private bool _firstFrame;

	public float HandleRatio;

	protected bool _handleClicked;

	protected bool _valueChangedByMouse;

	private float _downStartTime = -1f;

	private Vector2 _handleClickOffset;

	private bool _snapCursorToHandle;

	private bool _locked;

	private bool _isDiscrete;

	private bool _updateValueOnRelease;

	private Vector2 _localClickPos;

	private float _valueFloat;

	private Widget _handle;

	public bool UpdateValueOnScroll { get; set; }

	private float _holdTimeToStartMovement => 0.3f;

	private float _dynamicIncrement
	{
		get
		{
			if (!(MaxValueFloat - MinValueFloat > 2f))
			{
				return 0.1f;
			}
			return 1f;
		}
	}

	[Editor(false)]
	public bool IsDiscrete
	{
		get
		{
			return _isDiscrete;
		}
		set
		{
			if (_isDiscrete != value)
			{
				_isDiscrete = value;
				OnPropertyChanged(_isDiscrete, "IsDiscrete");
			}
		}
	}

	[Editor(false)]
	public bool Locked
	{
		get
		{
			return _locked;
		}
		set
		{
			if (_locked != value)
			{
				_locked = value;
				OnPropertyChanged(_locked, "Locked");
			}
		}
	}

	[Editor(false)]
	public bool UpdateValueOnRelease
	{
		get
		{
			return _updateValueOnRelease;
		}
		set
		{
			if (_updateValueOnRelease != value)
			{
				_updateValueOnRelease = value;
				OnPropertyChanged(_updateValueOnRelease, "UpdateValueOnRelease");
			}
		}
	}

	[Editor(false)]
	public bool UpdateValueContinuously
	{
		get
		{
			return !_updateValueOnRelease;
		}
		set
		{
			if (UpdateValueContinuously != value)
			{
				_updateValueOnRelease = !value;
				OnPropertyChanged(_updateValueOnRelease, "UpdateValueContinuously");
			}
		}
	}

	[Editor(false)]
	public AlignmentAxis AlignmentAxis { get; set; }

	[Editor(false)]
	public bool ReverseDirection { get; set; }

	[Editor(false)]
	public Widget Filler { get; set; }

	[Editor(false)]
	public Widget HandleExtension { get; set; }

	[Editor(false)]
	public float ValueFloat
	{
		get
		{
			return _valueFloat;
		}
		set
		{
			if (Locked || !(MathF.Abs(_valueFloat - value) > 1E-05f))
			{
				return;
			}
			float valueFloat = _valueFloat;
			if (!(MinValueFloat <= MaxValueFloat))
			{
				return;
			}
			if (_valueFloat < MinValueFloat)
			{
				_valueFloat = MinValueFloat;
			}
			if (_valueFloat > MaxValueFloat)
			{
				_valueFloat = MaxValueFloat;
			}
			if (IsDiscrete)
			{
				if (value >= (float)MaxValueInt)
				{
					_valueFloat = MaxValueInt;
				}
				else
				{
					float num = Mathf.Floor((value - (float)MinValueInt) / (float)DiscreteIncrementInterval);
					_valueFloat = num * (float)DiscreteIncrementInterval + (float)MinValueInt;
				}
			}
			else
			{
				_valueFloat = value;
			}
			UpdateHandleByValue();
			if (MathF.Abs(_valueFloat - valueFloat) > 1E-05f && ((UpdateValueOnRelease && !base.IsPressed) || !UpdateValueOnRelease))
			{
				OnPropertyChanged(_valueFloat, "ValueFloat");
				OnPropertyChanged(ValueInt, "ValueInt");
				OnValueFloatChanged(_valueFloat);
			}
		}
	}

	[Editor(false)]
	public int ValueInt
	{
		get
		{
			return MathF.Round(ValueFloat);
		}
		set
		{
			ValueFloat = value;
			OnValueIntChanged(ValueInt);
		}
	}

	[Editor(false)]
	public float MinValueFloat { get; set; }

	[Editor(false)]
	public float MaxValueFloat { get; set; }

	[Editor(false)]
	public int MinValueInt
	{
		get
		{
			return MathF.Round(MinValueFloat);
		}
		set
		{
			MinValueFloat = value;
		}
	}

	[Editor(false)]
	public int MaxValueInt
	{
		get
		{
			return MathF.Round(MaxValueFloat);
		}
		set
		{
			MaxValueFloat = value;
		}
	}

	public int DiscreteIncrementInterval { get; set; } = 1;

	[Editor(false)]
	public bool DoNotUpdateHandleSize { get; set; }

	[Editor(false)]
	public Widget Handle
	{
		get
		{
			return _handle;
		}
		set
		{
			if (_handle != value)
			{
				_handle = value;
				UpdateHandleByValue();
				if (_handle != null)
				{
					_handle.ExtendCursorAreaLeft = 6f;
					_handle.ExtendCursorAreaRight = 6f;
					_handle.ExtendCursorAreaTop = 3f;
					_handle.ExtendCursorAreaBottom = 3f;
				}
			}
		}
	}

	[Editor(false)]
	public Widget SliderArea { get; set; }

	public SliderWidget(UIContext context)
		: base(context)
	{
		SliderArea = this;
		_firstFrame = true;
		base.FrictionEnabled = true;
		base.UsedNavigationMovements = GamepadNavigationTypes.Horizontal;
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		bool flag = false;
		base.IsUsingNavigation = false;
		if (!base.IsPressed)
		{
			Widget handle = Handle;
			if (handle == null || !handle.IsPressed)
			{
				Widget handleExtension = HandleExtension;
				if (handleExtension == null || !handleExtension.IsPressed)
				{
					_downStartTime = -1f;
					_handleClickOffset = Vector2.Zero;
					_handleClicked = false;
					_valueChangedByMouse = false;
					goto IL_01c0;
				}
			}
		}
		if (base.EventManager.IsControllerActive && IsRecursivelyVisible() && base.EventManager.GetIsHitThisFrame())
		{
			float num = 0f;
			if (Input.IsKeyDown(InputKey.ControllerLLeft))
			{
				num = -1f;
			}
			else if (Input.IsKeyDown(InputKey.ControllerLRight))
			{
				num = 1f;
			}
			if (num != 0f)
			{
				num *= (IsDiscrete ? ((float)DiscreteIncrementInterval) : _dynamicIncrement);
				if (_downStartTime == -1f)
				{
					_downStartTime = base.Context.EventManager.Time;
					ValueFloat = MathF.Clamp(_valueFloat + num, MinValueFloat, MaxValueFloat);
					flag = true;
				}
				else if (_holdTimeToStartMovement < base.Context.EventManager.Time - _downStartTime)
				{
					ValueFloat = MathF.Clamp(_valueFloat + num, MinValueFloat, MaxValueFloat);
					flag = true;
				}
			}
			else
			{
				_downStartTime = -1f;
			}
			base.IsUsingNavigation = true;
		}
		if (!_handleClicked)
		{
			_handleClicked = true;
			UpdateLocalClickPosition();
			_handleClickOffset = base.EventManager.MousePosition - Handle.AreaRect.GetCenter();
		}
		HandleMouseMove();
		goto IL_01c0;
		IL_01c0:
		UpdateScrollBar();
		UpdateHandleLength();
		Handle?.SetState(base.CurrentState);
		if (_snapCursorToHandle)
		{
			Vector2 center = Handle.AreaRect.GetCenter();
			Input.SetMousePosition((int)center.X, (int)center.Y);
			_snapCursorToHandle = false;
		}
		if (flag && Input.MouseMoveX == 0f && Input.MouseMoveY == 0f)
		{
			_snapCursorToHandle = true;
		}
		_firstFrame = false;
	}

	protected override void OnParallelUpdate(float dt)
	{
		base.OnParallelUpdate(dt);
		if (Filler != null)
		{
			float num = 1f;
			if (MathF.Abs(MaxValueFloat - MinValueFloat) > float.Epsilon)
			{
				num = (_valueFloat - MinValueFloat) / (MaxValueFloat - MinValueFloat);
			}
			Filler.HorizontalAlignment = HorizontalAlignment.Left;
			if (AlignmentAxis == AlignmentAxis.Horizontal)
			{
				Filler.WidthSizePolicy = SizePolicy.Fixed;
				Filler.ScaledSuggestedWidth = SliderArea.Size.X * num;
			}
			else
			{
				Filler.HeightSizePolicy = SizePolicy.Fixed;
				Filler.ScaledSuggestedHeight = SliderArea.Size.Y * num;
			}
			Filler.DoNotAcceptEvents = true;
			Filler.DoNotPassEventsToChildren = true;
		}
	}

	protected internal override void OnMousePressed()
	{
		if (Handle != null && Handle.IsVisible)
		{
			base.IsPressed = true;
			EventFired("MousePressed");
			UpdateLocalClickPosition();
			OnPropertyChanged("MouseDown", "OnMousePressed");
			HandleMouseMove();
		}
	}

	protected internal override void OnMouseReleased(bool isFromInput)
	{
		if (Handle != null)
		{
			base.IsPressed = false;
			EventFired("MouseReleased");
			if (UpdateValueOnRelease)
			{
				OnPropertyChanged(_valueFloat, "ValueFloat");
				OnPropertyChanged(ValueInt, "ValueInt");
				OnValueFloatChanged(_valueFloat);
			}
		}
	}

	protected internal override void OnMouseMove()
	{
		base.OnMouseMove();
		if (base.IsPressed)
		{
			HandleMouseMove();
		}
	}

	protected internal virtual void OnValueIntChanged(int value)
	{
	}

	protected internal virtual void OnValueFloatChanged(float value)
	{
	}

	private void UpdateScrollBar()
	{
		if (!_firstFrame && base.IsVisible)
		{
			UpdateHandleByValue();
		}
	}

	private void UpdateLocalClickPosition()
	{
		Vector2 screenPosition = base.EventManager.MousePosition;
		_localClickPos = Handle.AreaRect.TransformScreenPositionToLocal(in screenPosition);
		if (_localClickPos.X < 0f || _localClickPos.X > Handle.Size.X)
		{
			_localClickPos.X = Handle.Size.X / 2f;
		}
		if (_localClickPos.Y < -5f)
		{
			_localClickPos.Y = -5f;
		}
		else if (_localClickPos.Y > Handle.Size.Y + 5f)
		{
			_localClickPos.Y = Handle.Size.Y + 5f;
		}
	}

	private void HandleMouseMove()
	{
		if ((base.EventManager.IsControllerActive && Input.MouseMoveX == 0f && Input.MouseMoveY == 0f) || Handle == null)
		{
			return;
		}
		Vector2 value = base.EventManager.MousePosition - _localClickPos;
		float num = GetValue(value, AlignmentAxis);
		float num2;
		float num3;
		if (AlignmentAxis == AlignmentAxis.Horizontal)
		{
			float x = base.ParentWidget.GlobalPosition.X;
			num2 = x + base.Left;
			num3 = x + base.Right;
			num3 -= Handle.Size.X;
			Widget handleExtension = HandleExtension;
			if (handleExtension != null && handleExtension.IsPressed)
			{
				num -= _handleClickOffset.X;
			}
		}
		else
		{
			float y = base.ParentWidget.GlobalPosition.Y;
			num2 = y + base.Top;
			num3 = y + base.Bottom;
			num3 -= Handle.Size.Y;
		}
		if (Mathf.Abs(num3 - num2) < 1E-05f)
		{
			ValueFloat = 0f;
			return;
		}
		if (num < num2)
		{
			num = num2;
		}
		if (num > num3)
		{
			num = num3;
		}
		float num4 = (num - num2) / (num3 - num2);
		_valueChangedByMouse = true;
		ValueFloat = MinValueFloat + (MaxValueFloat - MinValueFloat) * num4;
	}

	private void UpdateHandleByValue()
	{
		if (_valueFloat < MinValueFloat)
		{
			ValueFloat = MinValueFloat;
		}
		if (_valueFloat > MaxValueFloat)
		{
			ValueFloat = MaxValueFloat;
		}
		float num = 1f;
		if (MathF.Abs(MaxValueFloat - MinValueFloat) > float.Epsilon)
		{
			num = (_valueFloat - MinValueFloat) / (MaxValueFloat - MinValueFloat);
			if (ReverseDirection)
			{
				num = 1f - num;
			}
		}
		if (Handle != null)
		{
			if (AlignmentAxis == AlignmentAxis.Horizontal)
			{
				Handle.HorizontalAlignment = HorizontalAlignment.Left;
				Handle.VerticalAlignment = VerticalAlignment.Center;
				float x = SliderArea.Size.X;
				x -= Handle.Size.X;
				Handle.ScaledPositionXOffset = x * num;
				Handle.ScaledPositionYOffset = 0f;
			}
			else
			{
				Handle.HorizontalAlignment = HorizontalAlignment.Center;
				Handle.VerticalAlignment = VerticalAlignment.Bottom;
				float y = SliderArea.Size.Y;
				y -= Handle.Size.Y;
				Handle.ScaledPositionYOffset = -1f * y * (1f - num);
				Handle.ScaledPositionXOffset = 0f;
			}
			if (HandleExtension != null)
			{
				HandleExtension.HorizontalAlignment = Handle.HorizontalAlignment;
				HandleExtension.VerticalAlignment = Handle.VerticalAlignment;
				HandleExtension.ScaledPositionXOffset = Handle.ScaledPositionXOffset;
				HandleExtension.ScaledPositionYOffset = Handle.ScaledPositionYOffset;
			}
		}
	}

	private void UpdateHandleLength()
	{
		if (Handle != null && !DoNotUpdateHandleSize && IsDiscrete && Handle.WidthSizePolicy == SizePolicy.Fixed)
		{
			if (AlignmentAxis == AlignmentAxis.Horizontal)
			{
				Handle.SuggestedWidth = Mathf.Clamp(base.SuggestedWidth / (MaxValueFloat + 1f), 50f, base.SuggestedWidth / 2f);
			}
			else if (AlignmentAxis == AlignmentAxis.Vertical)
			{
				Handle.SuggestedHeight = Mathf.Clamp(base.SuggestedHeight / (MaxValueFloat + 1f), 50f, base.SuggestedHeight / 2f);
			}
		}
	}

	private float GetValue(Vector2 value, AlignmentAxis alignmentAxis)
	{
		if (alignmentAxis == AlignmentAxis.Horizontal)
		{
			return value.X;
		}
		return value.Y;
	}

	protected override bool OnPreviewMouseScroll()
	{
		if (UpdateValueOnScroll)
		{
			float num = base.EventManager.DeltaMouseScroll * 0.004f;
			ValueFloat = MathF.Clamp(_valueFloat + _dynamicIncrement * num, MinValueFloat, MaxValueFloat);
		}
		return base.OnPreviewMouseScroll();
	}
}
