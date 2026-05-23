using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Order;

public class OrderSiegeMachineItemButtonWidget : ButtonWidget
{
	private bool _isRemainingCountVisible = true;

	private bool _isVisualsDirty = true;

	private int _remainingCount;

	private TextWidget _remainingCountWidget;

	private string _machineClass;

	private Widget _machineIconWidget;

	[Editor(false)]
	public int RemainingCount
	{
		get
		{
			return _remainingCount;
		}
		set
		{
			if (_remainingCount != value)
			{
				_remainingCount = value;
				OnPropertyChanged(value, "RemainingCount");
				_isVisualsDirty = true;
			}
		}
	}

	[Editor(false)]
	public TextWidget RemainingCountWidget
	{
		get
		{
			return _remainingCountWidget;
		}
		set
		{
			if (_remainingCountWidget != value)
			{
				_remainingCountWidget = value;
				OnPropertyChanged(value, "RemainingCountWidget");
				_isVisualsDirty = true;
			}
		}
	}

	[Editor(false)]
	public string MachineClass
	{
		get
		{
			return _machineClass;
		}
		set
		{
			if (_machineClass != value)
			{
				_machineClass = value;
				OnPropertyChanged(value, "MachineClass");
				_isVisualsDirty = true;
			}
		}
	}

	[Editor(false)]
	public Widget MachineIconWidget
	{
		get
		{
			return _machineIconWidget;
		}
		set
		{
			if (_machineIconWidget != value)
			{
				_machineIconWidget = value;
				OnPropertyChanged(value, "MachineIconWidget");
				_isVisualsDirty = true;
			}
		}
	}

	public OrderSiegeMachineItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isVisualsDirty)
		{
			MachineIconWidgetChanged();
			UpdateRemainingCount();
			UpdateMachineIcon();
			_isVisualsDirty = false;
		}
	}

	private void MachineIconWidgetChanged()
	{
		if (MachineIconWidget != null)
		{
			MachineIconWidget.RegisterBrushStatesOfWidget();
		}
	}

	private void UpdateMachineIcon()
	{
		if (MachineIconWidget != null)
		{
			_isRemainingCountVisible = true;
			switch (MachineClass)
			{
			case "preparations":
				MachineIconWidget.SetState("Preparations");
				break;
			case "ballista":
				MachineIconWidget.SetState("Ballista");
				break;
			case "fire_ballista":
				MachineIconWidget.SetState("FireBallista");
				break;
			case "bricole":
				MachineIconWidget.SetState("Bricole");
				break;
			case "ladder":
				MachineIconWidget.SetState("Ladder");
				break;
			case "Mangonel":
				MachineIconWidget.SetState("Mangonel");
				break;
			case "FireMangonel":
				MachineIconWidget.SetState("FireMangonel");
				break;
			case "ram":
				MachineIconWidget.SetState("Ram");
				break;
			case "improved_ram":
				MachineIconWidget.SetState("ImprovedRam");
				break;
			case "siege_tower_level1":
				MachineIconWidget.SetState("SiegeTower");
				break;
			case "trebuchet":
				MachineIconWidget.SetState("Trebuchet");
				break;
			case "onager":
				MachineIconWidget.SetState("Onager");
				break;
			case "fire_onager":
				MachineIconWidget.SetState("FireOnager");
				break;
			case "fire_catapult":
				MachineIconWidget.SetState("FireCatapult");
				break;
			case "catapult":
				MachineIconWidget.SetState("Catapult");
				break;
			default:
				MachineIconWidget.SetState("None");
				_isRemainingCountVisible = false;
				break;
			}
		}
	}

	private void UpdateRemainingCount()
	{
		if (RemainingCountWidget != null)
		{
			base.IsDisabled = RemainingCount == 0;
			RemainingCountWidget.IsVisible = _isRemainingCountVisible;
		}
	}
}
