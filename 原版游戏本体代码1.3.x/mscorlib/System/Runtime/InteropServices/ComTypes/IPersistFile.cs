using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A32 RID: 2610
	[Guid("0000010b-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[__DynamicallyInvokable]
	[ComImport]
	public interface IPersistFile
	{
		// Token: 0x0600664B RID: 26187
		[__DynamicallyInvokable]
		void GetClassID(out Guid pClassID);

		// Token: 0x0600664C RID: 26188
		[__DynamicallyInvokable]
		[PreserveSig]
		int IsDirty();

		// Token: 0x0600664D RID: 26189
		[__DynamicallyInvokable]
		void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);

		// Token: 0x0600664E RID: 26190
		[__DynamicallyInvokable]
		void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);

		// Token: 0x0600664F RID: 26191
		[__DynamicallyInvokable]
		void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

		// Token: 0x06006650 RID: 26192
		[__DynamicallyInvokable]
		void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
	}
}
