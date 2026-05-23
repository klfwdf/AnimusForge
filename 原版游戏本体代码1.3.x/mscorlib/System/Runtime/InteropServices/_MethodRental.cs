using System;
using System.Reflection.Emit;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009BB RID: 2491
	[Guid("C2323C25-F57F-3880-8A4D-12EBEA7A5852")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(MethodRental))]
	[ComVisible(true)]
	public interface _MethodRental
	{
		// Token: 0x0600638B RID: 25483
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x0600638C RID: 25484
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x0600638D RID: 25485
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x0600638E RID: 25486
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
