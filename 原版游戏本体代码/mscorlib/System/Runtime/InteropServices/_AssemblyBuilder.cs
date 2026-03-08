using System;
using System.Reflection.Emit;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009B2 RID: 2482
	[Guid("BEBB2505-8B54-3443-AEAD-142A16DD9CC7")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(AssemblyBuilder))]
	[ComVisible(true)]
	public interface _AssemblyBuilder
	{
		// Token: 0x06006367 RID: 25447
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06006368 RID: 25448
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06006369 RID: 25449
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x0600636A RID: 25450
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
