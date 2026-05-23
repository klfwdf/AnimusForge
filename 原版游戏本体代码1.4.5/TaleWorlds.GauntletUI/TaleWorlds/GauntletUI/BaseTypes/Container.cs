using System;
using System.Collections.Generic;
using System.Numerics;

namespace TaleWorlds.GauntletUI.BaseTypes;

public abstract class Container : Widget
{
	public List<Action<Widget>> SelectEventHandlers = new List<Action<Widget>>();

	public List<Action<Widget, Widget>> ItemAddEventHandlers = new List<Action<Widget, Widget>>();

	public List<Action<Widget, Widget>> ItemRemoveEventHandlers = new List<Action<Widget, Widget>>();

	public List<Action<Widget>> ItemAfterRemoveEventHandlers = new List<Action<Widget>>();

	private int _intValue = -1;

	private bool _currentlyChangingIntValue;

	public bool ShowSelection;

	private int _dragHoverInsertionIndex;

	private List<ContainerItemDescription> _itemDescriptions;

	private bool _clearSelectedOnRemoval;

	public ContainerItemDescription DefaultItemDescription { get; private set; }

	public abstract Predicate<Widget> AcceptDropPredicate { get; set; }

	public int IntValue
	{
		get
		{
			return _intValue;
		}
		set
		{
			if (_currentlyChangingIntValue)
			{
				return;
			}
			_currentlyChangingIntValue = true;
			if (value != _intValue && value < base.ChildCount)
			{
				_intValue = value;
				UpdateSelected();
				foreach (Action<Widget> selectEventHandler in SelectEventHandlers)
				{
					selectEventHandler(this);
				}
				EventFired("SelectedItemChange");
				OnPropertyChanged(value, "IntValue");
			}
			_currentlyChangingIntValue = false;
		}
	}

	public abstract bool IsDragHovering { get; }

	public int DragHoverInsertionIndex
	{
		get
		{
			return _dragHoverInsertionIndex;
		}
		set
		{
			if (_dragHoverInsertionIndex != value)
			{
				_dragHoverInsertionIndex = value;
				SetMeasureAndLayoutDirty();
			}
		}
	}

	[Editor(false)]
	public bool ClearSelectedOnRemoval
	{
		get
		{
			return _clearSelectedOnRemoval;
		}
		set
		{
			if (_clearSelectedOnRemoval != value)
			{
				_clearSelectedOnRemoval = value;
				OnPropertyChanged(value, "ClearSelectedOnRemoval");
			}
		}
	}

	public abstract Vector2 GetDropGizmoPosition(Vector2 draggedWidgetPosition);

	public abstract int GetIndexForDrop(Vector2 draggedWidgetPosition);

	protected Container(UIContext context)
		: base(context)
	{
		DefaultItemDescription = new ContainerItemDescription();
		_itemDescriptions = new List<ContainerItemDescription>();
	}

	private void UpdateSelected()
	{
		for (int i = 0; i < base.ChildCount; i++)
		{
			if (GetChild(i) is ButtonWidget buttonWidget)
			{
				bool isSelected = i == IntValue;
				buttonWidget.IsSelected = isSelected;
			}
		}
	}

	protected internal override bool OnDrop()
	{
		if (base.AcceptDrop)
		{
			bool flag = true;
			if (AcceptDropHandler != null)
			{
				flag = AcceptDropHandler(this, base.EventManager.DraggedWidget);
			}
			if (flag)
			{
				Widget widget = base.EventManager.ReleaseDraggedWidget();
				int indexForDrop = GetIndexForDrop(base.EventManager.MousePosition);
				if (!base.DropEventHandledManually)
				{
					widget.ParentWidget = this;
					widget.SetSiblingIndex(indexForDrop);
				}
				EventFired("Drop", widget, indexForDrop);
				return true;
			}
		}
		return false;
	}

	public abstract void OnChildSelected(Widget widget);

	public ContainerItemDescription GetItemDescription(string id, int index)
	{
		bool flag = !string.IsNullOrEmpty(id);
		ContainerItemDescription containerItemDescription = null;
		ContainerItemDescription containerItemDescription2 = null;
		for (int i = 0; i < _itemDescriptions.Count; i++)
		{
			ContainerItemDescription containerItemDescription3 = _itemDescriptions[i];
			if (flag && containerItemDescription3.WidgetId == id)
			{
				containerItemDescription = containerItemDescription3;
			}
			if (index == containerItemDescription3.WidgetIndex)
			{
				containerItemDescription2 = containerItemDescription3;
			}
		}
		return containerItemDescription ?? containerItemDescription2 ?? DefaultItemDescription;
	}

	protected override void OnChildAdded(Widget child)
	{
		foreach (Action<Widget, Widget> itemAddEventHandler in ItemAddEventHandlers)
		{
			itemAddEventHandler(this, child);
		}
		base.OnChildAdded(child);
	}

	protected override void OnBeforeChildRemoved(Widget child)
	{
		foreach (Action<Widget, Widget> itemRemoveEventHandler in ItemRemoveEventHandlers)
		{
			itemRemoveEventHandler(this, child);
		}
		base.OnBeforeChildRemoved(child);
	}

	protected override void OnAfterChildRemoved(Widget child, int previousIndexOfChild)
	{
		if (IntValue >= base.ChildCount)
		{
			if (ClearSelectedOnRemoval)
			{
				IntValue = -1;
			}
			else
			{
				IntValue = base.ChildCount - 1;
			}
		}
		else if (previousIndexOfChild >= 0 && IntValue >= 0)
		{
			if (IntValue == previousIndexOfChild && ClearSelectedOnRemoval)
			{
				IntValue = -1;
			}
			else if (previousIndexOfChild < IntValue)
			{
				IntValue--;
			}
		}
		foreach (Action<Widget> itemAfterRemoveEventHandler in ItemAfterRemoveEventHandlers)
		{
			itemAfterRemoveEventHandler(this);
		}
		base.OnAfterChildRemoved(child, previousIndexOfChild);
	}

	public void AddItemDescription(ContainerItemDescription itemDescription)
	{
		_itemDescriptions.Add(itemDescription);
	}

	public ScrollablePanel FindParentPanel()
	{
		for (Widget parentWidget = base.ParentWidget; parentWidget != null; parentWidget = parentWidget.ParentWidget)
		{
			if (parentWidget is ScrollablePanel result)
			{
				return result;
			}
		}
		return null;
	}
}
