using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x02000019 RID: 25
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class SpanDebugView<[Nullable(2)] T>
	{
		// Token: 0x060000F4 RID: 244 RVA: 0x00006060 File Offset: 0x00004260
		public SpanDebugView([Nullable(new byte[] { 0, 1 })] Span<T> span)
		{
			this._array = span.ToArray();
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x00006075 File Offset: 0x00004275
		public SpanDebugView([Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> span)
		{
			this._array = span.ToArray();
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x060000F6 RID: 246 RVA: 0x0000608A File Offset: 0x0000428A
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get
			{
				return this._array;
			}
		}

		// Token: 0x04000034 RID: 52
		private readonly T[] _array;
	}
}
