using System;
using System.Reflection.Emit;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009B6 RID: 2486
	[Guid("AADABA99-895D-3D65-9760-B1F12621FAE8")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(EventBuilder))]
	[ComVisible(true)]
	public interface _EventBuilder
	{
		// Token: 0x06006377 RID: 25463
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06006378 RID: 25464
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06006379 RID: 25465
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x0600637A RID: 25466
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
