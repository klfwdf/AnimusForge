using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x02000476 RID: 1142
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class SpanDebugView<[Nullable(2)] T>
	{
		// Token: 0x06001941 RID: 6465 RVA: 0x00051089 File Offset: 0x0004F289
		public SpanDebugView([Nullable(new byte[] { 0, 1 })] Span<T> span)
		{
			this._array = span.ToArray();
		}

		// Token: 0x06001942 RID: 6466 RVA: 0x0005109E File Offset: 0x0004F29E
		public SpanDebugView([Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> span)
		{
			this._array = span.ToArray();
		}

		// Token: 0x170005BB RID: 1467
		// (get) Token: 0x06001943 RID: 6467 RVA: 0x000510B3 File Offset: 0x0004F2B3
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get
			{
				return this._array;
			}
		}

		// Token: 0x040010A8 RID: 4264
		private readonly T[] _array;
	}
}
