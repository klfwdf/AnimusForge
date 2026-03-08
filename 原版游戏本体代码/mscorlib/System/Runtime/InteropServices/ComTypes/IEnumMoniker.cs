using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A29 RID: 2601
	[Guid("00000102-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[__DynamicallyInvokable]
	[ComImport]
	public interface IEnumMoniker
	{
		// Token: 0x0600661F RID: 26143
		[PreserveSig]
		int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [Out] IMoniker[] rgelt, IntPtr pceltFetched);

		// Token: 0x06006620 RID: 26144
		[__DynamicallyInvokable]
		[PreserveSig]
		int Skip(int celt);

		// Token: 0x06006621 RID: 26145
		[__DynamicallyInvokable]
		void Reset();

		// Token: 0x06006622 RID: 26146
		[__DynamicallyInvokable]
		void Clone(out IEnumMoniker ppenum);
	}
}
