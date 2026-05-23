using System;
using System.Reflection.Emit;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009BE RID: 2494
	[Guid("15F9A479-9397-3A63-ACBD-F51977FB0F02")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(PropertyBuilder))]
	[ComVisible(true)]
	public interface _PropertyBuilder
	{
		// Token: 0x06006397 RID: 25495
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06006398 RID: 25496
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06006399 RID: 25497
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x0600639A RID: 25498
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
