using System;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200090D RID: 2317
	[Guid("B42B6AAC-317E-34D5-9FA9-093BB4160C50")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(AssemblyName))]
	[ComVisible(true)]
	public interface _AssemblyName
	{
		// Token: 0x06005FE1 RID: 24545
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06005FE2 RID: 24546
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06005FE3 RID: 24547
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x06005FE4 RID: 24548
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
