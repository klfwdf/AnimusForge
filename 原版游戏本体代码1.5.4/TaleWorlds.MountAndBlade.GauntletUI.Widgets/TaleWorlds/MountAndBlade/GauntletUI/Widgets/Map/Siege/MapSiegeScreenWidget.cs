using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.Siege;

public class MapSiegeScreenWidget : Widget
{
	private Widget _deployableSiegeMachinesPopup;

	private MapSiegeMachineButtonWidget _currentSelectedButton;

	[Editor(false)]
	public Widget DeployableSiegeMachinesPopup
	{
		get
		{
			return _deployableSiegeMachinesPopup;
		}
		set
		{
			if (value != _deployableSiegeMachinesPopup)
			{
				_deployableSiegeMachinesPopup = value;
				OnPropertyChanged(value, "DeployableSiegeMachinesPopup");
			}
		}
	}

	public MapSiegeScreenWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		Widget latestMouseUpWidget = base.EventManager.LatestMouseUpWidget;
		if (_currentSelectedButton != null && latestMouseUpWidget != null && !(latestMouseUpWidget is MapSiegeMachineButtonWidget) && !_currentSelectedButton.CheckIsMyChildRecursive(latestMouseUpWidget) && IsWidgetChildOfType<MapSiegeMachineButtonWidget>(latestMouseUpWidget) == null)
		{
			SetCurrentButton(null);
		}
		if (base.EventManager.LatestMouseUpWidget == null)
		{
			SetCurrentButton(null);
		}
		if (DeployableSiegeMachinesPopup != null)
		{
			DeployableSiegeMachinesPopup.IsVisible = _currentSelectedButton != null;
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_currentSelectedButton != null && DeployableSiegeMachinesPopup != null)
		{
			DeployableSiegeMachinesPopup.ScaledPositionXOffset = Mathf.Clamp(_currentSelectedButton.GlobalPosition.X - DeployableSiegeMachinesPopup.Size.X / 2f + _currentSelectedButton.Size.X / 2f, 0f, base.EventManager.PageSize.X - DeployableSiegeMachinesPopup.Size.X);
			DeployableSiegeMachinesPopup.ScaledPositionYOffset = Mathf.Clamp(_currentSelectedButton.GlobalPosition.Y + _currentSelectedButton.Size.Y + 10f * base._inverseScaleToUse, 0f, base.EventManager.PageSize.Y - DeployableSiegeMachinesPopup.Size.Y);
		}
	}

	public void SetCurrentButton(MapSiegeMachineButtonWidget button)
	{
		if (button == null)
		{
			_currentSelectedButton = null;
		}
		else if (_currentSelectedButton == button || !button.IsDeploymentTarget)
		{
			SetCurrentButton(null);
		}
		else
		{
			_currentSelectedButton = button;
		}
	}

	protected override bool OnPreviewMousePressed()
	{
		SetCurrentButton(null);
		return false;
	}

	protected override bool OnPreviewDragEnd()
	{
		return false;
	}

	protected override bool OnPreviewDragBegin()
	{
		return false;
	}

	protected override bool OnPreviewDrop()
	{
		return false;
	}

	protected override bool OnPreviewDragHover()
	{
		return false;
	}

	protected override bool OnPreviewMouseMove()
	{
		return false;
	}

	protected override bool OnPreviewMouseReleased()
	{
		return false;
	}

	protected override bool OnPreviewMouseScroll()
	{
		return false;
	}

	protected override bool OnPreviewMouseAlternatePressed()
	{
		return false;
	}

	protected override bool OnPreviewMouseAlternateReleased()
	{
		return false;
	}

	private T IsWidgetChildOfType<T>(Widget currentWidget) where T : Widget
	{
		while (currentWidget != null)
		{
			if (currentWidget is T)
			{
				return (T)currentWidget;
			}
			currentWidget = currentWidget.ParentWidget;
		}
		return null;
	}
}
