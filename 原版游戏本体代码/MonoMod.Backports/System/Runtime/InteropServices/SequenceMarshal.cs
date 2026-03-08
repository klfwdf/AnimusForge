using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000039 RID: 57
	[NullableContext(2)]
	[Nullable(0)]
	public static class SequenceMarshal
	{
		// Token: 0x0600023E RID: 574 RVA: 0x0000BDED File Offset: 0x00009FED
		public static bool TryGetReadOnlySequenceSegment<T>([Nullable(new byte[] { 0, 1 })] ReadOnlySequence<T> sequence, [Nullable(new byte[] { 2, 1 })] out ReadOnlySequenceSegment<T> startSegment, out int startIndex, [Nullable(new byte[] { 2, 1 })] out ReadOnlySequenceSegment<T> endSegment, out int endIndex)
		{
			return sequence.TryGetReadOnlySequenceSegment(out startSegment, out startIndex, out endSegment, out endIndex);
		}

		// Token: 0x0600023F RID: 575 RVA: 0x0000BDFB File Offset: 0x00009FFB
		public static bool TryGetArray<T>([Nullable(new byte[] { 0, 1 })] ReadOnlySequence<T> sequence, [Nullable(new byte[] { 0, 1 })] out ArraySegment<T> segment)
		{
			return sequence.TryGetArray(out segment);
		}

		// Token: 0x06000240 RID: 576 RVA: 0x0000BE05 File Offset: 0x0000A005
		public static bool TryGetReadOnlyMemory<T>([Nullable(new byte[] { 0, 1 })] ReadOnlySequence<T> sequence, [Nullable(new byte[] { 0, 1 })] out ReadOnlyMemory<T> memory)
		{
			if (!sequence.IsSingleSegment)
			{
				memory = default(ReadOnlyMemory<T>);
				return false;
			}
			memory = sequence.First;
			return true;
		}

		// Token: 0x06000241 RID: 577 RVA: 0x0000BE27 File Offset: 0x0000A027
		[NullableContext(0)]
		internal static bool TryGetString(ReadOnlySequence<char> sequence, [Nullable(1)] [MaybeNullWhen(false)] out string text, out int start, out int length)
		{
			return sequence.TryGetString(out text, out start, out length);
		}
	}
}
