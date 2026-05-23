using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200097D RID: 2429
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IConnectionPoint instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Guid("B196B286-BAB4-101A-B69C-00AA00341D07")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface UCOMIConnectionPoint
	{
		// Token: 0x06006283 RID: 25219
		void GetConnectionInterface(out Guid pIID);

		// Token: 0x06006284 RID: 25220
		void GetConnectionPointContainer(out UCOMIConnectionPointContainer ppCPC);

		// Token: 0x06006285 RID: 25221
		void Advise([MarshalAs(UnmanagedType.Interface)] object pUnkSink, out int pdwCookie);

		// Token: 0x06006286 RID: 25222
		void Unadvise(int dwCookie);

		// Token: 0x06006287 RID: 25223
		void EnumConnections(out UCOMIEnumConnections ppEnum);
	}
}
