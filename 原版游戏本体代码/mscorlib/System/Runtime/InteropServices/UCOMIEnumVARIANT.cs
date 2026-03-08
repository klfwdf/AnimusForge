using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000985 RID: 2437
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IEnumVARIANT instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Guid("00020404-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface UCOMIEnumVARIANT
	{
		// Token: 0x0600629C RID: 25244
		[PreserveSig]
		int Next(int celt, int rgvar, int pceltFetched);

		// Token: 0x0600629D RID: 25245
		[PreserveSig]
		int Skip(int celt);

		// Token: 0x0600629E RID: 25246
		[PreserveSig]
		int Reset();

		// Token: 0x0600629F RID: 25247
		void Clone(int ppenum);
	}
}
