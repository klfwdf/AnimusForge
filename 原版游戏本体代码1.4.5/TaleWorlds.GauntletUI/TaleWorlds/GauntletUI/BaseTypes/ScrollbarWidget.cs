using System.Numerics;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes;

public class ScrollbarWidget : ImageWidget
{
	private bool _locked;

	private bool _isDiscrete;

	private float _valueFloat;

	public float HandleRatio;

	private Widget _handle;

	private bool _firstFrame;

	private Vector2 _localClickPos;

	private bool _handleClicked;

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
	public AlignmentAxis AlignmentAxis { get; set; }

	[Editor(false)]
	public bool ReverseDirection { get; set; }

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
			if (MinValue <= MaxValue)
			{
				if (_valueFloat < MinValue)
				{
					_valueFloat = MinValue;
				}
				if (_valueFloat > MaxValue)
				{
					_valueFloat = MaxValue;
				}
				if (IsDiscrete)
				{
					_valueFloat = MathF.Round(value);
				}
				else
				{
					_valueFloat = value;
				}
				UpdateHandleByValue();
				if (MathF.Abs(_valueFloat - valueFloat) > 1E-05f)
				{
					OnPropertyChanged(_valueFloat, "ValueFloat");
					OnPropertyChanged(ValueInt, "ValueInt");
				}
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
		}
	}

	[Editor(false)]
	public float MinValue { get; set; }

	[Editor(false)]
	public float MaxValue { get; set; }

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
			}
		}
	}

	[Editor(false)]
	public Widget ScrollbarArea { get; set; }

	public ScrollbarWidget(UIContext context)
		: base(context)
	{
		ScrollbarArea = this;
		_firstFrame = true;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (Handle.IsPressed)
		{
			if (!_handleClicked)
			{
				_handleClicked = true;
				_localClickPos = Handle.AreaRect.TransformScreenPositionToLocal(base.EventManager.MousePosition);
			}
			HandleMouseMove();
		}
		else
		{
			_handleClicked = false;
		}
		UpdateScrollBar();
		UpdateHandleLength();
		_firstFrame = false;
	}

	protected internal override void OnMousePressed()
	{
		if (Handle != null)
		{
			base.IsPressed = true;
			Vector2 screenPosition = base.EventManager.MousePosition;
			_localClickPos = Handle.AreaRect.TransformScreenPositionToLocal(in screenPosition);
			if (_localClickPos.X < -5f)
			{
				_localClickPos.X = -5f;
			}
			else if (_localClickPos.X > Handle.Size.X + 5f)
			{
				_localClickPos.X = Handle.Size.X + 5f;
			}
			if (_localClickPos.Y < -5f)
			{
				_localClickPos.Y = -5f;
			}
			else if (_localClickPos.Y > Handle.Size.Y + 5f)
			{
				_localClickPos.Y = Handle.Size.Y + 5f;
			}
			HandleMouseMove();
		}
	}

	protected internal override void OnMouseReleased(bool isFromInput)
	{
		if (Handle != null)
		{
			base.IsPressed = false;
		}
	}

	public void SetValueForced(float value)
	{
		if (value > MaxValue)
		{
			MaxValue = value;
		}
		else if (value < MinValue)
		{
			MinValue = value;
		}
		ValueFloat = value;
	}

	private void UpdateScrollBar()
	{
		if (!_firstFrame)
		{
			UpdateHandleByValue();
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

	private void HandleMouseMove()
	{
		if (Handle == null)
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
		}
		else
		{
			if (num < num2)
			{
				num = num2;
			}
			if (num > num3)
			{
				num = num3;
			}
			float num4 = (num - num2) / (num3 - num2);
			ValueFloat = MinValue + (MaxValue - MinValue) * num4;
		}
		UpdateHandleByValue();
	}

	private void UpdateHandleLength()
	{
		if (!DoNotUpdateHandleSize && IsDiscrete && Handle.WidthSizePolicy == SizePolicy.Fixed)
		{
			if (AlignmentAxis == AlignmentAxis.Horizontal)
			{
				Handle.SuggestedWidth = Mathf.Clamp(base.SuggestedWidth / (MaxValue + 1f), 50f, base.SuggestedWidth / 2f);
			}
			else if (AlignmentAxis == AlignmentAxis.Vertical)
			{
				Handle.SuggestedHeight = Mathf.Clamp(base.SuggestedHeight / (MaxValue + 1f), 50f, base.SuggestedHeight / 2f);
			}
		}
	}

	private void UpdateHandleByValue()
	{
		if (_valueFloat < MinValue)
		{
			ValueFloat = MinValue;
		}
		if (_valueFloat > MaxValue)
		{
			ValueFloat = MaxValue;
		}
		float num = 0f;
		if (MathF.Abs(MaxValue - MinValue) > float.Epsilon)
		{
			num = (_valueFloat - MinValue) / (MaxValue - MinValue);
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
				float x = ScrollbarArea.Size.X;
				x -= Handle.Size.X;
				Handle.ScaledPositionXOffset = x * num;
				Handle.ScaledPositionYOffset = 0f;
			}
			else
			{
				Handle.HorizontalAlignment = HorizontalAlignment.Center;
				Handle.VerticalAlignment = VerticalAlignment.Bottom;
				float y = ScrollbarArea.Size.Y;
				y -= Handle.Size.Y;
				Handle.ScaledPositionYOffset = -1f * y * (1f - num);
				Handle.ScaledPositionXOffset = 0f;
			}
		}
	}
}
