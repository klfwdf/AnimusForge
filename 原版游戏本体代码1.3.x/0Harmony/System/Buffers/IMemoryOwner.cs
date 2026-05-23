using System;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x02000492 RID: 1170
	internal interface IMemoryOwner<[Nullable(2)] T> : IDisposable
	{
		// Token: 0x170005C1 RID: 1473
		// (get) Token: 0x06001A16 RID: 6678
		[Nullable(new byte[] { 0, 1 })]
		Memory<T> Memory
		{
			[return: Nullable(new byte[] { 0, 1 })]
			get;
		}
	}
}
