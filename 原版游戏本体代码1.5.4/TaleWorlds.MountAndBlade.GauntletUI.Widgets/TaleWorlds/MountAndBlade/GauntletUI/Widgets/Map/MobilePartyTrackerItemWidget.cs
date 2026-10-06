using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map;

public class MobilePartyTrackerItemWidget : Widget
{
	private float _screenWidth;

	private float _screenHeight;

	private bool _isActive;

	private bool _isBehind;

	private bool _isTracked;

	private string _trackerType;

	private Vec2 _position;

	private Brush _trackerImageBrush;

	public Widget FrameVisualWidget { get; set; }

	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (_isActive != value)
			{
				_isActive = value;
				OnPropertyChanged(value, "IsActive");
			}
		}
	}

	public bool IsBehind
	{
		get
		{
			return _isBehind;
		}
		set
		{
			if (_isBehind != value)
			{
				_isBehind = value;
				OnPropertyChanged(value, "IsBehind");
			}
		}
	}

	public bool IsTracked
	{
		get
		{
			return _isTracked;
		}
		set
		{
			if (_isTracked != value)
			{
				_isTracked = value;
				OnPropertyChanged(value, "IsTracked");
			}
		}
	}

	public string TrackerType
	{
		get
		{
			return _trackerType;
		}
		set
		{
			if (_trackerType != value)
			{
				_trackerType = value;
				OnPropertyChanged(value, "TrackerType");
				UpdateTrackerVisual();
			}
		}
	}

	public Vec2 Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (_position != value)
			{
				_position = value;
				OnPropertyChanged(value, "Position");
			}
		}
	}

	public Brush TrackerImageBrush
	{
		get
		{
			return _trackerImageBrush;
		}
		set
		{
			if (_trackerImageBrush != value)
			{
				_trackerImageBrush = value;
				OnPropertyChanged(value, "TrackerImageBrush");
				UpdateTrackerVisual();
			}
		}
	}

	public MobilePartyTrackerItemWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		UpdateScreenPosition();
	}

	private void UpdateScreenPosition()
	{
		_screenWidth = base.Context.EventManager.PageSize.X;
		_screenHeight = base.Context.EventManager.PageSize.Y;
		if (!IsActive)
		{
			base.IsHidden = true;
			return;
		}
		Vec2 vec = new Vec2(Position);
		if (IsTracked)
		{
			if (!IsBehind && vec.X - base.Size.X / 2f > 0f && vec.x + base.Size.X / 2f < base.Context.EventManager.PageSize.X && vec.y > 0f && vec.y + base.Size.Y < base.Context.EventManager.PageSize.Y)
			{
				base.ScaledPositionXOffset = vec.x - base.Size.X / 2f;
				base.ScaledPositionYOffset = vec.y;
			}
			else
			{
				Vec2 vec2 = new Vec2(base.Context.EventManager.PageSize.X / 2f, base.Context.EventManager.PageSize.Y / 2f);
				vec -= vec2;
				if (IsBehind)
				{
					vec *= -1f;
				}
				float radian = Mathf.Atan2(vec.y, vec.x) - System.MathF.PI / 2f;
				float num = Mathf.Cos(radian);
				float num2 = Mathf.Sin(radian);
				float num3 = num / num2;
				Vec2 vec3 = vec2 * 1f;
				vec = ((num > 0f) ? new Vec2((0f - vec3.y) / num3, vec2.y) : new Vec2(vec3.y / num3, 0f - vec2.y));
				if (vec.x > vec3.x)
				{
					vec = new Vec2(vec3.x, (0f - vec3.x) * num3);
				}
				else if (vec.x < 0f - vec3.x)
				{
					vec = new Vec2(0f - vec3.x, vec3.x * num3);
				}
				vec += vec2;
				base.ScaledPositionXOffset = Mathf.Clamp(vec.x - base.Size.X / 2f, 0f, _screenWidth - base.Size.X);
				base.ScaledPositionYOffset = Mathf.Clamp(vec.y, 0f, _screenHeight - (base.Size.Y + 55f));
			}
		}
		else
		{
			base.ScaledPositionXOffset = Position.x - base.Size.X / 2f;
			base.ScaledPositionYOffset = Position.y;
		}
		base.IsHidden = (!IsTracked && IsBehind) || base.ScaledPositionXOffset > base.Context.TwoDimensionContext.Width || base.ScaledPositionYOffset > base.Context.TwoDimensionContext.Height || base.ScaledPositionXOffset + base.Size.X < 0f || base.ScaledPositionYOffset + base.Size.Y < 0f;
	}

	private void UpdateTrackerVisual()
	{
		if (FrameVisualWidget != null && TrackerImageBrush != null && !string.IsNullOrEmpty(TrackerType))
		{
			FrameVisualWidget.Sprite = TrackerImageBrush.GetLayer(TrackerType)?.Sprite;
		}
	}
}
