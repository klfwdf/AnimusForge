using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A34 RID: 2612
	[Guid("00000010-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[__DynamicallyInvokable]
	[ComImport]
	public interface IRunningObjectTable
	{
		// Token: 0x0600665D RID: 26205
		[__DynamicallyInvokable]
		int Register(int grfFlags, [MarshalAs(UnmanagedType.Interface)] object punkObject, IMoniker pmkObjectName);

		// Token: 0x0600665E RID: 26206
		[__DynamicallyInvokable]
		void Revoke(int dwRegister);

		// Token: 0x0600665F RID: 26207
		[__DynamicallyInvokable]
		[PreserveSig]
		int IsRunning(IMoniker pmkObjectName);

		// Token: 0x06006660 RID: 26208
		[__DynamicallyInvokable]
		[PreserveSig]
		int GetObject(IMoniker pmkObjectName, [MarshalAs(UnmanagedType.Interface)] out object ppunkObject);

		// Token: 0x06006661 RID: 26209
		[__DynamicallyInvokable]
		void NoteChangeTime(int dwRegister, ref FILETIME pfiletime);

		// Token: 0x06006662 RID: 26210
		[__DynamicallyInvokable]
		[PreserveSig]
		int GetTimeOfLastChange(IMoniker pmkObjectName, out FILETIME pfiletime);

		// Token: 0x06006663 RID: 26211
		[__DynamicallyInvokable]
		void EnumRunning(out IEnumMoniker ppenumMoniker);
	}
}
