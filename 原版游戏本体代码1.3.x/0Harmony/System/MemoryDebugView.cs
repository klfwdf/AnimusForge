using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x0200046F RID: 1135
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class MemoryDebugView<[Nullable(2)] T>
	{
		// Token: 0x0600189C RID: 6300 RVA: 0x0004E714 File Offset: 0x0004C914
		public MemoryDebugView([Nullable(new byte[] { 0, 1 })] Memory<T> memory)
		{
			this._memory = memory;
		}

		// Token: 0x0600189D RID: 6301 RVA: 0x0004E728 File Offset: 0x0004C928
		public MemoryDebugView([Nullable(new byte[] { 0, 1 })] ReadOnlyMemory<T> memory)
		{
			this._memory = memory;
		}

		// Token: 0x170005A8 RID: 1448
		// (get) Token: 0x0600189E RID: 6302 RVA: 0x0004E737 File Offset: 0x0004C937
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get
			{
				return this._memory.ToArray();
			}
		}

		// Token: 0x04001099 RID: 4249
		[Nullable(new byte[] { 0, 1 })]
		private readonly ReadOnlyMemory<T> _memory;
	}
}
