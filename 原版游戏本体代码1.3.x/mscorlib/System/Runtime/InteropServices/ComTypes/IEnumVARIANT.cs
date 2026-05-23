using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A2E RID: 2606
	[Guid("00020404-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[__DynamicallyInvokable]
	[ComImport]
	public interface IEnumVARIANT
	{
		// Token: 0x0600662F RID: 26159
		[PreserveSig]
		int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [Out] object[] rgVar, IntPtr pceltFetched);

		// Token: 0x06006630 RID: 26160
		[__DynamicallyInvokable]
		[PreserveSig]
		int Skip(int celt);

		// Token: 0x06006631 RID: 26161
		[__DynamicallyInvokable]
		[PreserveSig]
		int Reset();

		// Token: 0x06006632 RID: 26162
		[__DynamicallyInvokable]
		IEnumVARIANT Clone();
	}
}
