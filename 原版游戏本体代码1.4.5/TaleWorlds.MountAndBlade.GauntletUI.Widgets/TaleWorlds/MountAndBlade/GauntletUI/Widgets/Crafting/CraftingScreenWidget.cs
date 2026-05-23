using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Crafting;

public class CraftingScreenWidget : Widget
{
	private ButtonWidget _mainActionButtonWidget;

	private ButtonWidget _finalCraftButtonWidget;

	private bool _isInCraftingMode;

	private bool _isInRefinementMode;

	private bool _isInSmeltingMode;

	private Widget _newCraftedWeaponPopupWidget;

	private Widget _craftingOrdersPopupWidget;

	[Editor(false)]
	public bool IsInCraftingMode
	{
		get
		{
			return _isInCraftingMode;
		}
		set
		{
			if (_isInCraftingMode != value)
			{
				_isInCraftingMode = value;
				OnPropertyChanged(value, "IsInCraftingMode");
			}
		}
	}

	[Editor(false)]
	public bool IsInRefinementMode
	{
		get
		{
			return _isInRefinementMode;
		}
		set
		{
			if (_isInRefinementMode != value)
			{
				_isInRefinementMode = value;
				OnPropertyChanged(value, "IsInRefinementMode");
			}
		}
	}

	[Editor(false)]
	public bool IsInSmeltingMode
	{
		get
		{
			return _isInSmeltingMode;
		}
		set
		{
			if (_isInSmeltingMode != value)
			{
				_isInSmeltingMode = value;
				OnPropertyChanged(value, "IsInSmeltingMode");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget MainActionButtonWidget
	{
		get
		{
			return _mainActionButtonWidget;
		}
		set
		{
			if (_mainActionButtonWidget != value)
			{
				_mainActionButtonWidget = value;
				OnPropertyChanged(value, "MainActionButtonWidget");
				if (!value.ClickEventHandlers.Contains(OnMainAction))
				{
					value.ClickEventHandlers.Add(OnMainAction);
				}
			}
		}
	}

	[Editor(false)]
	public ButtonWidget FinalCraftButtonWidget
	{
		get
		{
			return _mainActionButtonWidget;
		}
		set
		{
			if (_finalCraftButtonWidget != value)
			{
				_finalCraftButtonWidget = value;
				OnPropertyChanged(value, "FinalCraftButtonWidget");
				if (!value.ClickEventHandlers.Contains(OnFinalAction))
				{
					value.ClickEventHandlers.Add(OnFinalAction);
				}
			}
		}
	}

	[Editor(false)]
	public Widget NewCraftedWeaponPopupWidget
	{
		get
		{
			return _newCraftedWeaponPopupWidget;
		}
		set
		{
			if (_newCraftedWeaponPopupWidget != value)
			{
				_newCraftedWeaponPopupWidget = value;
				OnPropertyChanged(value, "NewCraftedWeaponPopupWidget");
			}
		}
	}

	[Editor(false)]
	public Widget CraftingOrderPopupWidget
	{
		get
		{
			return _craftingOrdersPopupWidget;
		}
		set
		{
			if (_craftingOrdersPopupWidget != value)
			{
				_craftingOrdersPopupWidget = value;
				OnPropertyChanged(value, "CraftingOrderPopupWidget");
			}
		}
	}

	public CraftingScreenWidget(UIContext context)
		: base(context)
	{
	}

	private void OnMainAction(Widget widget)
	{
		if (IsInCraftingMode)
		{
			base.Context.TwoDimensionContext.PlaySound("crafting/craft_success");
		}
		else if (IsInRefinementMode)
		{
			base.Context.TwoDimensionContext.PlaySound("crafting/refine_success");
		}
		else if (IsInSmeltingMode)
		{
			base.Context.TwoDimensionContext.PlaySound("crafting/smelt_success");
		}
	}

	private void OnFinalAction(Widget widget)
	{
		if (NewCraftedWeaponPopupWidget != null && IsInCraftingMode)
		{
			_ = NewCraftedWeaponPopupWidget.IsVisible;
		}
	}
}
