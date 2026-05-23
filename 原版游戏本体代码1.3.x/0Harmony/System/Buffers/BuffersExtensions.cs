using System;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x0200048E RID: 1166
	[NullableContext(1)]
	[Nullable(0)]
	internal static class BuffersExtensions
	{
		// Token: 0x06001A03 RID: 6659 RVA: 0x00054E18 File Offset: 0x00053018
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SequencePosition? PositionOf<[Nullable(0)] T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySequence<T> source, T value) where T : IEquatable<T>
		{
			if (!source.IsSingleSegment)
			{
				return BuffersExtensions.PositionOfMultiSegment<T>(source, value);
			}
			int index = source.First.Span.IndexOf(value);
			if (index != -1)
			{
				return new SequencePosition?(source.GetPosition((long)index));
			}
			return null;
		}

		// Token: 0x06001A04 RID: 6660 RVA: 0x00054E68 File Offset: 0x00053068
		private static SequencePosition? PositionOfMultiSegment<[Nullable(0)] T>([Nullable(new byte[] { 0, 1 })] in ReadOnlySequence<T> source, T value) where T : IEquatable<T>
		{
			SequencePosition position = source.Start;
			SequencePosition result = position;
			ReadOnlyMemory<T> memory;
			while (source.TryGet(ref position, out memory, true))
			{
				int index = memory.Span.IndexOf(value);
				if (index != -1)
				{
					return new SequencePosition?(source.GetPosition((long)index, result));
				}
				if (position.GetObject() == null)
				{
					break;
				}
				result = position;
			}
			return null;
		}

		// Token: 0x06001A05 RID: 6661 RVA: 0x00054EC4 File Offset: 0x000530C4
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CopyTo<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySequence<T> source, [Nullable(new byte[] { 0, 1 })] Span<T> destination)
		{
			if (source.Length > (long)destination.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.destination);
			}
			if (source.IsSingleSegment)
			{
				source.First.Span.CopyTo(destination);
				return;
			}
			BuffersExtensions.CopyToMultiSegment<T>(source, destination);
		}

		// Token: 0x06001A06 RID: 6662 RVA: 0x00054F10 File Offset: 0x00053110
		[NullableContext(2)]
		private static void CopyToMultiSegment<T>([Nullable(new byte[] { 0, 1 })] in ReadOnlySequence<T> sequence, [Nullable(new byte[] { 0, 1 })] Span<T> destination)
		{
			SequencePosition position = sequence.Start;
			ReadOnlyMemory<T> memory;
			while (sequence.TryGet(ref position, out memory, true))
			{
				ReadOnlySpan<T> span = memory.Span;
				span.CopyTo(destination);
				if (position.GetObject() == null)
				{
					break;
				}
				destination = destination.Slice(span.Length);
			}
		}

		// Token: 0x06001A07 RID: 6663 RVA: 0x00054F5C File Offset: 0x0005315C
		public static T[] ToArray<[Nullable(2)] T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySequence<T> sequence)
		{
			T[] array = new T[sequence.Length];
			(sequence).CopyTo(array);
			return array;
		}

		// Token: 0x06001A08 RID: 6664 RVA: 0x00054F84 File Offset: 0x00053184
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Write<[Nullable(2)] T>(this IBufferWriter<T> writer, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value)
		{
			ThrowHelper.ThrowIfArgumentNull(writer, "writer", null);
			Span<T> destination = writer.GetSpan(0);
			if (value.Length <= destination.Length)
			{
				value.CopyTo(destination);
				writer.Advance(value.Length);
				return;
			}
			BuffersExtensions.WriteMultiSegment<T>(writer, value, destination);
		}

		// Token: 0x06001A09 RID: 6665 RVA: 0x00054FD4 File Offset: 0x000531D4
		private static void WriteMultiSegment<[Nullable(2)] T>(IBufferWriter<T> writer, [Nullable(new byte[] { 0, 1 })] in ReadOnlySpan<T> source, [Nullable(new byte[] { 0, 1 })] Span<T> destination)
		{
			ReadOnlySpan<T> input = source;
			for (;;)
			{
				int writeSize = Math.Min(destination.Length, input.Length);
				input.Slice(0, writeSize).CopyTo(destination);
				writer.Advance(writeSize);
				input = input.Slice(writeSize);
				if (input.Length <= 0)
				{
					break;
				}
				destination = writer.GetSpan(input.Length);
			}
		}
	}
}
