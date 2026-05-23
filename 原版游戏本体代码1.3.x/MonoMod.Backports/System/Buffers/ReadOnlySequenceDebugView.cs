using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x02000032 RID: 50
	internal sealed class ReadOnlySequenceDebugView<[Nullable(2)] T>
	{
		// Token: 0x060001FB RID: 507 RVA: 0x0000B0DC File Offset: 0x000092DC
		public ReadOnlySequenceDebugView([Nullable(new byte[] { 0, 1 })] ReadOnlySequence<T> sequence)
		{
			this._array = (sequence).ToArray<T>();
			int num = 0;
			foreach (ReadOnlyMemory<T> readOnlyMemory in sequence)
			{
				num++;
			}
			ReadOnlyMemory<T>[] array = new ReadOnlyMemory<T>[num];
			int num2 = 0;
			foreach (ReadOnlyMemory<T> readOnlyMemory2 in sequence)
			{
				array[num2] = readOnlyMemory2;
				num2++;
			}
			this._segments = new ReadOnlySequenceDebugView<T>.ReadOnlySequenceDebugViewSegments
			{
				Segments = array
			};
		}

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x060001FC RID: 508 RVA: 0x0000B167 File Offset: 0x00009367
		public ReadOnlySequenceDebugView<T>.ReadOnlySequenceDebugViewSegments BufferSegments
		{
			get
			{
				return this._segments;
			}
		}

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x060001FD RID: 509 RVA: 0x0000B16F File Offset: 0x0000936F
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

		// Token: 0x04000066 RID: 102
		[Nullable(1)]
		private readonly T[] _array;

		// Token: 0x04000067 RID: 103
		private readonly ReadOnlySequenceDebugView<T>.ReadOnlySequenceDebugViewSegments _segments;

		// Token: 0x02000065 RID: 101
		[DebuggerDisplay("Count: {Segments.Length}", Name = "Segments")]
		public struct ReadOnlySequenceDebugViewSegments
		{
			// Token: 0x17000046 RID: 70
			// (get) Token: 0x060002D5 RID: 725 RVA: 0x0000CFEF File Offset: 0x0000B1EF
			// (set) Token: 0x060002D6 RID: 726 RVA: 0x0000CFF7 File Offset: 0x0000B1F7
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
