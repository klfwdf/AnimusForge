using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x0200049A RID: 1178
	internal sealed class ReadOnlySequenceDebugView<[Nullable(2)] T>
	{
		// Token: 0x06001A5B RID: 6747 RVA: 0x000562E8 File Offset: 0x000544E8
		public ReadOnlySequenceDebugView([Nullable(new byte[] { 0, 1 })] ReadOnlySequence<T> sequence)
		{
			this._array = (sequence).ToArray<T>();
			int segmentCount = 0;
			foreach (ReadOnlyMemory<T> readOnlyMemory2 in sequence)
			{
				segmentCount++;
			}
			ReadOnlyMemory<T>[] segments = new ReadOnlyMemory<T>[segmentCount];
			int i = 0;
			foreach (ReadOnlyMemory<T> readOnlyMemory in sequence)
			{
				segments[i] = readOnlyMemory;
				i++;
			}
			this._segments = new ReadOnlySequenceDebugView<T>.ReadOnlySequenceDebugViewSegments
			{
				Segments = segments
			};
		}

		// Token: 0x170005CB RID: 1483
		// (get) Token: 0x06001A5C RID: 6748 RVA: 0x00056373 File Offset: 0x00054573
		public ReadOnlySequenceDebugView<T>.ReadOnlySequenceDebugViewSegments BufferSegments
		{
			get
			{
				return this._segments;
			}
		}

		// Token: 0x170005CC RID: 1484
		// (get) Token: 0x06001A5D RID: 6749 RVA: 0x0005637B File Offset: 0x0005457B
		[Nullable(1)]
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			[NullableContext(1)]
			get
			{
				return this._array;
			}
		}

		// Token: 0x040010F3 RID: 4339
		[Nullable(1)]
		private readonly T[] _array;

		// Token: 0x040010F4 RID: 4340
		private readonly ReadOnlySequenceDebugView<T>.ReadOnlySequenceDebugViewSegments _segments;

		// Token: 0x0200049B RID: 1179
		[DebuggerDisplay("Count: {Segments.Length}", Name = "Segments")]
		public struct ReadOnlySequenceDebugViewSegments
		{
			// Token: 0x170005CD RID: 1485
			// (get) Token: 0x06001A5E RID: 6750 RVA: 0x00056383 File Offset: 0x00054583
			// (set) Token: 0x06001A5F RID: 6751 RVA: 0x0005638B File Offset: 0x0005458B
			[Nullable(new byte[] { 1, 0, 1 })]
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public ReadOnlyMemory<T>[] Segments
			{
				[return: Nullable(new byte[] { 1, 0, 1 })]
				readonly get;
				[param: Nullable(new byte[] { 1, 0, 1 })]
				set;
			}
		}
	}
}
