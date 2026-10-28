using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Order;

public class OrderSiegeDeploymentScreenWidget : Widget
{
	private OrderSiegeDeploymentItemButtonWidget _selectedDeploymentItem;

	private bool _isSiegeDeploymentDisabled;

	private Widget _deploymentTargetsParent;

	private ListPanel _deploymentListPanel;

	public bool IsSiegeDeploymentDisabled
	{
		get
		{
			return _isSiegeDeploymentDisabled;
		}
		set
		{
			if (value != _isSiegeDeploymentDisabled)
			{
				_isSiegeDeploymentDisabled = value;
				OnPropertyChanged(value, "IsSiegeDeploymentDisabled");
				UpdateEnabledState(!value);
			}
		}
	}

	public Widget DeploymentTargetsParent
	{
		get
		{
			return _deploymentTargetsParent;
		}
		set
		{
			if (_deploymentTargetsParent != value)
			{
				_deploymentTargetsParent = value;
				OnPropertyChanged(value, "DeploymentTargetsParent");
			}
		}
	}

	public ListPanel DeploymentListPanel
	{
		get
		{
			return _deploymentListPanel;
		}
		set
		{
			if (_deploymentListPanel != value)
			{
				_deploymentListPanel = value;
				OnPropertyChanged(value, "DeploymentListPanel");
			}
		}
	}

	public OrderSiegeDeploymentScreenWidget(UIContext context)
		: base(context)
	{
	}

	public void SetSelectedDeploymentItem(OrderSiegeDeploymentItemButtonWidget deploymentItem)
	{
		_selectedDeploymentItem = deploymentItem;
		DeploymentListPanel.ParentWidget.IsVisible = _selectedDeploymentItem != null;
		UpdatePosition();
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		UpdatePosition();
		HandleClickOutside();
	}

	private void HandleClickOutside()
	{
		if (_selectedDeploymentItem == null || _selectedDeploymentItem.IsPointInsideMeasuredArea(base.EventManager.MousePosition) || DeploymentListPanel.ParentWidget.IsPointInsideMeasuredArea(base.EventManager.MousePosition))
		{
			return;
		}
		InputKey[] clickKeys = base.Context.InputContext.GetClickKeys();
		for (int i = 0; i < clickKeys.Length; i++)
		{
			if (Input.IsKeyPressed(clickKeys[i]))
			{
				EventFired("SelectNone");
				break;
			}
		}
	}

	private void UpdateEnabledState(bool isEnabled)
	{
		this.SetGlobalAlphaRecursively(isEnabled ? 1f : 0.5f);
		base.DoNotPassEventsToChildren = !isEnabled;
	}

	private void UpdatePosition()
	{
		if (_selectedDeploymentItem != null)
		{
			DeploymentListPanel.MarginLeft = (_selectedDeploymentItem.GlobalPosition.X + _selectedDeploymentItem.Size.Y + 20f) / base._scaleToUse;
			DeploymentListPanel.MarginTop = (_selectedDeploymentItem.GlobalPosition.Y + (_selectedDeploymentItem.Size.Y / 2f - DeploymentListPanel.Size.Y / 2f)) / base._scaleToUse;
		}
	}
}
