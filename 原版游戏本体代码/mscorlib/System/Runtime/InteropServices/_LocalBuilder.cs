using System;
using System.Reflection.Emit;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009B9 RID: 2489
	[Guid("4E6350D1-A08B-3DEC-9A3E-C465F9AEEC0C")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(LocalBuilder))]
	[ComVisible(true)]
	public interface _LocalBuilder
	{
		// Token: 0x06006383 RID: 25475
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06006384 RID: 25476
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06006385 RID: 25477
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x06006386 RID: 25478
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
