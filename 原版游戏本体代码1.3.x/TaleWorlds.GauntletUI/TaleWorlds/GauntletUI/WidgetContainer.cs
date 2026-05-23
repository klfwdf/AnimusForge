using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x0200003A RID: 58
	internal class WidgetContainer
	{
		// Token: 0x17000134 RID: 308
		// (get) Token: 0x060003E6 RID: 998 RVA: 0x0000FB25 File Offset: 0x0000DD25
		internal int Count
		{
			get
			{
				return this.GetActiveList().Count;
			}
		}

		// Token: 0x060003E7 RID: 999 RVA: 0x0000FB32 File Offset: 0x0000DD32
		internal WidgetContainer(UIContext context, int initialCapacity, WidgetContainer.ContainerType containerType)
		{
			this._containerType = containerType;
			this._emptyWidget = new EmptyWidget(context);
			this._backList = new HashSet<Widget>();
			this._frontList = new MBList<Widget>(initialCapacity);
		}

		// Token: 0x060003E8 RID: 1000 RVA: 0x0000FB64 File Offset: 0x0000DD64
		internal void Add(Widget widget)
		{
			this._backList.Add(widget);
			this._isFragmented = true;
		}

		// Token: 0x060003E9 RID: 1001 RVA: 0x0000FB7A File Offset: 0x0000DD7A
		internal void Remove(Widget widget)
		{
			this._backList.Remove(widget);
			this._isFragmented = true;
		}

		// Token: 0x060003EA RID: 1002 RVA: 0x0000FB90 File Offset: 0x0000DD90
		public void Clear()
		{
			this._backList.Clear();
			this._frontList.Clear();
			this._backList = null;
			this._frontList = null;
			this._isFragmented = true;
		}

		// Token: 0x060003EB RID: 1003 RVA: 0x0000FBBD File Offset: 0x0000DDBD
		public MBReadOnlyList<Widget> GetActiveList()
		{
			return this._frontList;
		}

		// Token: 0x060003EC RID: 1004 RVA: 0x0000FBC8 File Offset: 0x0000DDC8
		public void Defrag()
		{
			if (!this._isFragmented)
			{
				return;
			}
			this._frontList.Clear();
			int num = 0;
			foreach (Widget widget in this._backList)
			{
				if (widget != this._emptyWidget)
				{
					this._frontList.Add(widget);
					num++;
				}
			}
			this._backList.Clear();
			for (int i = 0; i < this._frontList.Count; i++)
			{
				this._backList.Add(this._frontList[i]);
			}
			this._isFragmented = false;
		}

		// Token: 0x040001E9 RID: 489
		private HashSet<Widget> _backList;

		// Token: 0x040001EA RID: 490
		private MBList<Widget> _frontList;

		// Token: 0x040001EB RID: 491
		private EmptyWidget _emptyWidget;

		// Token: 0x040001EC RID: 492
		private readonly WidgetContainer.ContainerType _containerType;

		// Token: 0x040001ED RID: 493
		private bool _isFragmented;

		// Token: 0x02000083 RID: 131
		internal enum ContainerType
		{
			// Token: 0x0400045C RID: 1116
			Update,
			// Token: 0x0400045D RID: 1117
			ParallelUpdate,
			// Token: 0x0400045E RID: 1118
			LateUpdate,
			// Token: 0x0400045F RID: 1119
			VisualDefinition,
			// Token: 0x04000460 RID: 1120
			TweenPosition,
			// Token: 0x04000461 RID: 1121
			UpdateBrushes
		}
	}
}
