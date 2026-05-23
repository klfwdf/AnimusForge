using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

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
		base.IsVisible = IsInsideWindow && IsInFront;
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
		UpdatePosition();
	}

	private void UpdatePosition()
	{
		if (IsInsideWindow)
		{
			base.ScaledPositionXOffset = Position.x - base.Size.X / 2f;
			base.ScaledPositionYOffset = Position.y - base.Size.Y;
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
