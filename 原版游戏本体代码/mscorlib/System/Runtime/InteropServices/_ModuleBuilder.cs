using System;
using System.Reflection.Emit;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009BC RID: 2492
	[Guid("D05FFA9A-04AF-3519-8EE1-8D93AD73430B")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(ModuleBuilder))]
	[ComVisible(true)]
	public interface _ModuleBuilder
	{
		// Token: 0x0600638F RID: 25487
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06006390 RID: 25488
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06006391 RID: 25489
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x06006392 RID: 25490
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
	}
}
