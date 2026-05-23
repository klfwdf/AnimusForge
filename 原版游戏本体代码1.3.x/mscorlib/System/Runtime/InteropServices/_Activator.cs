using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x020008FF RID: 2303
	[Guid("03973551-57A1-3900-A2B5-9083E3FF2943")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(Activator))]
	[ComVisible(true)]
	public interface _Activator
	{
		// Token: 0x06005E57 RID: 24151
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06005E58 RID: 24152
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06005E59 RID: 24153
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x06005E5A RID: 24154
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
