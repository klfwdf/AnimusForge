using System;
using System.Numerics;
using TaleWorlds.GauntletUI.Layout;

namespace TaleWorlds.GauntletUI.BaseTypes;

public class GridWidget : Container
{
	private float _defaultCellWidth;

	private float _defaultCellHeight;

	private int _rowCount;

	private int _columnCount;

	private bool _useDynamicCellWidth;

	private bool _useDynamicCellHeight;

	public const int DefaultRowCount = 3;

	public const int DefaultColumnCount = 3;

	public GridLayout GridLayout { get; private set; }

	[Editor(false)]
	public float DefaultCellWidth
	{
		get
		{
			return _defaultCellWidth;
		}
		set
		{
			if (_defaultCellWidth != value)
			{
				_defaultCellWidth = value;
				OnPropertyChanged(value, "DefaultCellWidth");
			}
		}
	}

	public float DefaultScaledCellWidth => DefaultCellWidth * base._scaleToUse;

	[Editor(false)]
	public float DefaultCellHeight
	{
		get
		{
			return _defaultCellHeight;
		}
		set
		{
			if (_defaultCellHeight != value)
			{
				_defaultCellHeight = value;
				OnPropertyChanged(value, "DefaultCellHeight");
			}
		}
	}

	public float DefaultScaledCellHeight => DefaultCellHeight * base._scaleToUse;

	[Editor(false)]
	public int RowCount
	{
		get
		{
			return _rowCount;
		}
		set
		{
			if (_rowCount != value)
			{
				_rowCount = value;
				OnPropertyChanged(value, "RowCount");
			}
		}
	}

	[Editor(false)]
	public int ColumnCount
	{
		get
		{
			return _columnCount;
		}
		set
		{
			if (_columnCount != value)
			{
				_columnCount = value;
				OnPropertyChanged(value, "ColumnCount");
			}
		}
	}

	[Editor(false)]
	public bool UseDynamicCellWidth
	{
		get
		{
			return _useDynamicCellWidth;
		}
		set
		{
			if (_useDynamicCellWidth != value)
			{
				_useDynamicCellWidth = value;
				OnPropertyChanged(value, "UseDynamicCellWidth");
			}
		}
	}

	[Editor(false)]
	public bool UseDynamicCellHeight
	{
		get
		{
			return _useDynamicCellHeight;
		}
		set
		{
			if (_useDynamicCellHeight != value)
			{
				_useDynamicCellHeight = value;
				OnPropertyChanged(value, "UseDynamicCellHeight");
			}
		}
	}

	public override Predicate<Widget> AcceptDropPredicate { get; set; }

	public override bool IsDragHovering => false;

	public GridWidget(UIContext context)
		: base(context)
	{
		GridLayout = new GridLayout();
		base.LayoutImp = GridLayout;
		RowCount = -1;
		ColumnCount = -1;
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
