using System;
using System.Numerics;
using TaleWorlds.GauntletUI.Layout;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x0200005E RID: 94
	public class ListPanel : Container
	{
		// Token: 0x170001C6 RID: 454
		// (get) Token: 0x0600064F RID: 1615 RVA: 0x0001B430 File Offset: 0x00019630
		// (set) Token: 0x06000650 RID: 1616 RVA: 0x0001B438 File Offset: 0x00019638
		public StackLayout StackLayout { get; private set; }

		// Token: 0x06000651 RID: 1617 RVA: 0x0001B441 File Offset: 0x00019641
		public ListPanel(UIContext context)
			: base(context)
		{
			this.StackLayout = new StackLayout();
			base.LayoutImp = this.StackLayout;
		}

		// Token: 0x06000652 RID: 1618 RVA: 0x0001B461 File Offset: 0x00019661
		protected override void OnUpdate(float dt)
		{
			base.OnUpdate(dt);
			this.UpdateListPanel();
			if (this.ResetSelectedOnLosingFocus && !base.CheckIsMyChildRecursive(base.EventManager.LatestMouseDownWidget))
			{
				base.IntValue = -1;
			}
		}

		// Token: 0x06000653 RID: 1619 RVA: 0x0001B492 File Offset: 0x00019692
		private void UpdateListPanel()
		{
			if (base.AcceptDrop && this.IsDragHovering)
			{
				base.DragHoverInsertionIndex = this.GetIndexForDrop(base.EventManager.DraggedWidgetPosition);
			}
		}

		// Token: 0x170001C7 RID: 455
		// (get) Token: 0x06000654 RID: 1620 RVA: 0x0001B4BB File Offset: 0x000196BB
		// (set) Token: 0x06000655 RID: 1621 RVA: 0x0001B4C3 File Offset: 0x000196C3
		public override Predicate<Widget> AcceptDropPredicate { get; set; }

		// Token: 0x06000656 RID: 1622 RVA: 0x0001B4CC File Offset: 0x000196CC
		public override int GetIndexForDrop(Vector2 draggedWidgetPosition)
		{
			return this.StackLayout.GetIndexForDrop(this, draggedWidgetPosition);
		}

		// Token: 0x06000657 RID: 1623 RVA: 0x0001B4DB File Offset: 0x000196DB
		public override Vector2 GetDropGizmoPosition(Vector2 draggedWidgetPosition)
		{
			return this.StackLayout.GetDropGizmoPosition(this, draggedWidgetPosition);
		}

		// Token: 0x06000658 RID: 1624 RVA: 0x0001B4EC File Offset: 0x000196EC
		public override void OnChildSelected(Widget widget)
		{
			int intValue = -1;
			for (int i = 0; i < base.ChildCount; i++)
			{
				if (widget == base.GetChild(i))
				{
					intValue = i;
				}
			}
			base.IntValue = intValue;
		}

		// Token: 0x06000659 RID: 1625 RVA: 0x0001B51F File Offset: 0x0001971F
		protected internal override void OnDragHoverBegin()
		{
			this._dragHovering = true;
			base.SetMeasureAndLayoutDirty();
		}

		// Token: 0x0600065A RID: 1626 RVA: 0x0001B52E File Offset: 0x0001972E
		protected internal override void OnDragHoverEnd()
		{
			this._dragHovering = false;
			base.SetMeasureAndLayoutDirty();
		}

		// Token: 0x0600065B RID: 1627 RVA: 0x0001B53D File Offset: 0x0001973D
		protected override bool OnPreviewDragHover()
		{
			return base.AcceptDrop;
		}

		// Token: 0x170001C8 RID: 456
		// (get) Token: 0x0600065C RID: 1628 RVA: 0x0001B545 File Offset: 0x00019745
		public override bool IsDragHovering
		{
			get
			{
				return this._dragHovering;
			}
		}

		// Token: 0x170001C9 RID: 457
		// (get) Token: 0x0600065D RID: 1629 RVA: 0x0001B54D File Offset: 0x0001974D
		// (set) Token: 0x0600065E RID: 1630 RVA: 0x0001B555 File Offset: 0x00019755
		[Editor(false)]
		public bool ResetSelectedOnLosingFocus
		{
			get
			{
				return this._resetSelectedOnLosingFocus;
			}
			set
			{
				if (this._resetSelectedOnLosingFocus != value)
				{
					this._resetSelectedOnLosingFocus = value;
					base.OnPropertyChanged(value, "ResetSelectedOnLosingFocus");
				}
			}
		}

		// Token: 0x040002F7 RID: 759
		private bool _dragHovering;

		// Token: 0x040002F8 RID: 760
		private bool _resetSelectedOnLosingFocus;
	}
}
