using System;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000970 RID: 2416
	[Guid("F1C3BF77-C3E4-11d3-88E7-00902754C43A")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(true)]
	public interface ITypeLibExporterNotifySink
	{
		// Token: 0x06006237 RID: 25143
		void ReportEvent(ExporterEventKind eventKind, int eventCode, string eventMsg);

		// Token: 0x06006238 RID: 25144
		[return: MarshalAs(UnmanagedType.Interface)]
		object ResolveRef(Assembly assembly);
	}
}
