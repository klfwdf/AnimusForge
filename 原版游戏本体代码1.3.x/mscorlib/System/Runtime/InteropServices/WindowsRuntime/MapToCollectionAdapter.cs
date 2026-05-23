using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009D8 RID: 2520
	internal sealed class MapToCollectionAdapter
	{
		// Token: 0x06006428 RID: 25640 RVA: 0x001558A7 File Offset: 0x00153AA7
		private MapToCollectionAdapter()
		{
		}

		// Token: 0x06006429 RID: 25641 RVA: 0x001558B0 File Offset: 0x00153AB0
		[SecurityCritical]
		internal int Count<K, V>()
		{
			object obj = JitHelpers.UnsafeCast<object>(this);
			IMap<K, V> map = obj as IMap<K, V>;
			if (map != null)
			{
				uint size = map.Size;
				if (2147483647U < size)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingDictionaryTooLarge"));
				}
				return (int)size;
			}
			else
			{
				IVector<KeyValuePair<K, V>> vector = JitHelpers.UnsafeCast<IVector<KeyValuePair<K, V>>>(this);
				uint size2 = vector.Size;
				if (2147483647U < size2)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
				}
				return (int)size2;
			}
		}

		// Token: 0x0600642A RID: 25642 RVA: 0x00155919 File Offset: 0x00153B19
		[SecurityCritical]
		internal bool IsReadOnly<K, V>()
		{
			return false;
		}

		// Token: 0x0600642B RID: 25643 RVA: 0x0015591C File Offset: 0x00153B1C
		[SecurityCritical]
		internal void Add<K, V>(KeyValuePair<K, V> item)
		{
			object obj = JitHelpers.UnsafeCast<object>(this);
			IDictionary<K, V> dictionary = obj as IDictionary<K, V>;
			if (dictionary != null)
			{
				dictionary.Add(item.Key, item.Value);
				return;
			}
			IVector<KeyValuePair<K, V>> vector = JitHelpers.UnsafeCast<IVector<KeyValuePair<K, V>>>(this);
			vector.Append(item);
		}

		// Token: 0x0600642C RID: 25644 RVA: 0x00155960 File Offset: 0x00153B60
		[SecurityCritical]
		internal void Clear<K, V>()
		{
			object obj = JitHelpers.UnsafeCast<object>(this);
			IMap<K, V> map = obj as IMap<K, V>;
			if (map != null)
			{
				map.Clear();
				return;
			}
			IVector<KeyValuePair<K, V>> vector = JitHelpers.UnsafeCast<IVector<KeyValuePair<K, V>>>(this);
			vector.Clear();
		}

		// Token: 0x0600642D RID: 25645 RVA: 0x00155994 File Offset: 0x00153B94
		[SecurityCritical]
		internal bool Contains<K, V>(KeyValuePair<K, V> item)
		{
			object obj = JitHelpers.UnsafeCast<object>(this);
			IDictionary<K, V> dictionary = obj as IDictionary<K, V>;
			if (dictionary != null)
			{
				V x;
				return dictionary.TryGetValue(item.Key, out x) && EqualityComparer<V>.Default.Equals(x, item.Value);
			}
			IVector<KeyValuePair<K, V>> vector = JitHelpers.UnsafeCast<IVector<KeyValuePair<K, V>>>(this);
			uint num;
			return vector.IndexOf(item, out num);
		}

		// Token: 0x0600642E RID: 25646 RVA: 0x001559EC File Offset: 0x00153BEC
		[SecurityCritical]
		internal void CopyTo<K, V>(KeyValuePair<K, V>[] array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (arrayIndex < 0)
			{
				throw new ArgumentOutOfRangeException("arrayIndex");
			}
			if (array.Length <= arrayIndex && this.Count<K, V>() > 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_IndexOutOfArrayBounds"));
			}
			if (array.Length - arrayIndex < this.Count<K, V>())
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InsufficientSpaceToCopyCollection"));
			}
			IIterable<KeyValuePair<K, V>> iterable = JitHelpers.UnsafeCast<IIterable<KeyValuePair<K, V>>>(this);
			foreach (KeyValuePair<K, V> keyValuePair in iterable)
			{
				array[arrayIndex++] = keyValuePair;
			}
		}

		// Token: 0x0600642F RID: 25647 RVA: 0x00155A9C File Offset: 0x00153C9C
		[SecurityCritical]
		internal bool Remove<K, V>(KeyValuePair<K, V> item)
		{
			object obj = JitHelpers.UnsafeCast<object>(this);
			IDictionary<K, V> dictionary = obj as IDictionary<K, V>;
			if (dictionary != null)
			{
				return dictionary.Remove(item.Key);
			}
			IVector<KeyValuePair<K, V>> vector = JitHelpers.UnsafeCast<IVector<KeyValuePair<K, V>>>(this);
			uint num;
			if (!vector.IndexOf(item, out num))
			{
				return false;
			}
			if (2147483647U < num)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
			}
			VectorToListAdapter.RemoveAtHelper<KeyValuePair<K, V>>(vector, num);
			return true;
		}
	}
}
