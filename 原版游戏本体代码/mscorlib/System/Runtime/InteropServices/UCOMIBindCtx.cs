using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200097B RID: 2427
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IBindCtx instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Guid("0000000e-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface UCOMIBindCtx
	{
		// Token: 0x06006277 RID: 25207
		void RegisterObjectBound([MarshalAs(UnmanagedType.Interface)] object punk);

		// Token: 0x06006278 RID: 25208
		void RevokeObjectBound([MarshalAs(UnmanagedType.Interface)] object punk);

		// Token: 0x06006279 RID: 25209
		void ReleaseBoundObjects();

		// Token: 0x0600627A RID: 25210
		void SetBindOptions([In] ref BIND_OPTS pbindopts);

		// Token: 0x0600627B RID: 25211
		void GetBindOptions(ref BIND_OPTS pbindopts);

		// Token: 0x0600627C RID: 25212
		void GetRunningObjectTable(out UCOMIRunningObjectTable pprot);

		// Token: 0x0600627D RID: 25213
		void RegisterObjectParam([MarshalAs(UnmanagedType.LPWStr)] string pszKey, [MarshalAs(UnmanagedType.Interface)] object punk);

		// Token: 0x0600627E RID: 25214
		void GetObjectParam([MarshalAs(UnmanagedType.LPWStr)] string pszKey, [MarshalAs(UnmanagedType.Interface)] out object ppunk);

		// Token: 0x0600627F RID: 25215
		void EnumObjectParam(out UCOMIEnumString ppenum);

		// Token: 0x06006280 RID: 25216
		void RevokeObjectParam([MarshalAs(UnmanagedType.LPWStr)] string pszKey);
	}
}
