using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
	// Token: 0x020004A9 RID: 1193
	[NullableContext(2)]
	[Nullable(0)]
	internal static class SequenceMarshal
	{
		// Token: 0x06001AB9 RID: 6841 RVA: 0x000574E3 File Offset: 0x000556E3
		public static bool TryGetReadOnlySequenceSegment<T>([Nullable(new byte[] { 0, 1 })] ReadOnlySequence<T> sequence, [Nullable(new byte[] { 2, 1 })] out ReadOnlySequenceSegment<T> startSegment, out int startIndex, [Nullable(new byte[] { 2, 1 })] out ReadOnlySequenceSegment<T> endSegment, out int endIndex)
		{
			return sequence.TryGetReadOnlySequenceSegment(out startSegment, out startIndex, out endSegment, out endIndex);
		}

		// Token: 0x06001ABA RID: 6842 RVA: 0x000574F1 File Offset: 0x000556F1
		public static bool TryGetArray<T>([Nullable(new byte[] { 0, 1 })] ReadOnlySequence<T> sequence, [Nullable(new byte[] { 0, 1 })] out ArraySegment<T> segment)
		{
			return sequence.TryGetArray(out segment);
		}

		// Token: 0x06001ABB RID: 6843 RVA: 0x000574FB File Offset: 0x000556FB
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

		// Token: 0x06001ABC RID: 6844 RVA: 0x0005751D File Offset: 0x0005571D
		[NullableContext(0)]
		internal static bool TryGetString(ReadOnlySequence<char> sequence, [Nullable(1)] [<1c2fb156-e9ba-45cc-af54-d5335bdb59af>MaybeNullWhen(false)] out string text, out int start, out int length)
		{
			return sequence.TryGetString(out text, out start, out length);
		}
	}
}
