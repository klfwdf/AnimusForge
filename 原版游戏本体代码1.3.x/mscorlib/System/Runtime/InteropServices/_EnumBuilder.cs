using System;
using System.Reflection.Emit;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009B5 RID: 2485
	[Guid("C7BD73DE-9F85-3290-88EE-090B8BDFE2DF")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(EnumBuilder))]
	[ComVisible(true)]
	public interface _EnumBuilder
	{
		// Token: 0x06006373 RID: 25459
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06006374 RID: 25460
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06006375 RID: 25461
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x06006376 RID: 25462
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
