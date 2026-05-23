using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000038 RID: 56
	public static class MemoryMarshal
	{
		// Token: 0x0600022D RID: 557 RVA: 0x0000B9A0 File Offset: 0x00009BA0
		[NullableContext(2)]
		public static bool TryGetArray<T>([Nullable(new byte[] { 0, 1 })] ReadOnlyMemory<T> memory, [Nullable(new byte[] { 0, 1 })] out ArraySegment<T> segment)
		{
			int num;
			int num2;
			object objectStartLength = memory.GetObjectStartLength(out num, out num2);
			if (num < 0)
			{
				ArraySegment<T> arraySegment;
				if (((MemoryManager<T>)objectStartLength).TryGetArray(out arraySegment))
				{
					segment = new ArraySegment<T>(arraySegment.Array, arraySegment.Offset + (num & int.MaxValue), num2);
					return true;
				}
			}
			else
			{
				T[] array = objectStartLength as T[];
				if (array != null)
				{
					segment = new ArraySegment<T>(array, num, num2 & int.MaxValue);
					return true;
				}
			}
			if ((num2 & 2147483647) == 0)
			{
				segment = new ArraySegment<T>(SpanHelpers.PerTypeValues<T>.EmptyArray);
				return true;
			}
			segment = default(ArraySegment<T>);
			return false;
		}

		// Token: 0x0600022E RID: 558 RVA: 0x0000BA38 File Offset: 0x00009C38
		[NullableContext(2)]
		public static bool TryGetMemoryManager<T, [Nullable(0)] TManager>([Nullable(new byte[] { 0, 1 })] ReadOnlyMemory<T> memory, out TManager manager) where TManager : MemoryManager<T>
		{
			int num;
			int num2;
			return (manager = memory.GetObjectStartLength(out num, out num2) as TManager) != null;
		}

		// Token: 0x0600022F RID: 559 RVA: 0x0000BA6C File Offset: 0x00009C6C
		[NullableContext(2)]
		public static bool TryGetMemoryManager<T, [Nullable(0)] TManager>([Nullable(new byte[] { 0, 1 })] ReadOnlyMemory<T> memory, out TManager manager, out int start, out int length) where TManager : MemoryManager<T>
		{
			TManager tmanager = (manager = memory.GetObjectStartLength(out start, out length) as TManager);
			start &= int.MaxValue;
			if (tmanager == null)
			{
				start = 0;
				length = 0;
				return false;
			}
			return true;
		}

		// Token: 0x06000230 RID: 560 RVA: 0x0000BAAF File Offset: 0x00009CAF
		[NullableContext(1)]
		public unsafe static IEnumerable<T> ToEnumerable<[Nullable(2)] T>([Nullable(new byte[] { 0, 1 })] ReadOnlyMemory<T> memory)
		{
			int num;
			for (int i = 0; i < memory.Length; i = num + 1)
			{
				yield return *memory.Span[i];
				num = i;
			}
			yield break;
		}

		// Token: 0x06000231 RID: 561 RVA: 0x0000BAC0 File Offset: 0x00009CC0
		public static bool TryGetString(ReadOnlyMemory<char> memory, [Nullable(2)] out string text, out int start, out int length)
		{
			int num;
			int num2;
			string text2 = memory.GetObjectStartLength(out num, out num2) as string;
			if (text2 != null)
			{
				text = text2;
				start = num;
				length = num2;
				return true;
			}
			text = null;
			start = 0;
			length = 0;
			return false;
		}

		// Token: 0x06000232 RID: 562 RVA: 0x0000BAF6 File Offset: 0x00009CF6
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Read<T>(ReadOnlySpan<byte> source) where T : struct
		{
			if (SpanHelpers.IsReferenceOrContainsReferences<T>())
			{
				ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
			}
			if (Unsafe.SizeOf<T>() > source.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
			}
			return Unsafe.ReadUnaligned<T>(MemoryMarshal.GetReference<byte>(source));
		}

		// Token: 0x06000233 RID: 563 RVA: 0x0000BB30 File Offset: 0x00009D30
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryRead<T>(ReadOnlySpan<byte> source, out T value) where T : struct
		{
			if (SpanHelpers.IsReferenceOrContainsReferences<T>())
			{
				ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
			}
			if ((long)Unsafe.SizeOf<T>() > (long)((ulong)source.Length))
			{
				value = default(T);
				return false;
			}
			value = Unsafe.ReadUnaligned<T>(MemoryMarshal.GetReference<byte>(source));
			return true;
		}

		// Token: 0x06000234 RID: 564 RVA: 0x0000BB7E File Offset: 0x00009D7E
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Write<T>(Span<byte> destination, ref T value) where T : struct
		{
			if (SpanHelpers.IsReferenceOrContainsReferences<T>())
			{
				ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
			}
			if (Unsafe.SizeOf<T>() > destination.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
			}
			Unsafe.WriteUnaligned<T>(MemoryMarshal.GetReference<byte>(destination), value);
		}

		// Token: 0x06000235 RID: 565 RVA: 0x0000BBBB File Offset: 0x00009DBB
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryWrite<T>(Span<byte> destination, ref T value) where T : struct
		{
			if (SpanHelpers.IsReferenceOrContainsReferences<T>())
			{
				ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
			}
			if ((long)Unsafe.SizeOf<T>() > (long)((ulong)destination.Length))
			{
				return false;
			}
			Unsafe.WriteUnaligned<T>(MemoryMarshal.GetReference<byte>(destination), value);
			return true;
		}

		// Token: 0x06000236 RID: 566 RVA: 0x0000BBF8 File Offset: 0x00009DF8
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Memory<T> CreateFromPinnedArray<[Nullable(2)] T>(T[] array, int start, int length)
		{
			if (array == null)
			{
				if (start != 0 || length != 0)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException();
				}
				return default(Memory<T>);
			}
			if (default(T) == null && array.GetType() != typeof(T[]))
			{
				ThrowHelper.ThrowArrayTypeMismatchException();
			}
			if (start > array.Length || length > array.Length - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			return new Memory<T>(array, start, length | int.MinValue);
		}

		// Token: 0x06000237 RID: 567 RVA: 0x0000BC6C File Offset: 0x00009E6C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<byte> AsBytes<T>(Span<T> span) where T : struct
		{
			if (SpanHelpers.IsReferenceOrContainsReferences<T>())
			{
				ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
			}
			int length = checked(span.Length * Unsafe.SizeOf<T>());
			return new Span<byte>(span.Pinnable, span.ByteOffset, length);
		}

		// Token: 0x06000238 RID: 568 RVA: 0x0000BCB4 File Offset: 0x00009EB4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<byte> AsBytes<T>(ReadOnlySpan<T> span) where T : struct
		{
			if (SpanHelpers.IsReferenceOrContainsReferences<T>())
			{
				ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
			}
			int length = checked(span.Length * Unsafe.SizeOf<T>());
			return new ReadOnlySpan<byte>(span.Pinnable, span.ByteOffset, length);
		}

		// Token: 0x06000239 RID: 569 RVA: 0x0000BCF9 File Offset: 0x00009EF9
		[NullableContext(2)]
		[return: Nullable(new byte[] { 0, 1 })]
		public unsafe static Memory<T> AsMemory<T>([Nullable(new byte[] { 0, 1 })] ReadOnlyMemory<T> memory)
		{
			return *Unsafe.As<ReadOnlyMemory<T>, Memory<T>>(ref memory);
		}

		// Token: 0x0600023A RID: 570 RVA: 0x0000BD07 File Offset: 0x00009F07
		[NullableContext(1)]
		public static ref T GetReference<[Nullable(2)] T>([Nullable(new byte[] { 0, 1 })] Span<T> span)
		{
			return span.DangerousGetPinnableReference();
		}

		// Token: 0x0600023B RID: 571 RVA: 0x0000BD10 File Offset: 0x00009F10
		[NullableContext(1)]
		public static ref T GetReference<[Nullable(2)] T>([Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> span)
		{
			return Unsafe.AsRef<T>(span.GetPinnableReference());
		}

		// Token: 0x0600023C RID: 572 RVA: 0x0000BD20 File Offset: 0x00009F20
		public static Span<TTo> Cast<TFrom, TTo>(Span<TFrom> span) where TFrom : struct where TTo : struct
		{
			if (SpanHelpers.IsReferenceOrContainsReferences<TFrom>())
			{
				ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(TFrom));
			}
			if (SpanHelpers.IsReferenceOrContainsReferences<TTo>())
			{
				ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(TTo));
			}
			int length = checked((int)(unchecked((long)span.Length) * unchecked((long)Unsafe.SizeOf<TFrom>()) / unchecked((long)Unsafe.SizeOf<TTo>())));
			return new Span<TTo>(span.Pinnable, span.ByteOffset, length);
		}

		// Token: 0x0600023D RID: 573 RVA: 0x0000BD88 File Offset: 0x00009F88
		public static ReadOnlySpan<TTo> Cast<TFrom, TTo>(ReadOnlySpan<TFrom> span) where TFrom : struct where TTo : struct
		{
			if (SpanHelpers.IsReferenceOrContainsReferences<TFrom>())
			{
				ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(TFrom));
			}
			if (SpanHelpers.IsReferenceOrContainsReferences<TTo>())
			{
				ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(TTo));
			}
			int length = checked((int)(unchecked((long)span.Length) * unchecked((long)Unsafe.SizeOf<TFrom>()) / unchecked((long)Unsafe.SizeOf<TTo>())));
			return new ReadOnlySpan<TTo>(span.Pinnable, span.ByteOffset, length);
		}
	}
}
