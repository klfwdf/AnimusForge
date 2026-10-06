using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Crafting;

public class CardSelectionPopupButtonWidget : ButtonWidget
{
	public CircularAutoScrollablePanelWidget PropertiesContainer { get; set; }

	public CardSelectionPopupButtonWidget(UIContext context)
		: base(context)
	{
	}

	public override void SetState(string stateName)
	{
		base.SetState(stateName);
		PropertiesContainer?.SetState(stateName);
	}

	protected override void OnHoverBegin()
	{
		base.OnHoverBegin();
		PropertiesContainer?.SetHoverBegin();
	}

	protected override void OnHoverEnd()
	{
		base.OnHoverEnd();
		PropertiesContainer?.SetHoverEnd();
	}

	protected override void OnMouseScroll()
	{
		base.OnMouseScroll();
		PropertiesContainer?.SetScrollMouse();
	}
}
