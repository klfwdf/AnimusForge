using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000673 RID: 1651
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("285a8860-c84a-11d7-850f-005cd062464f")]
	[ComImport]
	internal interface ICDF
	{
		// Token: 0x06004F2A RID: 20266
		ISection GetRootSection(uint SectionId);

		// Token: 0x06004F2B RID: 20267
		ISectionEntry GetRootSectionEntry(uint SectionId);

		// Token: 0x17000C9B RID: 3227
		// (get) Token: 0x06004F2C RID: 20268
		object _NewEnum
		{
			[return: MarshalAs(UnmanagedType.Interface)]
			get;
		}

		// Token: 0x17000C9C RID: 3228
		// (get) Token: 0x06004F2D RID: 20269
		uint Count { get; }

		// Token: 0x06004F2E RID: 20270
		object GetItem(uint SectionId);
	}
}
