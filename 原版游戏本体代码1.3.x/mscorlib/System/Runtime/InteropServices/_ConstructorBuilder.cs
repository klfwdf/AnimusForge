using System;
using System.Reflection.Emit;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009B3 RID: 2483
	[Guid("ED3E4384-D7E2-3FA7-8FFD-8940D330519A")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(ConstructorBuilder))]
	[ComVisible(true)]
	public interface _ConstructorBuilder
	{
		// Token: 0x0600636B RID: 25451
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x0600636C RID: 25452
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x0600636D RID: 25453
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x0600636E RID: 25454
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
