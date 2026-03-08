using System;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200096F RID: 2415
	[Guid("F1C3BF76-C3E4-11d3-88E7-00902754C43A")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(true)]
	public interface ITypeLibImporterNotifySink
	{
		// Token: 0x06006235 RID: 25141
		void ReportEvent(ImporterEventKind eventKind, int eventCode, string eventMsg);

		// Token: 0x06006236 RID: 25142
		Assembly ResolveRef([MarshalAs(UnmanagedType.Interface)] object typeLib);
	}
}
