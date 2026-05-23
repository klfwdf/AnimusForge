using System;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200090C RID: 2316
	[Guid("D002E9BA-D9E3-3749-B1D3-D565A08B13E7")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(Module))]
	[ComVisible(true)]
	public interface _Module
	{
		// Token: 0x06005FDD RID: 24541
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06005FDE RID: 24542
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06005FDF RID: 24543
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x06005FE0 RID: 24544
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
