using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Order;

public class OrderSiegeDeploymentScreenWidget : Widget
{
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
		DeploymentListPanel.ParentWidget.IsVisible = deploymentItem != null;
		if (deploymentItem != null)
		{
			DeploymentListPanel.MarginLeft = (deploymentItem.GlobalPosition.X + deploymentItem.Size.Y + 20f) / base._scaleToUse;
			DeploymentListPanel.MarginTop = (deploymentItem.GlobalPosition.Y + (deploymentItem.Size.Y / 2f - DeploymentListPanel.Size.Y / 2f)) / base._scaleToUse;
		}
	}

	private void UpdateEnabledState(bool isEnabled)
	{
		this.SetGlobalAlphaRecursively(isEnabled ? 1f : 0.5f);
		base.DoNotPassEventsToChildren = !isEnabled;
	}
}
