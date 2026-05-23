using System;
using System.Numerics;
using TaleWorlds.GauntletUI.Layout;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x0200005A RID: 90
	public class GridWidget : Container
	{
		// Token: 0x170001B4 RID: 436
		// (get) Token: 0x0600061D RID: 1565 RVA: 0x0001A82F File Offset: 0x00018A2F
		// (set) Token: 0x0600061E RID: 1566 RVA: 0x0001A837 File Offset: 0x00018A37
		public GridLayout GridLayout { get; private set; }

		// Token: 0x170001B5 RID: 437
		// (get) Token: 0x0600061F RID: 1567 RVA: 0x0001A840 File Offset: 0x00018A40
		// (set) Token: 0x06000620 RID: 1568 RVA: 0x0001A848 File Offset: 0x00018A48
		[Editor(false)]
		public float DefaultCellWidth
		{
			get
			{
				return this._defaultCellWidth;
			}
			set
			{
				if (this._defaultCellWidth != value)
				{
					this._defaultCellWidth = value;
					base.OnPropertyChanged(value, "DefaultCellWidth");
				}
			}
		}

		// Token: 0x170001B6 RID: 438
		// (get) Token: 0x06000621 RID: 1569 RVA: 0x0001A866 File Offset: 0x00018A66
		public float DefaultScaledCellWidth
		{
			get
			{
				return this.DefaultCellWidth * base._scaleToUse;
			}
		}

		// Token: 0x170001B7 RID: 439
		// (get) Token: 0x06000622 RID: 1570 RVA: 0x0001A875 File Offset: 0x00018A75
		// (set) Token: 0x06000623 RID: 1571 RVA: 0x0001A87D File Offset: 0x00018A7D
		[Editor(false)]
		public float DefaultCellHeight
		{
			get
			{
				return this._defaultCellHeight;
			}
			set
			{
				if (this._defaultCellHeight != value)
				{
					this._defaultCellHeight = value;
					base.OnPropertyChanged(value, "DefaultCellHeight");
				}
			}
		}

		// Token: 0x170001B8 RID: 440
		// (get) Token: 0x06000624 RID: 1572 RVA: 0x0001A89B File Offset: 0x00018A9B
		public float DefaultScaledCellHeight
		{
			get
			{
				return this.DefaultCellHeight * base._scaleToUse;
			}
		}

		// Token: 0x170001B9 RID: 441
		// (get) Token: 0x06000625 RID: 1573 RVA: 0x0001A8AA File Offset: 0x00018AAA
		// (set) Token: 0x06000626 RID: 1574 RVA: 0x0001A8B2 File Offset: 0x00018AB2
		[Editor(false)]
		public int RowCount
		{
			get
			{
				return this._rowCount;
			}
			set
			{
				if (this._rowCount != value)
				{
					this._rowCount = value;
					base.OnPropertyChanged(value, "RowCount");
				}
			}
		}

		// Token: 0x170001BA RID: 442
		// (get) Token: 0x06000627 RID: 1575 RVA: 0x0001A8D0 File Offset: 0x00018AD0
		// (set) Token: 0x06000628 RID: 1576 RVA: 0x0001A8D8 File Offset: 0x00018AD8
		[Editor(false)]
		public int ColumnCount
		{
			get
			{
				return this._columnCount;
			}
			set
			{
				if (this._columnCount != value)
				{
					this._columnCount = value;
					base.OnPropertyChanged(value, "ColumnCount");
				}
			}
		}

		// Token: 0x170001BB RID: 443
		// (get) Token: 0x06000629 RID: 1577 RVA: 0x0001A8F6 File Offset: 0x00018AF6
		// (set) Token: 0x0600062A RID: 1578 RVA: 0x0001A8FE File Offset: 0x00018AFE
		[Editor(false)]
		public bool UseDynamicCellWidth
		{
			get
			{
				return this._useDynamicCellWidth;
			}
			set
			{
				if (this._useDynamicCellWidth != value)
				{
					this._useDynamicCellWidth = value;
					base.OnPropertyChanged(value, "UseDynamicCellWidth");
				}
			}
		}

		// Token: 0x170001BC RID: 444
		// (get) Token: 0x0600062B RID: 1579 RVA: 0x0001A91C File Offset: 0x00018B1C
		// (set) Token: 0x0600062C RID: 1580 RVA: 0x0001A924 File Offset: 0x00018B24
		[Editor(false)]
		public bool UseDynamicCellHeight
		{
			get
			{
				return this._useDynamicCellHeight;
			}
			set
			{
				if (this._useDynamicCellHeight != value)
				{
					this._useDynamicCellHeight = value;
					base.OnPropertyChanged(value, "UseDynamicCellHeight");
				}
			}
		}

		// Token: 0x170001BD RID: 445
		// (get) Token: 0x0600062D RID: 1581 RVA: 0x0001A942 File Offset: 0x00018B42
		// (set) Token: 0x0600062E RID: 1582 RVA: 0x0001A94A File Offset: 0x00018B4A
		public override Predicate<Widget> AcceptDropPredicate { get; set; }

		// Token: 0x170001BE RID: 446
		// (get) Token: 0x0600062F RID: 1583 RVA: 0x0001A953 File Offset: 0x00018B53
		public override bool IsDragHovering
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000630 RID: 1584 RVA: 0x0001A956 File Offset: 0x00018B56
		public GridWidget(UIContext context)
			: base(context)
		{
			this.GridLayout = new GridLayout();
			base.LayoutImp = this.GridLayout;
			this.RowCount = -1;
			this.ColumnCount = -1;
		}

		// Token: 0x06000631 RID: 1585 RVA: 0x0001A984 File Offset: 0x00018B84
		public override Vector2 GetDropGizmoPosition(Vector2 draggedWidgetPosition)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000632 RID: 1586 RVA: 0x0001A98B File Offset: 0x00018B8B
		public override int GetIndexForDrop(Vector2 draggedWidgetPosition)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000633 RID: 1587 RVA: 0x0001A994 File Offset: 0x00018B94
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

		// Token: 0x040002E7 RID: 743
		private float _defaultCellWidth;

		// Token: 0x040002E8 RID: 744
		private float _defaultCellHeight;

		// Token: 0x040002E9 RID: 745
		private int _rowCount;

		// Token: 0x040002EA RID: 746
		private int _columnCount;

		// Token: 0x040002EB RID: 747
		private bool _useDynamicCellWidth;

		// Token: 0x040002EC RID: 748
		private bool _useDynamicCellHeight;

		// Token: 0x040002ED RID: 749
		public const int DefaultRowCount = 3;

		// Token: 0x040002EE RID: 750
		public const int DefaultColumnCount = 3;
	}
}
