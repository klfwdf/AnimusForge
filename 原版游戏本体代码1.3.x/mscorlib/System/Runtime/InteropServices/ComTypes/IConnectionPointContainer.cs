using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A25 RID: 2597
	[Guid("B196B284-BAB4-101A-B69C-00AA00341D07")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[__DynamicallyInvokable]
	[ComImport]
	public interface IConnectionPointContainer
	{
		// Token: 0x06006614 RID: 26132
		[__DynamicallyInvokable]
		void EnumConnectionPoints(out IEnumConnectionPoints ppEnum);

		// Token: 0x06006615 RID: 26133
		[__DynamicallyInvokable]
		void FindConnectionPoint([In] ref Guid riid, out IConnectionPoint ppCP);
	}
}
