using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200098B RID: 2443
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IRunningObjectTable instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Guid("00000010-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface UCOMIRunningObjectTable
	{
		// Token: 0x060062CA RID: 25290
		void Register(int grfFlags, [MarshalAs(UnmanagedType.Interface)] object punkObject, UCOMIMoniker pmkObjectName, out int pdwRegister);

		// Token: 0x060062CB RID: 25291
		void Revoke(int dwRegister);

		// Token: 0x060062CC RID: 25292
		void IsRunning(UCOMIMoniker pmkObjectName);

		// Token: 0x060062CD RID: 25293
		void GetObject(UCOMIMoniker pmkObjectName, [MarshalAs(UnmanagedType.Interface)] out object ppunkObject);

		// Token: 0x060062CE RID: 25294
		void NoteChangeTime(int dwRegister, ref FILETIME pfiletime);

		// Token: 0x060062CF RID: 25295
		void GetTimeOfLastChange(UCOMIMoniker pmkObjectName, out FILETIME pfiletime);

		// Token: 0x060062D0 RID: 25296
		void EnumRunning(out UCOMIEnumMoniker ppenumMoniker);
	}
}
