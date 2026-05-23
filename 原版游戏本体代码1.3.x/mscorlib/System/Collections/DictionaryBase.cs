using System;
using System.Runtime.InteropServices;

namespace System.Collections
{
	// Token: 0x0200048D RID: 1165
	[ComVisible(true)]
	[Serializable]
	public abstract class DictionaryBase : IDictionary, ICollection, IEnumerable
	{
		// Token: 0x1700082C RID: 2092
		// (get) Token: 0x06003797 RID: 14231 RVA: 0x000D5ABD File Offset: 0x000D3CBD
		protected Hashtable InnerHashtable
		{
			get
			{
				if (this.hashtable == null)
				{
					this.hashtable = new Hashtable();
				}
				return this.hashtable;
			}
		}

		// Token: 0x1700082D RID: 2093
		// (get) Token: 0x06003798 RID: 14232 RVA: 0x000D5AD8 File Offset: 0x000D3CD8
		protected IDictionary Dictionary
		{
			get
			{
				return this;
			}
		}

		// Token: 0x1700082E RID: 2094
		// (get) Token: 0x06003799 RID: 14233 RVA: 0x000D5ADB File Offset: 0x000D3CDB
		public int Count
		{
			get
			{
				if (this.hashtable != null)
				{
					return this.hashtable.Count;
				}
				return 0;
			}
		}

		// Token: 0x1700082F RID: 2095
		// (get) Token: 0x0600379A RID: 14234 RVA: 0x000D5AF2 File Offset: 0x000D3CF2
		bool IDictionary.IsReadOnly
		{
			get
			{
				return this.InnerHashtable.IsReadOnly;
			}
		}

		// Token: 0x17000830 RID: 2096
		// (get) Token: 0x0600379B RID: 14235 RVA: 0x000D5AFF File Offset: 0x000D3CFF
		bool IDictionary.IsFixedSize
		{
			get
			{
				return this.InnerHashtable.IsFixedSize;
			}
		}

		// Token: 0x17000831 RID: 2097
		// (get) Token: 0x0600379C RID: 14236 RVA: 0x000D5B0C File Offset: 0x000D3D0C
		bool ICollection.IsSynchronized
		{
			get
			{
				return this.InnerHashtable.IsSynchronized;
			}
		}

		// Token: 0x17000832 RID: 2098
		// (get) Token: 0x0600379D RID: 14237 RVA: 0x000D5B19 File Offset: 0x000D3D19
		ICollection IDictionary.Keys
		{
			get
			{
				return this.InnerHashtable.Keys;
			}
		}

		// Token: 0x17000833 RID: 2099
		// (get) Token: 0x0600379E RID: 14238 RVA: 0x000D5B26 File Offset: 0x000D3D26
		object ICollection.SyncRoot
		{
			get
			{
				return this.InnerHashtable.SyncRoot;
			}
		}

		// Token: 0x17000834 RID: 2100
		// (get) Token: 0x0600379F RID: 14239 RVA: 0x000D5B33 File Offset: 0x000D3D33
		ICollection IDictionary.Values
		{
			get
			{
				return this.InnerHashtable.Values;
			}
		}

		// Token: 0x060037A0 RID: 14240 RVA: 0x000D5B40 File Offset: 0x000D3D40
		public void CopyTo(Array array, int index)
		{
			this.InnerHashtable.CopyTo(array, index);
		}

		// Token: 0x17000835 RID: 2101
		object IDictionary.this[object key]
		{
			get
			{
				object obj = this.InnerHashtable[key];
				this.OnGet(key, obj);
				return obj;
			}
			set
			{
				this.OnValidate(key, value);
				bool flag = true;
				object obj = this.InnerHashtable[key];
				if (obj == null)
				{
					flag = this.InnerHashtable.Contains(key);
				}
				this.OnSet(key, obj, value);
				this.InnerHashtable[key] = value;
				try
				{
					this.OnSetComplete(key, obj, value);
				}
				catch
				{
					if (flag)
					{
						this.InnerHashtable[key] = obj;
					}
					else
					{
						this.InnerHashtable.Remove(key);
					}
					throw;
				}
			}
		}

