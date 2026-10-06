using System;
using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Order;

public class OrderSiegeDeploymentItemButtonWidget : ButtonWidget
{
	private bool preSelectedState;

	private bool _isVisualsDirty = true;

	private Vec2 _position;

	private bool _isInsideWindow;

	private bool _isInFront;

	private bool _isPlayerGeneral;

	private OrderSiegeDeploymentScreenWidget _screenWidget;

	private int _pointType;

	private Widget _typeIconWidget;

	private TextWidget _breachedTextWidget;

	[Editor(false)]
	public TextWidget BreachedTextWidget
	{
		get
		{
			return _breachedTextWidget;
		}
		set
		{
			if (_breachedTextWidget != value)
			{
				_breachedTextWidget = value;
				OnPropertyChanged(value, "BreachedTextWidget");
				_isVisualsDirty = true;
			}
		}
	}

	[Editor(false)]
	public Widget TypeIconWidget
	{
		get
		{
			return _typeIconWidget;
		}
		set
		{
			if (_typeIconWidget != value)
			{
				_typeIconWidget = value;
				OnPropertyChanged(value, "TypeIconWidget");
				_isVisualsDirty = true;
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

	public int PointType
	{
		get
		{
			return _pointType;
		}
		set
		{
			if (_pointType != value)
			{
				_pointType = value;
				OnPropertyChanged(value, "PointType");
			}
		}
	}

	public bool IsInsideWindow
	{
		get
		{
			return _isInsideWindow;
		}
		set
		{
			if (_isInsideWindow != value)
			{
				_isInsideWindow = value;
				OnPropertyChanged(value, "IsInsideWindow");
			}
		}
	}

	public bool IsInFront
	{
		get
		{
			return _isInFront;
		}
		set
		{
			if (_isInFront != value)
			{
				_isInFront = value;
				OnPropertyChanged(value, "IsInFront");
			}
		}
	}

	public bool IsPlayerGeneral
	{
		get
		{
			return _isPlayerGeneral;
		}
		set
		{
			if (_isPlayerGeneral != value)
			{
				_isPlayerGeneral = value;
				OnPropertyChanged(value, "IsPlayerGeneral");
			}
		}
	}

	public OrderSiegeDeploymentScreenWidget ScreenWidget
	{
		get
		{
			return _screenWidget;
		}
		set
		{
			if (_screenWidget != value)
			{
				_screenWidget = value;
				OnPropertyChanged(value, "ScreenWidget");
			}
		}
	}

	public OrderSiegeDeploymentItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		base.IsEnabled = IsPlayerGeneral && PointType != 2;
		if (preSelectedState != base.IsSelected)
		{
			if (base.IsSelected)
			{
				ScreenWidget.SetSelectedDeploymentItem(this);
			}
			preSelectedState = base.IsSelected;
		}
		if (_isVisualsDirty)
		{
			UpdateTypeVisuals();
			_isVisualsDirty = false;
		}
		UpdateScreenPosition();
	}

	private void UpdateScreenPosition()
	{
		float num = Position.X - base.Size.X / 2f;
		float num2 = Position.X + base.Size.X / 2f;
		float num3 = Position.Y - base.Size.Y / 2f;
		float num4 = Position.Y + base.Size.Y / 2f;
		bool flag = IsInFront && num > 0f && num2 < base.Context.EventManager.PageSize.X && num3 > 0f && num4 < base.Context.EventManager.PageSize.Y;
		bool flag2 = IsInFront && (num2 > 0f || num < base.Context.EventManager.PageSize.X) && (num4 > 0f || num3 < base.Context.EventManager.PageSize.Y);
		if (!flag && base.IsSelected)
		{
			base.IsVisible = true;
			Vec2 vec = new Vec2(num, num3);
			Vector2 vector = base.Context.EventManager.PageSize - base.Size;
			Vec2 vec2 = vector / 2f;
			vec -= vec2;
			if (!IsInFront)
			{
				vec *= -1f;
			}
			float radian = Mathf.Atan2(vec.y, vec.x) - System.MathF.PI / 2f;
			float num5 = Mathf.Cos(radian);
			float num6 = Mathf.Sin(radian);
			float num7 = num5 / num6;
			Vec2 vec3 = vec2 * 1f;
			vec = ((num5 > 0f) ? new Vec2((0f - vec3.y) / num7, vec2.y) : new Vec2(vec3.y / num7, 0f - vec2.y));
			if (vec.x > vec3.x)
			{
				vec = new Vec2(vec3.x, (0f - vec3.x) * num7);
			}
			else if (vec.x < 0f - vec3.x)
			{
				vec = new Vec2(0f - vec3.x, vec3.x * num7);
			}
			vec += vec2;
			base.ScaledPositionXOffset = Mathf.Clamp(vec.x, 0f, vector.X);
			base.ScaledPositionYOffset = Mathf.Clamp(vec.y, 0f, vector.Y);
		}
		else if (flag || flag2)
		{
			base.IsVisible = true;
			base.ScaledPositionXOffset = num;
			base.ScaledPositionYOffset = num3;
		}
		else
		{
			base.IsVisible = false;
		}
	}

	private void UpdateTypeVisuals()
	{
		TypeIconWidget.RegisterBrushStatesOfWidget();
		BreachedTextWidget.IsVisible = PointType == 2;
		TypeIconWidget.IsVisible = PointType != 2;
		if (PointType == 0)
		{
			TypeIconWidget.SetState("BatteringRam");
		}
		else if (PointType == 1)
		{
			TypeIconWidget.SetState("TowerLadder");
		}
		else if (PointType == 2)
		{
			TypeIconWidget.SetState("Breach");
		}
		else if (PointType == 3)
		{
			TypeIconWidget.SetState("Ranged");
		}
		else
		{
			TypeIconWidget.SetState("Default");
		}
	}
}
