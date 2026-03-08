using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009EB RID: 2539
	[DebuggerDisplay("Size = {Size}")]
	internal sealed class IReadOnlyDictionaryToIMapViewAdapter
	{
		// Token: 0x060064A4 RID: 25764 RVA: 0x00156F18 File Offset: 0x00155118
		private IReadOnlyDictionaryToIMapViewAdapter()
		{
		}

		// Token: 0x060064A5 RID: 25765 RVA: 0x00156F20 File Offset: 0x00155120
		[SecurityCritical]
		internal V Lookup<K, V>(K key)
		{
			IReadOnlyDictionary<K, V> readOnlyDictionary = JitHelpers.UnsafeCast<IReadOnlyDictionary<K, V>>(this);
			V result;
			if (!readOnlyDictionary.TryGetValue(key, out result))
			{
				Exception ex = new KeyNotFoundException(Environment.GetResourceString("Arg_KeyNotFound"));
				ex.SetErrorCode(-2147483637);
				throw ex;
			}
			return result;
		}

		// Token: 0x060064A6 RID: 25766 RVA: 0x00156F60 File Offset: 0x00155160
		[SecurityCritical]
		internal uint Size<K, V>()
		{
			IReadOnlyDictionary<K, V> readOnlyDictionary = JitHelpers.UnsafeCast<IReadOnlyDictionary<K, V>>(this);
			return (uint)readOnlyDictionary.Count;
		}

		// Token: 0x060064A7 RID: 25767 RVA: 0x00156F7C File Offset: 0x0015517C
		[SecurityCritical]
		internal bool HasKey<K, V>(K key)
		{
			IReadOnlyDictionary<K, V> readOnlyDictionary = JitHelpers.UnsafeCast<IReadOnlyDictionary<K, V>>(this);
			return readOnlyDictionary.ContainsKey(key);
		}

		// Token: 0x060064A8 RID: 25768 RVA: 0x00156F98 File Offset: 0x00155198
		[SecurityCritical]
		internal void Split<K, V>(out IMapView<K, V> first, out IMapView<K, V> second)
		{
			IReadOnlyDictionary<K, V> readOnlyDictionary = JitHelpers.UnsafeCast<IReadOnlyDictionary<K, V>>(this);
			if (readOnlyDictionary.Count < 2)
			{
				first = null;
				second = null;
				return;
			}
			ConstantSplittableMap<K, V> constantSplittableMap = readOnlyDictionary as ConstantSplittableMap<K, V>;
			if (constantSplittableMap == null)
			{
				constantSplittableMap = new ConstantSplittableMap<K, V>(readOnlyDictionary);
			}
			constantSplittableMap.Split(out first, out second);
		}
	}
}
