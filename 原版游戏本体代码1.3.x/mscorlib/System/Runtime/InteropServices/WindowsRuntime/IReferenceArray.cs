using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A09 RID: 2569
	[Guid("61c17707-2d65-11e0-9ae8-d48564015472")]
	[ComImport]
	internal interface IReferenceArray<T> : IPropertyValue
	{
		// Token: 0x1700116E RID: 4462
		// (get) Token: 0x0600657B RID: 25979
		T[] Value { get; }
	}
}
