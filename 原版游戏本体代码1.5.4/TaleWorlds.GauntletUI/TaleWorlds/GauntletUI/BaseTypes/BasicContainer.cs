using System;
using System.Numerics;

namespace TaleWorlds.GauntletUI.BaseTypes;

public class BasicContainer : Container
{
	public override Predicate<Widget> AcceptDropPredicate { get; set; }

	public override bool IsDragHovering { get; }

	public BasicContainer(UIContext context)
		: base(context)
	{
	}

	public override Vector2 GetDropGizmoPosition(Vector2 draggedWidgetPosition)
	{
		throw new NotImplementedException();
	}

	public override int GetIndexForDrop(Vector2 draggedWidgetPosition)
	{
		throw new NotImplementedException();
	}

	public override void OnChildSelected(Widget widget)
	{
		int intValue = -1;
		for (int i = 0; i < base.ChildCount; i++)
		{
			if (widget == GetChild(i))
			{
				intValue = i;
			}
		}
		base.IntValue = intValue;
	}
}
