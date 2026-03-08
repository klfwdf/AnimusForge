using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A2C RID: 2604
	[Guid("B196B285-BAB4-101A-B69C-00AA00341D07")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[__DynamicallyInvokable]
	[ComImport]
	public interface IEnumConnectionPoints
	{
		// Token: 0x06006627 RID: 26151
		[PreserveSig]
		int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [Out] IConnectionPoint[] rgelt, IntPtr pceltFetched);

		// Token: 0x06006628 RID: 26152
		[__DynamicallyInvokable]
		[PreserveSig]
		int Skip(int celt);

		// Token: 0x06006629 RID: 26153
		[__DynamicallyInvokable]
		void Reset();

		// Token: 0x0600662A RID: 26154
		[__DynamicallyInvokable]
		void Clone(out IEnumConnectionPoints ppenum);
	}
}
