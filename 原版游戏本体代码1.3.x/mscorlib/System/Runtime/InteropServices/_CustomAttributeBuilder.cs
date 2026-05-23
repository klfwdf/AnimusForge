using System;
using System.Reflection.Emit;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009B4 RID: 2484
	[Guid("BE9ACCE8-AAFF-3B91-81AE-8211663F5CAD")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(CustomAttributeBuilder))]
	[ComVisible(true)]
	public interface _CustomAttributeBuilder
	{
		// Token: 0x0600636F RID: 25455
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06006370 RID: 25456
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06006371 RID: 25457
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x06006372 RID: 25458
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
