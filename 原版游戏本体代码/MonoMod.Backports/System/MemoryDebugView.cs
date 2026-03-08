using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x02000014 RID: 20
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class MemoryDebugView<[Nullable(2)] T>
	{
		// Token: 0x06000055 RID: 85 RVA: 0x00003780 File Offset: 0x00001980
		public MemoryDebugView([Nullable(new byte[] { 0, 1 })] Memory<T> memory)
		{
			this._memory = memory;
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00003794 File Offset: 0x00001994
		public MemoryDebugView([Nullable(new byte[] { 0, 1 })] ReadOnlyMemory<T> memory)
		{
			this._memory = memory;
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000057 RID: 87 RVA: 0x000037A3 File Offset: 0x000019A3
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get
			{
				return this._memory.ToArray();
			}
		}

		// Token: 0x04000029 RID: 41
		[Nullable(new byte[] { 0, 1 })]
		private readonly ReadOnlyMemory<T> _memory;
	}
}
