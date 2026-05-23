using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000900 RID: 2304
	[Guid("917B14D0-2D9E-38B8-92A9-381ACF52F7C0")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(Attribute))]
	[ComVisible(true)]
	public interface _Attribute
	{
		// Token: 0x06005E5B RID: 24155
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06005E5C RID: 24156
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06005E5D RID: 24157
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x06005E5E RID: 24158
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
