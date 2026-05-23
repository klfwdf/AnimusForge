using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Collections.ObjectModel
{
	// Token: 0x020004B9 RID: 1209
	[ComVisible(false)]
	[DebuggerTypeProxy(typeof(Mscorlib_KeyedCollectionDebugView<, >))]
	[DebuggerDisplay("Count = {Count}")]
	[__DynamicallyInvokable]
	[Serializable]
	public abstract class KeyedCollection<TKey, TItem> : Collection<TItem>
	{
		// Token: 0x06003A13 RID: 14867 RVA: 0x000DD870 File Offset: 0x000DBA70
		[__DynamicallyInvokable]
		protected KeyedCollection()
			: this(null, 0)
		{
		}

		// Token: 0x06003A14 RID: 14868 RVA: 0x000DD87A File Offset: 0x000DBA7A
		[__DynamicallyInvokable]
		protected KeyedCollection(IEqualityComparer<TKey> comparer)
			: this(comparer, 0)
		{
		}

		// Token: 0x06003A15 RID: 14869 RVA: 0x000DD884 File Offset: 0x000DBA84
		[__DynamicallyInvokable]
		protected KeyedCollection(IEqualityComparer<TKey> comparer, int dictionaryCreationThreshold)
		{
			if (comparer == null)
			{
				comparer = EqualityComparer<TKey>.Default;
			}
			if (dictionaryCreationThreshold == -1)
			{
				dictionaryCreationThreshold = int.MaxValue;
			}
			if (dictionaryCreationThreshold < -1)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.dictionaryCreationThreshold, ExceptionResource.ArgumentOutOfRange_InvalidThreshold);
			}
			this.comparer = comparer;
			this.threshold = dictionaryCreationThreshold;
		}

		// Token: 0x170008CB RID: 2251
		// (get) Token: 0x06003A16 RID: 14870 RVA: 0x000DD8BB File Offset: 0x000DBABB
		[__DynamicallyInvokable]
		public IEqualityComparer<TKey> Comparer
		{
			[__DynamicallyInvokable]
			get
			{
				return this.comparer;
			}
		}

		// Token: 0x170008CC RID: 2252
		[__DynamicallyInvokable]
		public TItem this[TKey key]
		{
			[__DynamicallyInvokable]
			get
			{
				if (key == null)
				{
					ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
				}
				if (this.dict != null)
				{
					return this.dict[key];
				}
				foreach (TItem titem in base.Items)
				{
					if (this.comparer.Equals(this.GetKeyForItem(titem), key))
					{
						return titem;
					}
				}
				ThrowHelper.ThrowKeyNotFoundException();
				return default(TItem);
			}
		}

		// Token: 0x06003A18 RID: 14872 RVA: 0x000DD958 File Offset: 0x000DBB58
		[__DynamicallyInvokable]
		public bool Contains(TKey key)
		{
			if (key == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
			}
			if (this.dict != null)
			{
				return this.dict.ContainsKey(key);
			}
			if (key != null)
			{
				foreach (TItem item in base.Items)
				{
					if (this.comparer.Equals(this.GetKeyForItem(item), key))
					{
						return true;
					}
				}
				return false;
			}
			return false;
		}

		// Token: 0x06003A19 RID: 14873 RVA: 0x000DD9E8 File Offset: 0x000DBBE8
		private bool ContainsItem(TItem item)
		{
			TKey keyForItem;
			if (this.dict == null || (keyForItem = this.GetKeyForItem(item)) == null)
			{
				return base.Items.Contains(item);
			}
			TItem x;
			bool flag = this.dict.TryGetValue(keyForItem, out x);
			return flag && EqualityComparer<TItem>.Default.Equals(x, item);
		}

		// Token: 0x06003A1A RID: 14874 RVA: 0x000DDA3C File Offset: 0x000DBC3C
		[__DynamicallyInvokable]
		public bool Remove(TKey key)
		{
			if (key == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
			}
			if (this.dict != null)
			{
				return this.dict.ContainsKey(key) && base.Remove(this.dict[key]);
			}
			if (key != null)
			{
				for (int i = 0; i < base.Items.Count; i++)
				{
					if (this.comparer.Equals(this.GetKeyForItem(base.Items[i]), key))
					{
						this.RemoveItem(i);
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x170008CD RID: 2253
		// (get) Token: 0x06003A1B RID: 14875 RVA: 0x000DDACA File Offset: 0x000DBCCA
		[__DynamicallyInvokable]
		protected IDictionary<TKey, TItem> Dictionary
		{
			[__DynamicallyInvokable]
			get
			{
				return this.dict;
			}
		}

		// Token: 0x06003A1C RID: 14876 RVA: 0x000DDAD4 File Offset: 0x000DBCD4
		[__DynamicallyInvokable]
		protected void ChangeItemKey(TItem item, TKey newKey)
		{
			if (!this.ContainsItem(item))
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_ItemNotExist);
			}
			TKey keyForItem = this.GetKeyForItem(item);
			if (!this.comparer.Equals(keyForItem, newKey))
			{
				if (newKey != null)
				{
					this.AddKey(newKey, item);
				}
				if (keyForItem != null)
				{
					this.RemoveKey(keyForItem);
				}
			}
		}

		// Token: 0x06003A1D RID: 14877 RVA: 0x000DDB27 File Offset: 0x000DBD27
		[__DynamicallyInvokable]
		protected override void ClearItems()
		{
			base.ClearItems();
			if (this.dict != null)
			{
				this.dict.Clear();
			}
			this.keyCount = 0;
		}

		// Token: 0x06003A1E RID: 14878
		[__DynamicallyInvokable]
		protected abstract TKey GetKeyForItem(TItem item);

		// Token: 0x06003A1F RID: 14879 RVA: 0x000DDB4C File Offset: 0x000DBD4C
		[__DynamicallyInvokable]
		protected override void InsertItem(int index, TItem item)
		{
			TKey keyForItem = this.GetKeyForItem(item);
			if (keyForItem != null)
			{
				this.AddKey(keyForItem, item);
			}
			base.InsertItem(index, item);
		}

		// Token: 0x06003A20 RID: 14880 RVA: 0x000DDB7C File Offset: 0x000DBD7C
		[__DynamicallyInvokable]
		protected override void RemoveItem(int index)
		{
			TKey keyForItem = this.GetKeyForItem(base.Items[index]);
			if (keyForItem != null)
			{
				this.RemoveKey(keyForItem);
			}
			base.RemoveItem(index);
		}

		// Token: 0x06003A21 RID: 14881 RVA: 0x000DDBB4 File Offset: 0x000DBDB4
		[__DynamicallyInvokable]
		protected override void SetItem(int index, TItem item)
		{
			TKey keyForItem = this.GetKeyForItem(item);
			TKey keyForItem2 = this.GetKeyForItem(base.Items[index]);
			if (this.comparer.Equals(keyForItem2, keyForItem))
			{
				if (keyForItem != null && this.dict != null)
				{
					this.dict[keyForItem] = item;
				}
			}
			else
			{
				if (keyForItem != null)
				{
					this.AddKey(keyForItem, item);
				}
				if (keyForItem2 != null)
				{
					this.RemoveKey(keyForItem2);
				}
			}
			base.SetItem(index, item);
		}

		// Token: 0x06003A22 RID: 14882 RVA: 0x000DDC34 File Offset: 0x000DBE34
		private void AddKey(TKey key, TItem item)
		{
			if (this.dict != null)
			{
				this.dict.Add(key, item);
				return;
			}
			if (this.keyCount == this.threshold)
			{
				this.CreateDictionary();
				this.dict.Add(key, item);
				return;
			}
			if (this.Contains(key))
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_AddingDuplicate);
			}
			this.keyCount++;
		}

		// Token: 0x06003A23 RID: 14883 RVA: 0x000DDC98 File Offset: 0x000DBE98
		private void CreateDictionary()
		{
			this.dict = new Dictionary<TKey, TItem>(this.comparer);
			foreach (TItem titem in base.Items)
			{
				TKey keyForItem = this.GetKeyForItem(titem);
				if (keyForItem != null)
				{
					this.dict.Add(keyForItem, titem);
				}
			}
		}

		// Token: 0x06003A24 RID: 14884 RVA: 0x000DDD0C File Offset: 0x000DBF0C
		private void RemoveKey(TKey key)
		{
			if (this.dict != null)
			{
				this.dict.Remove(key);
				return;
			}
			this.keyCount--;
		}

		// Token: 0x0400193C RID: 6460
		private const int defaultThreshold = 0;

		// Token: 0x0400193D RID: 6461
		private IEqualityComparer<TKey> comparer;

		// Token: 0x0400193E RID: 6462
		private Dictionary<TKey, TItem> dict;

		// Token: 0x0400193F RID: 6463
		private int keyCount;

		// Token: 0x04001940 RID: 6464
		private int threshold;
	}
}
