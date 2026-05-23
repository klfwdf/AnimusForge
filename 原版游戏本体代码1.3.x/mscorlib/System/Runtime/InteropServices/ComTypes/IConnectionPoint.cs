using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A26 RID: 2598
	[Guid("B196B286-BAB4-101A-B69C-00AA00341D07")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[__DynamicallyInvokable]
	[ComImport]
	public interface IConnectionPoint
	{
		// Token: 0x06006616 RID: 26134
		[__DynamicallyInvokable]
		void GetConnectionInterface(out Guid pIID);

		// Token: 0x06006617 RID: 26135
		[__DynamicallyInvokable]
		void GetConnectionPointContainer(out IConnectionPointContainer ppCPC);

		// Token: 0x06006618 RID: 26136
		[__DynamicallyInvokable]
		void Advise([MarshalAs(UnmanagedType.Interface)] object pUnkSink, out int pdwCookie);

		// Token: 0x06006619 RID: 26137
		[__DynamicallyInvokable]
		void Unadvise(int dwCookie);

		// Token: 0x0600661A RID: 26138
		[__DynamicallyInvokable]
		void EnumConnections(out IEnumConnections ppEnum);
	}
}
