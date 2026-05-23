using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class HintWidget : Widget
{
	public HintWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnConnectedToRoot()
	{
		base.ParentWidget.EventFire += ParentWidgetEventFired;
		base.OnConnectedToRoot();
	}

	protected override void OnDisconnectedFromRoot()
	{
		base.ParentWidget.EventFire -= ParentWidgetEventFired;
		base.OnDisconnectedFromRoot();
	}

	private void ParentWidgetEventFired(Widget widget, string eventName, object[] args)
	{
		if (base.IsVisible)
		{
			switch (eventName)
			{
			case "HoverBegin":
				EventFired("HoverBegin");
				break;
			case "HoverEnd":
				EventFired("HoverEnd");
				break;
			case "DragHoverBegin":
				EventFired("DragHoverBegin");
				break;
			case "DragHoverEnd":
				EventFired("DragHoverEnd");
				break;
			}
		}
	}

	protected override bool OnPreviewMousePressed()
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

	protected override bool OnPreviewMouseScroll()
	{
		return false;
	}

	protected override bool OnPreviewMouseReleased()
	{
		return false;
	}

	protected override bool OnPreviewMouseMove()
	{
		return true;
	}

	protected override bool OnPreviewDragHover()
	{
		return false;
	}
}
