using System;
using System.Runtime.InteropServices;

namespace System.Collections
{
	// Token: 0x0200048E RID: 1166
	[ComVisible(true)]
	[Serializable]
	public abstract class ReadOnlyCollectionBase : ICollection, IEnumerable
	{
		// Token: 0x17000836 RID: 2102
		// (get) Token: 0x060037B4 RID: 14260 RVA: 0x000D5D23 File Offset: 0x000D3F23
		protected ArrayList InnerList
		{
			get
			{
				if (this.list == null)
				{
					this.list = new ArrayList();
				}
				return this.list;
			}
		}

		// Token: 0x17000837 RID: 2103
		// (get) Token: 0x060037B5 RID: 14261 RVA: 0x000D5D3E File Offset: 0x000D3F3E
		public virtual int Count
		{
			get
			{
				return this.InnerList.Count;
			}
		}

		// Token: 0x17000838 RID: 2104
		// (get) Token: 0x060037B6 RID: 14262 RVA: 0x000D5D4B File Offset: 0x000D3F4B
		bool ICollection.IsSynchronized
		{
			get
			{
				return this.InnerList.IsSynchronized;
			}
		}

		// Token: 0x17000839 RID: 2105
		// (get) Token: 0x060037B7 RID: 14263 RVA: 0x000D5D58 File Offset: 0x000D3F58
		object ICollection.SyncRoot
		{
			get
			{
				return this.InnerList.SyncRoot;
			}
		}

		// Token: 0x060037B8 RID: 14264 RVA: 0x000D5D65 File Offset: 0x000D3F65
		void ICollection.CopyTo(Array array, int index)
		{
			this.InnerList.CopyTo(array, index);
		}

		// Token: 0x060037B9 RID: 14265 RVA: 0x000D5D74 File Offset: 0x000D3F74
		public virtual IEnumerator GetEnumerator()
		{
			return this.InnerList.GetEnumerator();
		}

		// Token: 0x040018BB RID: 6331
		private ArrayList list;
	}
}
