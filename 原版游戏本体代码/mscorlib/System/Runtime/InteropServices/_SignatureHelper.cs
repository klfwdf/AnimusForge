using System;
using System.Reflection.Emit;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009BF RID: 2495
	[Guid("7D13DD37-5A04-393C-BBCA-A5FEA802893D")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(SignatureHelper))]
	[ComVisible(true)]
	public interface _SignatureHelper
	{
		// Token: 0x0600639B RID: 25499
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x0600639C RID: 25500
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x0600639D RID: 25501
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x0600639E RID: 25502
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
