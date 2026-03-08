using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TaleWorlds.Library
{
	// Token: 0x02000068 RID: 104
	public class MBBindingList<T> : Collection<T>, IMBBindingList, IList, ICollection, IEnumerable
	{
		// Token: 0x06000344 RID: 836 RVA: 0x0000BFA0 File Offset: 0x0000A1A0
		public MBBindingList()
			: base(new List<T>(64))
		{
			this._list = (List<T>)base.Items;
		}

		// Token: 0x14000014 RID: 20
		// (add) Token: 0x06000345 RID: 837 RVA: 0x0000BFC0 File Offset: 0x0000A1C0
		// (remove) Token: 0x06000346 RID: 838 RVA: 0x0000BFE1 File Offset: 0x0000A1E1
		public event ListChangedEventHandler ListChanged
		{
			add
			{
				if (this._eventHandlers == null)
				{
					this._eventHandlers = new List<ListChangedEventHandler>();
				}
				this._eventHandlers.Add(value);
			}
			remove
			{
				if (this._eventHandlers != null)
				{
					this._eventHandlers.Remove(value);
				}
			}
		}

		// Token: 0x06000347 RID: 839 RVA: 0x0000BFF8 File Offset: 0x0000A1F8
		protected override void ClearItems()
		{
			base.ClearItems();
			this.FireListChanged(ListChangedType.Reset, -1);
		}

		// Token: 0x06000348 RID: 840 RVA: 0x0000C008 File Offset: 0x0000A208
		protected override void InsertItem(int index, T item)
		{
			base.InsertItem(index, item);
			this.FireListChanged(ListChangedType.ItemAdded, index);
		}

		// Token: 0x06000349 RID: 841 RVA: 0x0000C01A File Offset: 0x0000A21A
		protected override void RemoveItem(int index)
		{
			this.FireListChanged(ListChangedType.ItemBeforeDeleted, index);
			base.RemoveItem(index);
			this.FireListChanged(ListChangedType.ItemDeleted, index);
		}

		// Token: 0x0600034A RID: 842 RVA: 0x0000C033 File Offset: 0x0000A233
		protected override void SetItem(int index, T item)
		{
			base.SetItem(index, item);
			this.FireListChanged(ListChangedType.ItemChanged, index);
		}

		// Token: 0x0600034B RID: 843 RVA: 0x0000C045 File Offset: 0x0000A245
		private void FireListChanged(ListChangedType type, int index)
		{
			this.OnListChanged(new ListChangedEventArgs(type, index));
		}

		// Token: 0x0600034C RID: 844 RVA: 0x0000C054 File Offset: 0x0000A254
		protected virtual void OnListChanged(ListChangedEventArgs e)
		{
			if (this._eventHandlers != null)
			{
				foreach (ListChangedEventHandler listChangedEventHandler in this._eventHandlers)
				{
					listChangedEventHandler(this, e);
				}
			}
		}

		// Token: 0x0600034D RID: 845 RVA: 0x0000C0B0 File Offset: 0x0000A2B0
		public void Sort()
		{
			this._list.Sort();
			this.FireListChanged(ListChangedType.Sorted, -1);
		}

		// Token: 0x0600034E RID: 846 RVA: 0x0000C0C5 File Offset: 0x0000A2C5
		public void Sort(IComparer<T> comparer)
		{
			if (!this.IsOrdered(comparer))
			{
				this._list.Sort(comparer);
				this.FireListChanged(ListChangedType.Sorted, -1);
			}
		}

		// Token: 0x0600034F RID: 847 RVA: 0x0000C0E4 File Offset: 0x0000A2E4
		public bool IsOrdered(IComparer<T> comparer)
		{
			for (int i = 1; i < this._list.Count; i++)
			{
				if (comparer.Compare(this._list[i - 1], this._list[i]) == 1)
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000350 RID: 848 RVA: 0x0000C130 File Offset: 0x0000A330
		public void ApplyActionOnAllItems(Action<T> action)
		{
			for (int i = 0; i < this._list.Count; i++)
			{
				T obj = this._list[i];
				action(obj);
			}
		}

		// Token: 0x0400012F RID: 303
		private readonly List<T> _list;

		// Token: 0x04000130 RID: 304
		private List<ListChangedEventHandler> _eventHandlers;
	}
}
