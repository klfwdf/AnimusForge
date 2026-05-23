using System;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x0200002C RID: 44
	public interface IMemoryOwner<[Nullable(2)] T> : IDisposable
	{
		// Token: 0x1700001D RID: 29
		// (get) Token: 0x060001B9 RID: 441
		[Nullable(new byte[] { 0, 1 })]
		Memory<T> Memory
		{
			[return: Nullable(new byte[] { 0, 1 })]
			get;
		}
	}
}
