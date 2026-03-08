using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A2D RID: 2605
	[Guid("00000101-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[__DynamicallyInvokable]
	[ComImport]
	public interface IEnumString
	{
		// Token: 0x0600662B RID: 26155
		[PreserveSig]
		int Next(int celt, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 0)] [Out] string[] rgelt, IntPtr pceltFetched);

		// Token: 0x0600662C RID: 26156
		[__DynamicallyInvokable]
		[PreserveSig]
		int Skip(int celt);

		// Token: 0x0600662D RID: 26157
		[__DynamicallyInvokable]
		void Reset();

		// Token: 0x0600662E RID: 26158
		[__DynamicallyInvokable]
		void Clone(out IEnumString ppenum);
	}
}
