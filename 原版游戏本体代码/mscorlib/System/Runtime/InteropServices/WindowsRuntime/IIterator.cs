using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A17 RID: 2583
	[Guid("6a79e863-4300-459a-9966-cbb660963ee1")]
	[ComImport]
	internal interface IIterator<T>
	{
		// Token: 0x1700117B RID: 4475
		// (get) Token: 0x060065C3 RID: 26051
		T Current { get; }

		// Token: 0x1700117C RID: 4476
		// (get) Token: 0x060065C4 RID: 26052
		bool HasCurrent { get; }

		// Token: 0x060065C5 RID: 26053
		bool MoveNext();

		// Token: 0x060065C6 RID: 26054
		int GetMany([Out] T[] items);
	}
}
