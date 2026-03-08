using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A18 RID: 2584
	[Guid("6a1d6c07-076d-49f2-8314-f52c9c9a8331")]
	[ComImport]
	internal interface IBindableIterator
	{
		// Token: 0x1700117D RID: 4477
		// (get) Token: 0x060065C7 RID: 26055
		object Current { get; }

		// Token: 0x1700117E RID: 4478
		// (get) Token: 0x060065C8 RID: 26056
		bool HasCurrent { get; }

		// Token: 0x060065C9 RID: 26057
		bool MoveNext();
	}
}
