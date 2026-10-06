using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Crafting;

public class CraftingPieceTypeSelectorButtonWidget : ButtonWidget
{
	private Widget _visualsWidget;

	public Widget VisualsWidget
	{
		get
		{
			return _visualsWidget;
		}
		set
		{
			if (value != _visualsWidget)
			{
				_visualsWidget = value;
			}
		}
	}

	public CraftingPieceTypeSelectorButtonWidget(UIContext context)
		: base(context)
	{
	}

	public override void SetState(string stateName)
	{
		base.SetState(stateName);
		VisualsWidget?.SetState(stateName);
	}
}
