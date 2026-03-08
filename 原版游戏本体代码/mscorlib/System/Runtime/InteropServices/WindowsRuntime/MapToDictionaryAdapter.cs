using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009D7 RID: 2519
	internal sealed class MapToDictionaryAdapter
	{
		// Token: 0x0600641D RID: 25629 RVA: 0x00155650 File Offset: 0x00153850
		private MapToDictionaryAdapter()
		{
		}

		// Token: 0x0600641E RID: 25630 RVA: 0x00155658 File Offset: 0x00153858
		[SecurityCritical]
		internal V Indexer_Get<K, V>(K key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			IMap<K, V> this2 = JitHelpers.UnsafeCast<IMap<K, V>>(this);
			return MapToDictionaryAdapter.Lookup<K, V>(this2, key);
		}

		// Token: 0x0600641F RID: 25631 RVA: 0x00155688 File Offset: 0x00153888
		[SecurityCritical]
		internal void Indexer_Set<K, V>(K key, V value)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			IMap<K, V> this2 = JitHelpers.UnsafeCast<IMap<K, V>>(this);
			MapToDictionaryAdapter.Insert<K, V>(this2, key, value);
		}

		// Token: 0x06006420 RID: 25632 RVA: 0x001556B8 File Offset: 0x001538B8
		[SecurityCritical]
		internal ICollection<K> Keys<K, V>()
		{
			IMap<K, V> map = JitHelpers.UnsafeCast<IMap<K, V>>(this);
			IDictionary<K, V> dictionary = (IDictionary<K, V>)map;
			return new DictionaryKeyCollection<K, V>(dictionary);
		}

		// Token: 0x06006421 RID: 25633 RVA: 0x001556DC File Offset: 0x001538DC
		[SecurityCritical]
		internal ICollection<V> Values<K, V>()
		{
			IMap<K, V> map = JitHelpers.UnsafeCast<IMap<K, V>>(this);
			IDictionary<K, V> dictionary = (IDictionary<K, V>)map;
			return new DictionaryValueCollection<K, V>(dictionary);
		}

		// Token: 0x06006422 RID: 25634 RVA: 0x00155700 File Offset: 0x00153900
		[SecurityCritical]
		internal bool ContainsKey<K, V>(K key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			IMap<K, V> map = JitHelpers.UnsafeCast<IMap<K, V>>(this);
			return map.HasKey(key);
		}

		// Token: 0x06006423 RID: 25635 RVA: 0x00155730 File Offset: 0x00153930
		[SecurityCritical]
		internal void Add<K, V>(K key, V value)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			if (this.ContainsKey<K, V>(key))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_AddingDuplicate"));
			}
			IMap<K, V> this2 = JitHelpers.UnsafeCast<IMap<K, V>>(this);
			MapToDictionaryAdapter.Insert<K, V>(this2, key, value);
		}

		// Token: 0x06006424 RID: 25636 RVA: 0x0015577C File Offset: 0x0015397C
		[SecurityCritical]
		internal bool Remove<K, V>(K key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			IMap<K, V> map = JitHelpers.UnsafeCast<IMap<K, V>>(this);
			if (!map.HasKey(key))
			{
				return false;
			}
			bool result;
			try
			{
				map.Remove(key);
				result = true;
			}
			catch (Exception ex)
			{
				if (-2147483637 != ex._HResult)
				{
					throw;
				}
				result = false;
			}
			return result;
		}

		// Token: 0x06006425 RID: 25637 RVA: 0x001557E0 File Offset: 0x001539E0
		[SecurityCritical]
		internal bool TryGetValue<K, V>(K key, out V value)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			IMap<K, V> map = JitHelpers.UnsafeCast<IMap<K, V>>(this);
			if (!map.HasKey(key))
			{
				value = default(V);
				return false;
			}
			bool result;
			try
			{
				value = MapToDictionaryAdapter.Lookup<K, V>(map, key);
				result = true;
			}
			catch (KeyNotFoundException)
			{
				value = default(V);
				result = false;
			}
			return result;
		}

		// Token: 0x06006426 RID: 25638 RVA: 0x00155848 File Offset: 0x00153A48
		private static V Lookup<K, V>(IMap<K, V> _this, K key)
		{
			V result;
			try
			{
				result = _this.Lookup(key);
			}
			catch (Exception ex)
			{
				if (-2147483637 == ex._HResult)
				{
					throw new KeyNotFoundException(Environment.GetResourceString("Arg_KeyNotFound"));
				}
				throw;
			}
			return result;
		}

		// Token: 0x06006427 RID: 25639 RVA: 0x00155890 File Offset: 0x00153A90
		private static bool Insert<K, V>(IMap<K, V> _this, K key, V value)
		{
			return _this.Insert(key, value);
		}
	}
}
