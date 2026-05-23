using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A08 RID: 2568
	[Guid("61c17706-2d65-11e0-9ae8-d48564015472")]
	[ComImport]
	internal interface IReference<T> : IPropertyValue
	{
		// Token: 0x1700116D RID: 4461
		// (get) Token: 0x0600657A RID: 25978
		T Value { get; }
	}
}
