using System;
using System.Threading;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000901 RID: 2305
	[Guid("C281C7F1-4AA9-3517-961A-463CFED57E75")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(Thread))]
	[ComVisible(true)]
	public interface _Thread
	{
		// Token: 0x06005E5F RID: 24159
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06005E60 RID: 24160
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06005E61 RID: 24161
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x06005E62 RID: 24162
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
