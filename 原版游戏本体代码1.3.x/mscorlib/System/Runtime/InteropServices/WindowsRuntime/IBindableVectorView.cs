using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A1D RID: 2589
	[Guid("346dd6e7-976e-4bc3-815d-ece243bc0f33")]
	[ComImport]
	internal interface IBindableVectorView : IBindableIterable
	{
		// Token: 0x060065F0 RID: 26096
		object GetAt(uint index);

		// Token: 0x17001183 RID: 4483
		// (get) Token: 0x060065F1 RID: 26097
		uint Size { get; }

		// Token: 0x060065F2 RID: 26098
		bool IndexOf(object value, out uint index);
	}
}
