using System;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x02000029 RID: 41
	[NullableContext(1)]
	[Nullable(0)]
	public static class BuffersExtensions
	{
		// Token: 0x060001AA RID: 426 RVA: 0x00009D68 File Offset: 0x00007F68
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SequencePosition? PositionOf<[Nullable(0)] T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySequence<T> source, T value) where T : IEquatable<T>
		{
			if (!source.IsSingleSegment)
			{
				return BuffersExtensions.PositionOfMultiSegment<T>(source, value);
			}
			int num = source.First.Span.IndexOf(value);
			if (num != -1)
			{
				return new SequencePosition?(source.GetPosition((long)num));
			}
			return null;
		}

		// Token: 0x060001AB RID: 427 RVA: 0x00009DB8 File Offset: 0x00007FB8
		private static SequencePosition? PositionOfMultiSegment<[Nullable(0)] T>([Nullable(new byte[] { 0, 1 })] in ReadOnlySequence<T> source, T value) where T : IEquatable<T>
		{
			SequencePosition start = source.Start;
			SequencePosition origin = start;
			ReadOnlyMemory<T> readOnlyMemory;
			while (source.TryGet(ref start, out readOnlyMemory, true))
			{
				int num = readOnlyMemory.Span.IndexOf(value);
				if (num != -1)
				{
					return new SequencePosition?(source.GetPosition((long)num, origin));
				}
				if (start.GetObject() == null)
				{
					break;
				}
				origin = start;
			}
			return null;
		}

		// Token: 0x060001AC RID: 428 RVA: 0x00009E14 File Offset: 0x00008014
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

		// Token: 0x060001AD RID: 429 RVA: 0x00009E60 File Offset: 0x00008060
		[NullableContext(2)]
		private static void CopyToMultiSegment<T>([Nullable(new byte[] { 0, 1 })] in ReadOnlySequence<T> sequence, [Nullable(new byte[] { 0, 1 })] Span<T> destination)
		{
			SequencePosition start = sequence.Start;
			ReadOnlyMemory<T> readOnlyMemory;
			while (sequence.TryGet(ref start, out readOnlyMemory, true))
			{
				ReadOnlySpan<T> span = readOnlyMemory.Span;
				span.CopyTo(destination);
				if (start.GetObject() == null)
				{
					break;
				}
				destination = destination.Slice(span.Length);
			}
		}

		// Token: 0x060001AE RID: 430 RVA: 0x00009EAC File Offset: 0x000080AC
		public static T[] ToArray<[Nullable(2)] T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySequence<T> sequence)
		{
			T[] array = new T[sequence.Length];
			(sequence).CopyTo(array);
			return array;
		}

		// Token: 0x060001AF RID: 431 RVA: 0x00009ED4 File Offset: 0x000080D4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Write<[Nullable(2)] T>(this IBufferWriter<T> writer, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value)
		{
			ThrowHelper.ThrowIfArgumentNull(writer, "writer", null);
			Span<T> span = writer.GetSpan(0);
			if (value.Length <= span.Length)
			{
				value.CopyTo(span);
				writer.Advance(value.Length);
				return;
			}
			BuffersExtensions.WriteMultiSegment<T>(writer, value, span);
		}

		// Token: 0x060001B0 RID: 432 RVA: 0x00009F24 File Offset: 0x00008124
		private static void WriteMultiSegment<[Nullable(2)] T>(IBufferWriter<T> writer, [Nullable(new byte[] { 0, 1 })] in ReadOnlySpan<T> source, [Nullable(new byte[] { 0, 1 })] Span<T> destination)
		{
			ReadOnlySpan<T> readOnlySpan = source;
			for (;;)
			{
				int num = Math.Min(destination.Length, readOnlySpan.Length);
				readOnlySpan.Slice(0, num).CopyTo(destination);
				writer.Advance(num);
				readOnlySpan = readOnlySpan.Slice(num);
				if (readOnlySpan.Length <= 0)
				{
					break;
				}
				destination = writer.GetSpan(readOnlySpan.Length);
			}
		}
	}
}
