using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200097C RID: 2428
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IConnectionPointContainer instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Guid("B196B284-BAB4-101A-B69C-00AA00341D07")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface UCOMIConnectionPointContainer
	{
		// Token: 0x06006281 RID: 25217
		void EnumConnectionPoints(out UCOMIEnumConnectionPoints ppEnum);

		// Token: 0x06006282 RID: 25218
		void FindConnectionPoint(ref Guid riid, out UCOMIConnectionPoint ppCP);
	}
}
