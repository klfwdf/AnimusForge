using System;
using System.Numerics;
using TaleWorlds.GauntletUI.Layout;

namespace TaleWorlds.GauntletUI.BaseTypes;

public class ListPanel : Container
{
	private bool _dragHovering;

	private bool _resetSelectedOnLosingFocus;

	public StackLayout StackLayout { get; private set; }

	public override Predicate<Widget> AcceptDropPredicate { get; set; }

	public override bool IsDragHovering => _dragHovering;

	[Editor(false)]
	public bool ResetSelectedOnLosingFocus
	{
		get
		{
			return _resetSelectedOnLosingFocus;
		}
		set
		{
			if (_resetSelectedOnLosingFocus != value)
			{
				_resetSelectedOnLosingFocus = value;
				OnPropertyChanged(value, "ResetSelectedOnLosingFocus");
			}
		}
	}

	public ListPanel(UIContext context)
		: base(context)
	{
		StackLayout = new StackLayout();
		base.LayoutImp = StackLayout;
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		UpdateListPanel();
		if (ResetSelectedOnLosingFocus && !CheckIsMyChildRecursive(base.EventManager.LatestMouseDownWidget))
		{
			base.IntValue = -1;
		}
	}

	private void UpdateListPanel()
	{
		if (base.AcceptDrop && IsDragHovering)
		{
			base.DragHoverInsertionIndex = GetIndexForDrop(base.EventManager.MousePosition);
		}
	}

	public override int GetIndexForDrop(Vector2 mousePosition)
	{
		return StackLayout.GetIndexForDrop(this, mousePosition);
	}

	public override Vector2 GetDropGizmoPosition(Vector2 mousePosition)
	{
		return StackLayout.GetDropGizmoPosition(this, mousePosition);
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

	protected internal override void OnDragHoverBegin()
	{
		_dragHovering = true;
		SetMeasureAndLayoutDirty();
	}

	protected internal override void OnDragHoverEnd()
	{
		_dragHovering = false;
		SetMeasureAndLayoutDirty();
	}

	protected override bool OnPreviewDragHover()
	{
		return base.AcceptDrop;
	}
}
