using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000989 RID: 2441
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IPersistFile instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Guid("0000010b-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface UCOMIPersistFile
	{
		// Token: 0x060062B8 RID: 25272
		void GetClassID(out Guid pClassID);

		// Token: 0x060062B9 RID: 25273
		[PreserveSig]
		int IsDirty();

		// Token: 0x060062BA RID: 25274
		void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);

		// Token: 0x060062BB RID: 25275
		void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);

		// Token: 0x060062BC RID: 25276
		void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

		// Token: 0x060062BD RID: 25277
		void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
	}
}
