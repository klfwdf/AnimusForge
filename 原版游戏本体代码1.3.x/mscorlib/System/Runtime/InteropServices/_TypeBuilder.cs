using System;
using System.Reflection.Emit;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009C0 RID: 2496
	[Guid("7E5678EE-48B3-3F83-B076-C58543498A58")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(TypeBuilder))]
	[ComVisible(true)]
	public interface _TypeBuilder
	{
		// Token: 0x0600639F RID: 25503
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x060063A0 RID: 25504
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x060063A1 RID: 25505
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x060063A2 RID: 25506
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
