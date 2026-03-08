using System;
using System.Reflection.Emit;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009BA RID: 2490
	[Guid("007D8A14-FDF3-363E-9A0B-FEC0618260A2")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(MethodBuilder))]
	[ComVisible(true)]
	public interface _MethodBuilder
	{
		// Token: 0x06006387 RID: 25479
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06006388 RID: 25480
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06006389 RID: 25481
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x0600638A RID: 25482
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