		// Token: 0x060037A3 RID: 14243 RVA: 0x000D5BFC File Offset: 0x000D3DFC
		bool IDictionary.Contains(object key)
		{
			return this.InnerHashtable.Contains(key);
		}

		// Token: 0x060037A4 RID: 14244 RVA: 0x000D5C0C File Offset: 0x000D3E0C
		void IDictionary.Add(object key, object value)
		{
			this.OnValidate(key, value);
			this.OnInsert(key, value);
			this.InnerHashtable.Add(key, value);
			try
			{
				this.OnInsertComplete(key, value);
			}
			catch
			{
				this.InnerHashtable.Remove(key);
				throw;
			}
		}

		// Token: 0x060037A5 RID: 14245 RVA: 0x000D5C60 File Offset: 0x000D3E60
		public void Clear()
		{
			this.OnClear();
			this.InnerHashtable.Clear();
			this.OnClearComplete();
		}

		// Token: 0x060037A6 RID: 14246 RVA: 0x000D5C7C File Offset: 0x000D3E7C
		void IDictionary.Remove(object key)
		{
			if (this.InnerHashtable.Contains(key))
			{
				object value = this.InnerHashtable[key];
				this.OnValidate(key, value);
				this.OnRemove(key, value);
				this.InnerHashtable.Remove(key);
				try
				{
					this.OnRemoveComplete(key, value);
				}
				catch
				{
					this.InnerHashtable.Add(key, value);
					throw;
				}
			}
		}

		// Token: 0x060037A7 RID: 14247 RVA: 0x000D5CEC File Offset: 0x000D3EEC
		public IDictionaryEnumerator GetEnumerator()
		{
			return this.InnerHashtable.GetEnumerator();
		}

		// Token: 0x060037A8 RID: 14248 RVA: 0x000D5CF9 File Offset: 0x000D3EF9
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.InnerHashtable.GetEnumerator();
		}

		// Token: 0x060037A9 RID: 14249 RVA: 0x000D5D06 File Offset: 0x000D3F06
		protected virtual object OnGet(object key, object currentValue)
		{
			return currentValue;
		}

		// Token: 0x060037AA RID: 14250 RVA: 0x000D5D09 File Offset: 0x000D3F09
		protected virtual void OnSet(object key, object oldValue, object newValue)
		{
		}

		// Token: 0x060037AB RID: 14251 RVA: 0x000D5D0B File Offset: 0x000D3F0B
		protected virtual void OnInsert(object key, object value)
		{
		}

		// Token: 0x060037AC RID: 14252 RVA: 0x000D5D0D File Offset: 0x000D3F0D
		protected virtual void OnClear()
		{
		}

		// Token: 0x060037AD RID: 14253 RVA: 0x000D5D0F File Offset: 0x000D3F0F
		protected virtual void OnRemove(object key, object value)
		{
		}

		// Token: 0x060037AE RID: 14254 RVA: 0x000D5D11 File Offset: 0x000D3F11
		protected virtual void OnValidate(object key, object value)
		{
		}

		// Token: 0x060037AF RID: 14255 RVA: 0x000D5D13 File Offset: 0x000D3F13
		protected virtual void OnSetComplete(object key, object oldValue, object newValue)
		{
		}

		// Token: 0x060037B0 RID: 14256 RVA: 0x000D5D15 File Offset: 0x000D3F15
		protected virtual void OnInsertComplete(object key, object value)
		{
		}

		// Token: 0x060037B1 RID: 14257 RVA: 0x000D5D17 File Offset: 0x000D3F17
		protected virtual void OnClearComplete()
		{
		}

		// Token: 0x060037B2 RID: 14258 RVA: 0x000D5D19 File Offset: 0x000D3F19
		protected virtual void OnRemoveComplete(object key, object value)
		{
		}

		// Token: 0x040018BA RID: 6330
		private Hashtable hashtable;
	}
}
