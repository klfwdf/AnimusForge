using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000671 RID: 1649
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("285a8861-c84a-11d7-850f-005cd062464f")]
	[ComImport]
	internal interface ISectionEntry
	{
		// Token: 0x06004F24 RID: 20260
		object GetField(uint fieldId);

		// Token: 0x06004F25 RID: 20261
		string GetFieldName(uint fieldId);
	}
}
