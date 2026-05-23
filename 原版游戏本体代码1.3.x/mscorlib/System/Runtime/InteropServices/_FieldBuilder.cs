using System;
using System.Reflection.Emit;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009B7 RID: 2487
	[Guid("CE1A3BF5-975E-30CC-97C9-1EF70F8F3993")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(FieldBuilder))]
	[ComVisible(true)]
	public interface _FieldBuilder
	{
		// Token: 0x0600637B RID: 25467
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x0600637C RID: 25468
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x0600637D RID: 25469
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x0600637E RID: 25470
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
