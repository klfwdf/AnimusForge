using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
	// Token: 0x020004A7 RID: 1191
	internal static class MemoryMarshal
	{
		// Token: 0x06001AA0 RID: 6816 RVA: 0x00056F94 File Offset: 0x00055194
		[NullableContext(2)]
		public static bool TryGetArray<T>([Nullable(new byte[] { 0, 1 })] ReadOnlyMemory<T> memory, [Nullable(new byte[] { 0, 1 })] out ArraySegment<T> segment)
		{
			int index;
			int length;
			object obj = memory.GetObjectStartLength(out index, out length);
			if (index < 0)
			{
				ArraySegment<T> arraySegment;
				if (((MemoryManager<T>)obj).TryGetArray(out arraySegment))
				{
					segment = new ArraySegment<T>(arraySegment.Array, arraySegment.Offset + (index & int.MaxValue), length);
					return true;
				}
			}
			else
			{
				T[] arr = obj as T[];
				if (arr != null)
				{
					segment = new ArraySegment<T>(arr, index, length & int.MaxValue);
					return true;
				}
			}
			if ((length & 2147483647) == 0)
			{
				segment = new ArraySegment<T>(SpanHelpers.PerTypeValues<T>.EmptyArray);
				return true;
			}
			segment = default(ArraySegment<T>);
			return false;
		}

		// Token: 0x06001AA1 RID: 6817 RVA: 0x0005702C File Offset: 0x0005522C
		[NullableContext(2)]
		public static bool TryGetMemoryManager<T, [Nullable(0)] TManager>([Nullable(new byte[] { 0, 1 })] ReadOnlyMemory<T> memory, out TManager manager) where TManager : MemoryManager<T>
		{
			int num;
			int num2;
			return (manager = memory.GetObjectStartLength(out num, out num2) as TManager) != null;
		}

		// Token: 0x06001AA2 RID: 6818 RVA: 0x00057060 File Offset: 0x00055260
		[NullableContext(2)]
		public static bool TryGetMemoryManager<T, [Nullable(0)] TManager>([Nullable(new byte[] { 0, 1 })] ReadOnlyMemory<T> memory, out TManager manager, out int start, out int length) where TManager : MemoryManager<T>
		{
			TManager localManager = (manager = memory.GetObjectStartLength(out start, out length) as TManager);
			start &= int.MaxValue;
			if (localManager == null)
			{
				start = 0;
				length = 0;
				return false;
			}
			return true;
		}

		// Token: 0x06001AA3 RID: 6819 RVA: 0x000570A3 File Offset: 0x000552A3
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

		// Token: 0x06001AA4 RID: 6820 RVA: 0x000570B4 File Offset: 0x000552B4
		public static bool TryGetString(ReadOnlyMemory<char> memory, [Nullable(2)] out string text, out int start, out int length)
		{
			int offset;
			int count;
			string s = memory.GetObjectStartLength(out offset, out count) as string;
			if (s != null)
			{
				text = s;
				start = offset;
				length = count;
				return true;
			}
			text = null;
			start = 0;
			length = 0;
			return false;
		}

		// Token: 0x06001AA5 RID: 6821 RVA: 0x000570EA File Offset: 0x000552EA
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

		// Token: 0x06001AA6 RID: 6822 RVA: 0x00057124 File Offset: 0x00055324
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

		// Token: 0x06001AA7 RID: 6823 RVA: 0x00057172 File Offset: 0x00055372
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

		// Token: 0x06001AA8 RID: 6824 RVA: 0x000571AF File Offset: 0x000553AF
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

		// Token: 0x06001AA9 RID: 6825 RVA: 0x000571EC File Offset: 0x000553EC
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

		// Token: 0x06001AAA RID: 6826 RVA: 0x00057260 File Offset: 0x00055460
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<byte> AsBytes<T>(Span<T> span) where T : struct
		{
			if (SpanHelpers.IsReferenceOrContainsReferences<T>())
			{
				ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
			}
			int newLength = checked(span.Length * Unsafe.SizeOf<T>());
			return new Span<byte>(span.Pinnable, span.ByteOffset, newLength);
		}

		// Token: 0x06001AAB RID: 6827 RVA: 0x000572A8 File Offset: 0x000554A8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<byte> AsBytes<T>(ReadOnlySpan<T> span) where T : struct
		{
			if (SpanHelpers.IsReferenceOrContainsReferences<T>())
			{
				ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
			}
			int newLength = checked(span.Length * Unsafe.SizeOf<T>());
			return new ReadOnlySpan<byte>(span.Pinnable, span.ByteOffset, newLength);
		}

		// Token: 0x06001AAC RID: 6828 RVA: 0x000572ED File Offset: 0x000554ED
		[NullableContext(2)]
		[return: Nullable(new byte[] { 0, 1 })]
		public unsafe static Memory<T> AsMemory<T>([Nullable(new byte[] { 0, 1 })] ReadOnlyMemory<T> memory)
		{
			return *Unsafe.As<ReadOnlyMemory<T>, Memory<T>>(ref memory);
		}

		// Token: 0x06001AAD RID: 6829 RVA: 0x000572FB File Offset: 0x000554FB
		[NullableContext(1)]
		public static ref T GetReference<[Nullable(2)] T>([Nullable(new byte[] { 0, 1 })] Span<T> span)
		{
			return span.DangerousGetPinnableReference();
		}

		// Token: 0x06001AAE RID: 6830 RVA: 0x00057304 File Offset: 0x00055504
		[NullableContext(1)]
		public static ref T GetReference<[Nullable(2)] T>([Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> span)
		{
			return Unsafe.AsRef<T>(span.GetPinnableReference());
		}

		// Token: 0x06001AAF RID: 6831 RVA: 0x00057314 File Offset: 0x00055514
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
			int newLength = checked((int)(unchecked((long)span.Length) * unchecked((long)Unsafe.SizeOf<TFrom>()) / unchecked((long)Unsafe.SizeOf<TTo>())));
			return new Span<TTo>(span.Pinnable, span.ByteOffset, newLength);
		}

		// Token: 0x06001AB0 RID: 6832 RVA: 0x0005737C File Offset: 0x0005557C
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
			int newLength = checked((int)(unchecked((long)span.Length) * unchecked((long)Unsafe.SizeOf<TFrom>()) / unchecked((long)Unsafe.SizeOf<TTo>())));
			return new ReadOnlySpan<TTo>(span.Pinnable, span.ByteOffset, newLength);
		}
	}
}
