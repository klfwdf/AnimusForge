using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;

namespace System.StubHelpers
{
	// Token: 0x020005A8 RID: 1448
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class KeyValuePairMarshaler
	{
		// Token: 0x0600432B RID: 17195 RVA: 0x000FA358 File Offset: 0x000F8558
		[SecurityCritical]
		internal static IntPtr ConvertToNative<K, V>([In] ref KeyValuePair<K, V> pair)
		{
			IKeyValuePair<K, V> o = new CLRIKeyValuePairImpl<K, V>(ref pair);
			return Marshal.GetComInterfaceForObject(o, typeof(IKeyValuePair<K, V>));
		}

		// Token: 0x0600432C RID: 17196 RVA: 0x000FA37C File Offset: 0x000F857C
		[SecurityCritical]
		internal static KeyValuePair<K, V> ConvertToManaged<K, V>(IntPtr pInsp)
		{
			object obj = InterfaceMarshaler.ConvertToManagedWithoutUnboxing(pInsp);
			IKeyValuePair<K, V> keyValuePair = (IKeyValuePair<K, V>)obj;
			return new KeyValuePair<K, V>(keyValuePair.Key, keyValuePair.Value);
		}

		// Token: 0x0600432D RID: 17197 RVA: 0x000FA3A8 File Offset: 0x000F85A8
		[SecurityCritical]
		internal static object ConvertToManagedBox<K, V>(IntPtr pInsp)
		{
			return KeyValuePairMarshaler.ConvertToManaged<K, V>(pInsp);
		}
	}
}
