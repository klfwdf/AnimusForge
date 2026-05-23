using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A2B RID: 2603
	[Guid("B196B287-BAB4-101A-B69C-00AA00341D07")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[__DynamicallyInvokable]
	[ComImport]
	public interface IEnumConnections
	{
		// Token: 0x06006623 RID: 26147
		[PreserveSig]
		int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [Out] CONNECTDATA[] rgelt, IntPtr pceltFetched);

		// Token: 0x06006624 RID: 26148
		[__DynamicallyInvokable]
		[PreserveSig]
		int Skip(int celt);

		// Token: 0x06006625 RID: 26149
		[__DynamicallyInvokable]
		void Reset();

		// Token: 0x06006626 RID: 26150
		[__DynamicallyInvokable]
		void Clone(out IEnumConnections ppenum);
	}
}
