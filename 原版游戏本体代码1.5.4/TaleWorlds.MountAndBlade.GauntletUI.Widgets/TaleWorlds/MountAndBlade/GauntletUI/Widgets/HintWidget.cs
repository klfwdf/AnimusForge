using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class HintWidget : Widget
{
	public HintWidget(UIContext context)
		: base(context)
	{
		base.IsDisabled = true;
		base.DoNotAcceptEvents = true;
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

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		Debug.FailedAssert("HintWidget is not intended to be used as a parent widget!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\HintWidget.cs", "OnChildAdded", 34);
	}

	private void ParentWidgetEventFired(Widget widget, string eventName, object[] args)
	{
		if (base.IsVisible)
		{
			switch (eventName)
			{
			case "HoverBegin":
				EventFired("HoverBegin", args);
				break;
			case "HoverEnd":
				EventFired("HoverEnd", args);
				break;
			case "DragHoverBegin":
				EventFired("DragHoverBegin", args);
				break;
			case "DragHoverEnd":
				EventFired("DragHoverEnd", args);
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
		return false;
	}

	protected override bool OnPreviewDragHover()
	{
		return false;
	}
}
