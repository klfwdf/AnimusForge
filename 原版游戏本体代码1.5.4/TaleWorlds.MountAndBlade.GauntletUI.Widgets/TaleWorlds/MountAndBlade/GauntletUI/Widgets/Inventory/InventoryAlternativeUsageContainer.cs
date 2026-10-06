using System;
using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

public class InventoryAlternativeUsageContainer : Container
{
	private int _columnLimit = 2;

	private float _cellWidth = 100f;

	private float _cellHeight = 100f;

	[Editor(false)]
	public int ColumnLimit
	{
		get
		{
			return _columnLimit;
		}
		set
		{
			if (_columnLimit != value)
			{
				_columnLimit = value;
				OnPropertyChanged(value, "ColumnLimit");
			}
		}
	}

	[Editor(false)]
	public float CellWidth
	{
		get
		{
			return _cellWidth;
		}
		set
		{
			if (_cellWidth != value)
			{
				_cellWidth = value;
				OnPropertyChanged(value, "CellWidth");
			}
		}
	}

	[Editor(false)]
	public float CellHeight
	{
		get
		{
			return _cellHeight;
		}
		set
		{
			if (_cellHeight != value)
			{
				_cellHeight = value;
				OnPropertyChanged(value, "CellHeight");
			}
		}
	}

	public override Predicate<Widget> AcceptDropPredicate { get; set; }

	public override bool IsDragHovering { get; }

	public InventoryAlternativeUsageContainer(UIContext context)
		: base(context)
	{
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

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		foreach (Action<Widget, Widget> itemAddEventHandler in ItemAddEventHandlers)
		{
			itemAddEventHandler(this, child);
		}
		EventFired("ItemAdd");
		SetChildrenLayout();
	}

	protected override void OnBeforeChildRemoved(Widget child)
	{
		base.OnBeforeChildRemoved(child);
		foreach (Action<Widget, Widget> itemRemoveEventHandler in ItemRemoveEventHandlers)
		{
			itemRemoveEventHandler(this, child);
		}
		EventFired("ItemRemove");
		SetChildrenLayout();
	}

	private void SetChildrenLayout()
	{
		if (base.ChildCount == 0)
		{
			return;
		}
		int num = TaleWorlds.Library.MathF.Ceiling((float)base.ChildCount / (float)ColumnLimit);
		for (int i = 0; i < num; i++)
		{
			int num2 = TaleWorlds.Library.MathF.Min(ColumnLimit, base.ChildCount - (num - 1) * ColumnLimit);
			int num3 = i * (int)CellHeight;
			for (int j = 0; j < num2; j++)
			{
				int num4 = (int)(((float)j - ((float)num2 - 1f) / 2f) * CellWidth);
				int i2 = i * ColumnLimit + j;
				Widget child = GetChild(i2);
				if (num4 > 0)
				{
					child.MarginLeft = num4 * 2;
				}
				else if (num4 < 0)
				{
					child.MarginRight = -num4 * 2;
				}
				child.MarginTop = num3;
			}
		}
	}

	public override Vector2 GetDropGizmoPosition(Vector2 draggedWidgetPosition)
	{
		return Vector2.Zero;
	}

	public override int GetIndexForDrop(Vector2 draggedWidgetPosition)
	{
		return -1;
	}
}
