using System;

namespace MonoMod.Core.Platforms
{
	// Token: 0x020004FF RID: 1279
	internal interface IAllocatedMemory : IDisposable
	{
		// Token: 0x17000630 RID: 1584
		// (get) Token: 0x06001CA1 RID: 7329
		bool IsExecutable { get; }

		// Token: 0x17000631 RID: 1585
		// (get) Token: 0x06001CA2 RID: 7330
		IntPtr BaseAddress { get; }

		// Token: 0x17000632 RID: 1586
		// (get) Token: 0x06001CA3 RID: 7331
		int Size { get; }

		// Token: 0x17000633 RID: 1587
		// (get) Token: 0x06001CA4 RID: 7332
		Span<byte> Memory { get; }
	}
}
