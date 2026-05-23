using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A24 RID: 2596
	[Guid("0000000e-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[__DynamicallyInvokable]
	[ComImport]
	public interface IBindCtx
	{
		// Token: 0x0600660A RID: 26122
		[__DynamicallyInvokable]
		void RegisterObjectBound([MarshalAs(UnmanagedType.Interface)] object punk);

		// Token: 0x0600660B RID: 26123
		[__DynamicallyInvokable]
		void RevokeObjectBound([MarshalAs(UnmanagedType.Interface)] object punk);

		// Token: 0x0600660C RID: 26124
		[__DynamicallyInvokable]
		void ReleaseBoundObjects();

		// Token: 0x0600660D RID: 26125
		[__DynamicallyInvokable]
		void SetBindOptions([In] ref BIND_OPTS pbindopts);

		// Token: 0x0600660E RID: 26126
		[__DynamicallyInvokable]
		void GetBindOptions(ref BIND_OPTS pbindopts);

		// Token: 0x0600660F RID: 26127
		[__DynamicallyInvokable]
		void GetRunningObjectTable(out IRunningObjectTable pprot);

		// Token: 0x06006610 RID: 26128
		[__DynamicallyInvokable]
		void RegisterObjectParam([MarshalAs(UnmanagedType.LPWStr)] string pszKey, [MarshalAs(UnmanagedType.Interface)] object punk);

		// Token: 0x06006611 RID: 26129
		[__DynamicallyInvokable]
		void GetObjectParam([MarshalAs(UnmanagedType.LPWStr)] string pszKey, [MarshalAs(UnmanagedType.Interface)] out object ppunk);

		// Token: 0x06006612 RID: 26130
		[__DynamicallyInvokable]
		void EnumObjectParam(out IEnumString ppenum);

		// Token: 0x06006613 RID: 26131
		[__DynamicallyInvokable]
		[PreserveSig]
		int RevokeObjectParam([MarshalAs(UnmanagedType.LPWStr)] string pszKey);
	}
}
