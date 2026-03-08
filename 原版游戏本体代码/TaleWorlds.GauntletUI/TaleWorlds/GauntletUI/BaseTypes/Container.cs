using System;
using System.Collections.Generic;
using System.Numerics;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x02000055 RID: 85
	public abstract class Container : Widget
	{
		// Token: 0x1700019B RID: 411
		// (get) Token: 0x060005A8 RID: 1448 RVA: 0x000178E0 File Offset: 0x00015AE0
		// (set) Token: 0x060005A9 RID: 1449 RVA: 0x000178E8 File Offset: 0x00015AE8
		public ContainerItemDescription DefaultItemDescription { get; private set; }

		// Token: 0x1700019C RID: 412
		// (get) Token: 0x060005AA RID: 1450
		// (set) Token: 0x060005AB RID: 1451
		public abstract Predicate<Widget> AcceptDropPredicate { get; set; }

		// Token: 0x060005AC RID: 1452
		public abstract Vector2 GetDropGizmoPosition(Vector2 draggedWidgetPosition);

		// Token: 0x060005AD RID: 1453
		public abstract int GetIndexForDrop(Vector2 draggedWidgetPosition);

		// Token: 0x1700019D RID: 413
		// (get) Token: 0x060005AE RID: 1454 RVA: 0x000178F1 File Offset: 0x00015AF1
		// (set) Token: 0x060005AF RID: 1455 RVA: 0x000178FC File Offset: 0x00015AFC
		public int IntValue
		{
			get
			{
				return this._intValue;
			}
			set
			{
				if (!this._currentlyChangingIntValue)
				{
					this._currentlyChangingIntValue = true;
					if (value != this._intValue && value < base.ChildCount)
					{
						this._intValue = value;
						this.UpdateSelected();
						foreach (Action<Widget> action in this.SelectEventHandlers)
						{
							action(this);
						}
						base.EventFired("SelectedItemChange", Array.Empty<object>());
						base.OnPropertyChanged(value, "IntValue");
					}
					this._currentlyChangingIntValue = false;
				}
			}
		}

		// Token: 0x1700019E RID: 414
		// (get) Token: 0x060005B0 RID: 1456
		public abstract bool IsDragHovering { get; }

		// Token: 0x1700019F RID: 415
		// (get) Token: 0x060005B1 RID: 1457 RVA: 0x000179A0 File Offset: 0x00015BA0
		// (set) Token: 0x060005B2 RID: 1458 RVA: 0x000179A8 File Offset: 0x00015BA8
		public int DragHoverInsertionIndex
		{
			get
			{
				return this._dragHoverInsertionIndex;
			}
			set
			{
				if (this._dragHoverInsertionIndex != value)
				{
					this._dragHoverInsertionIndex = value;
					base.SetMeasureAndLayoutDirty();
				}
			}
		}

		// Token: 0x060005B3 RID: 1459 RVA: 0x000179C0 File Offset: 0x00015BC0
		protected Container(UIContext context)
			: base(context)
		{
			this.DefaultItemDescription = new ContainerItemDescription();
			this._itemDescriptions = new List<ContainerItemDescription>();
		}

		// Token: 0x060005B4 RID: 1460 RVA: 0x00017A20 File Offset: 0x00015C20
		private void UpdateSelected()
		{
			for (int i = 0; i < base.ChildCount; i++)
			{
				ButtonWidget buttonWidget = base.GetChild(i) as ButtonWidget;
				if (buttonWidget != null)
				{
					bool isSelected = i == this.IntValue;
					buttonWidget.IsSelected = isSelected;
				}
			}
		}

		// Token: 0x060005B5 RID: 1461 RVA: 0x00017A60 File Offset: 0x00015C60
		protected internal override bool OnDrop()
		{
			if (base.AcceptDrop)
			{
				bool flag = true;
				if (this.AcceptDropHandler != null)
				{
					flag = this.AcceptDropHandler(this, base.EventManager.DraggedWidget);
				}
				if (flag)
				{
					Widget widget = base.EventManager.ReleaseDraggedWidget();
					int indexForDrop = this.GetIndexForDrop(base.EventManager.DraggedWidgetPosition);
					if (!base.DropEventHandledManually)
					{
						widget.ParentWidget = this;
						widget.SetSiblingIndex(indexForDrop, false);
					}
					base.EventFired("Drop", new object[] { widget, indexForDrop });
					return true;
				}
			}
			return false;
		}

		// Token: 0x060005B6 RID: 1462
		public abstract void OnChildSelected(Widget widget);

		// Token: 0x060005B7 RID: 1463 RVA: 0x00017AF0 File Offset: 0x00015CF0
		public ContainerItemDescription GetItemDescription(string id, int index)
		{
			bool flag = !string.IsNullOrEmpty(id);
			ContainerItemDescription containerItemDescription = null;
			ContainerItemDescription containerItemDescription2 = null;
			for (int i = 0; i < this._itemDescriptions.Count; i++)
			{
				ContainerItemDescription containerItemDescription3 = this._itemDescriptions[i];
				if (flag && containerItemDescription3.WidgetId == id)
				{
					containerItemDescription = containerItemDescription3;
				}
				if (index == containerItemDescription3.WidgetIndex)
				{
					containerItemDescription2 = containerItemDescription3;
				}
			}
			ContainerItemDescription result;
			if ((result = containerItemDescription) == null)
			{
				result = containerItemDescription2 ?? this.DefaultItemDescription;
			}
			return result;
		}

		// Token: 0x060005B8 RID: 1464 RVA: 0x00017B64 File Offset: 0x00015D64
		protected override void OnChildAdded(Widget child)
		{
			foreach (Action<Widget, Widget> action in this.ItemAddEventHandlers)
			{
				action(this, child);
			}
			base.OnChildAdded(child);
		}

		// Token: 0x060005B9 RID: 1465 RVA: 0x00017BC0 File Offset: 0x00015DC0
		protected override void OnBeforeChildRemoved(Widget child)
		{
			foreach (Action<Widget, Widget> action in this.ItemRemoveEventHandlers)
			{
				action(this, child);
			}
			base.OnBeforeChildRemoved(child);
		}

		// Token: 0x060005BA RID: 1466 RVA: 0x00017C1C File Offset: 0x00015E1C
		protected override void OnAfterChildRemoved(Widget child, int previousIndexOfChild)
		{
			if (this.IntValue >= base.ChildCount)
			{
				if (this.ClearSelectedOnRemoval)
				{
					this.IntValue = -1;
				}
				else
				{
					this.IntValue = base.ChildCount - 1;
				}
			}
			else if (previousIndexOfChild >= 0 && this.IntValue >= 0)
			{
				if (this.IntValue == previousIndexOfChild && this.ClearSelectedOnRemoval)
				{
					this.IntValue = -1;
				}
				else if (previousIndexOfChild < this.IntValue)
				{
					int intValue = this.IntValue;
					this.IntValue = intValue - 1;
				}
			}
			foreach (Action<Widget> action in this.ItemAfterRemoveEventHandlers)
			{
				action(this);
			}
			base.OnAfterChildRemoved(child, previousIndexOfChild);
		}

		// Token: 0x060005BB RID: 1467 RVA: 0x00017CE4 File Offset: 0x00015EE4
		public void AddItemDescription(ContainerItemDescription itemDescription)
		{
			this._itemDescriptions.Add(itemDescription);
		}

		// Token: 0x060005BC RID: 1468 RVA: 0x00017CF4 File Offset: 0x00015EF4
		public ScrollablePanel FindParentPanel()
		{
			for (Widget parentWidget = base.ParentWidget; parentWidget != null; parentWidget = parentWidget.ParentWidget)
			{
				ScrollablePanel result;
				if ((result = parentWidget as ScrollablePanel) != null)
				{
					return result;
				}
			}
			return null;
		}

		// Token: 0x170001A0 RID: 416
		// (get) Token: 0x060005BD RID: 1469 RVA: 0x00017D21 File Offset: 0x00015F21
		// (set) Token: 0x060005BE RID: 1470 RVA: 0x00017D29 File Offset: 0x00015F29
		[Editor(false)]
		public bool ClearSelectedOnRemoval
		{
			get
			{
				return this._clearSelectedOnRemoval;
			}
			set
			{
				if (this._clearSelectedOnRemoval != value)
				{
					this._clearSelectedOnRemoval = value;
					base.OnPropertyChanged(value, "ClearSelectedOnRemoval");
				}
			}
		}

		// Token: 0x040002AF RID: 687
		public List<Action<Widget>> SelectEventHandlers = new List<Action<Widget>>();

		// Token: 0x040002B0 RID: 688
		public List<Action<Widget, Widget>> ItemAddEventHandlers = new List<Action<Widget, Widget>>();

		// Token: 0x040002B1 RID: 689
		public List<Action<Widget, Widget>> ItemRemoveEventHandlers = new List<Action<Widget, Widget>>();

		// Token: 0x040002B2 RID: 690
		public List<Action<Widget>> ItemAfterRemoveEventHandlers = new List<Action<Widget>>();

		// Token: 0x040002B3 RID: 691
		private int _intValue = -1;

		// Token: 0x040002B4 RID: 692
		private bool _currentlyChangingIntValue;

		// Token: 0x040002B5 RID: 693
		public bool ShowSelection;

		// Token: 0x040002B6 RID: 694
		private int _dragHoverInsertionIndex;

		// Token: 0x040002B7 RID: 695
		private List<ContainerItemDescription> _itemDescriptions;

		// Token: 0x040002B8 RID: 696
		private bool _clearSelectedOnRemoval;
	}
}
