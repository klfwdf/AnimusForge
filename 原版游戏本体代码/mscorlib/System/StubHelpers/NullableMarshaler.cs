using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;

namespace System.StubHelpers
{
	// Token: 0x020005A2 RID: 1442
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class NullableMarshaler
	{
		// Token: 0x06004321 RID: 17185 RVA: 0x000FA120 File Offset: 0x000F8320
		[SecurityCritical]
		internal static IntPtr ConvertToNative<T>(ref T? pManaged) where T : struct
		{
			if (pManaged != null)
			{
				object o = IReferenceFactory.CreateIReference(pManaged);
				return Marshal.GetComInterfaceForObject(o, typeof(IReference<T>));
			}
			return IntPtr.Zero;
		}

		// Token: 0x06004322 RID: 17186 RVA: 0x000FA15C File Offset: 0x000F835C
		[SecurityCritical]
		internal static void ConvertToManagedRetVoid<T>(IntPtr pNative, ref T? retObj) where T : struct
		{
			retObj = NullableMarshaler.ConvertToManaged<T>(pNative);
		}

		// Token: 0x06004323 RID: 17187 RVA: 0x000FA16C File Offset: 0x000F836C
		[SecurityCritical]
		internal static T? ConvertToManaged<T>(IntPtr pNative) where T : struct
		{
			if (pNative != IntPtr.Zero)
			{
				object wrapper = InterfaceMarshaler.ConvertToManagedWithoutUnboxing(pNative);
				return (T?)CLRIReferenceImpl<T>.UnboxHelper(wrapper);
			}
			return null;
		}
	}
}
