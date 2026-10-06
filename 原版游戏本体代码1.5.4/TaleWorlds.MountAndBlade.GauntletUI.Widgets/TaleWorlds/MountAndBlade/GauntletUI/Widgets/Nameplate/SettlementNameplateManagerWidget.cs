using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Nameplate;

public class SettlementNameplateManagerWidget : Widget
{
	private readonly List<SettlementNameplateWidget> _visibleNameplates = new List<SettlementNameplateWidget>();

	private List<SettlementNameplateWidget> _allChildrenNameplates = new List<SettlementNameplateWidget>();

	public SettlementNameplateManagerWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		_visibleNameplates.Clear();
		for (int i = 0; i < _allChildrenNameplates.Count; i++)
		{
			SettlementNameplateWidget settlementNameplateWidget = _allChildrenNameplates[i];
			if (settlementNameplateWidget != null && settlementNameplateWidget.IsVisibleOnMap)
			{
				_visibleNameplates.Add(settlementNameplateWidget);
			}
		}
		_visibleNameplates.Sort();
		for (int j = 0; j < _visibleNameplates.Count; j++)
		{
			SettlementNameplateWidget settlementNameplateWidget2 = _visibleNameplates[j];
			settlementNameplateWidget2.DisableRender = false;
			settlementNameplateWidget2.Render(twoDimensionContext, drawContext);
			settlementNameplateWidget2.DisableRender = true;
		}
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		child.DisableRender = true;
		_allChildrenNameplates.Add(child as SettlementNameplateWidget);
	}

	protected override void OnBeforeChildRemoved(Widget child)
	{
		base.OnBeforeChildRemoved(child);
		_allChildrenNameplates.Remove(child as SettlementNameplateWidget);
	}

	protected override void OnDisconnectedFromRoot()
	{
		base.OnDisconnectedFromRoot();
		_allChildrenNameplates.Clear();
		_allChildrenNameplates = null;
	}
}
