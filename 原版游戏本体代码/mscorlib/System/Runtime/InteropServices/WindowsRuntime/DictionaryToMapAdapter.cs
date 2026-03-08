using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009DB RID: 2523
	internal sealed class DictionaryToMapAdapter
	{
		// Token: 0x06006441 RID: 25665 RVA: 0x00155EC7 File Offset: 0x001540C7
		private DictionaryToMapAdapter()
		{
		}

		// Token: 0x06006442 RID: 25666 RVA: 0x00155ED0 File Offset: 0x001540D0
		[SecurityCritical]
		internal V Lookup<K, V>(K key)
		{
			IDictionary<K, V> dictionary = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
			V result;
			if (!dictionary.TryGetValue(key, out result))
			{
				Exception ex = new KeyNotFoundException(Environment.GetResourceString("Arg_KeyNotFound"));
				ex.SetErrorCode(-2147483637);
				throw ex;
			}
			return result;
		}

		// Token: 0x06006443 RID: 25667 RVA: 0x00155F10 File Offset: 0x00154110
		[SecurityCritical]
		internal uint Size<K, V>()
		{
			IDictionary<K, V> dictionary = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
			return (uint)dictionary.Count;
		}

		// Token: 0x06006444 RID: 25668 RVA: 0x00155F2C File Offset: 0x0015412C
		[SecurityCritical]
		internal bool HasKey<K, V>(K key)
		{
			IDictionary<K, V> dictionary = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
			return dictionary.ContainsKey(key);
		}

		// Token: 0x06006445 RID: 25669 RVA: 0x00155F48 File Offset: 0x00154148
		[SecurityCritical]
		internal IReadOnlyDictionary<K, V> GetView<K, V>()
		{
			IDictionary<K, V> dictionary = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
			IReadOnlyDictionary<K, V> readOnlyDictionary = dictionary as IReadOnlyDictionary<K, V>;
			if (readOnlyDictionary == null)
			{
				readOnlyDictionary = new ReadOnlyDictionary<K, V>(dictionary);
			}
			return readOnlyDictionary;
		}

		// Token: 0x06006446 RID: 25670 RVA: 0x00155F70 File Offset: 0x00154170
		[SecurityCritical]
		internal bool Insert<K, V>(K key, V value)
		{
			IDictionary<K, V> dictionary = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
			bool result = dictionary.ContainsKey(key);
			dictionary[key] = value;
			return result;
		}

		// Token: 0x06006447 RID: 25671 RVA: 0x00155F98 File Offset: 0x00154198
		[SecurityCritical]
		internal void Remove<K, V>(K key)
		{
			IDictionary<K, V> dictionary = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
			if (!dictionary.Remove(key))
			{
				Exception ex = new KeyNotFoundException(Environment.GetResourceString("Arg_KeyNotFound"));
				ex.SetErrorCode(-2147483637);
				throw ex;
			}
		}

		// Token: 0x06006448 RID: 25672 RVA: 0x00155FD4 File Offset: 0x001541D4
		[SecurityCritical]
		internal void Clear<K, V>()
		{
			IDictionary<K, V> dictionary = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
			dictionary.Clear();
		}
	}
}
