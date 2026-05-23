using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A48 RID: 2632
	[__DynamicallyInvokable]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct EXCEPINFO
	{
		// Token: 0x04002DC7 RID: 11719
		[__DynamicallyInvokable]
		public short wCode;

		// Token: 0x04002DC8 RID: 11720
		[__DynamicallyInvokable]
		public short wReserved;

		// Token: 0x04002DC9 RID: 11721
		[__DynamicallyInvokable]
		[MarshalAs(UnmanagedType.BStr)]
		public string bstrSource;

		// Token: 0x04002DCA RID: 11722
		[__DynamicallyInvokable]
		[MarshalAs(UnmanagedType.BStr)]
		public string bstrDescription;

		// Token: 0x04002DCB RID: 11723
		[__DynamicallyInvokable]
		[MarshalAs(UnmanagedType.BStr)]
		public string bstrHelpFile;

		// Token: 0x04002DCC RID: 11724
		[__DynamicallyInvokable]
		public int dwHelpContext;

		// Token: 0x04002DCD RID: 11725
		public IntPtr pvReserved;

		// Token: 0x04002DCE RID: 11726
		public IntPtr pfnDeferredFillIn;

		// Token: 0x04002DCF RID: 11727
		[__DynamicallyInvokable]
		public int scode;
	}
}
