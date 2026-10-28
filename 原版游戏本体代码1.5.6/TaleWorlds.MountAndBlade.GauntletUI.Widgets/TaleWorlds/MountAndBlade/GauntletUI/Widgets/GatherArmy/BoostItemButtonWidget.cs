using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.GatherArmy;

public class BoostItemButtonWidget : ButtonWidget
{
	private int _boostCurrencyType = -1;

	private Widget _boostCurrencyIconWidget;

	public BoostCohesionPopupWidget ParentPopupWidget { get; private set; }

	[Editor(false)]
	public int BoostCurrencyType
	{
		get
		{
			return _boostCurrencyType;
		}
		set
		{
			if (_boostCurrencyType != value)
			{
				_boostCurrencyType = value;
				OnPropertyChanged(value, "BoostCurrencyType");
			}
		}
	}

	[Editor(false)]
	public Widget BoostCurrencyIconWidget
	{
		get
		{
			return _boostCurrencyIconWidget;
		}
		set
		{
			if (_boostCurrencyIconWidget != value)
			{
				_boostCurrencyIconWidget = value;
				OnPropertyChanged(value, "BoostCurrencyIconWidget");
			}
		}
	}

	public BoostItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (BoostCurrencyIconWidget != null)
		{
			switch (BoostCurrencyType)
			{
			case 0:
				BoostCurrencyIconWidget.SetState("Gold");
				break;
			case 1:
				BoostCurrencyIconWidget.SetState("Influence");
				break;
			}
		}
		if (ParentPopupWidget == null)
		{
			ParentPopupWidget = FindParentPopupWidget();
			if (ParentPopupWidget != null)
			{
				ClickEventHandlers.Add(ParentPopupWidget.ClosePopup);
			}
		}
	}

	private BoostCohesionPopupWidget FindParentPopupWidget()
	{
		Widget widget = this;
		while (widget != base.EventManager.Root && ParentPopupWidget == null)
		{
			if (widget is BoostCohesionPopupWidget)
			{
				return widget as BoostCohesionPopupWidget;
			}
			widget = widget.ParentWidget;
		}
		return null;
	}
}
