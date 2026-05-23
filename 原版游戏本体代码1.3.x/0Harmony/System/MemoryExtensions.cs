using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x02000470 RID: 1136
	internal static class MemoryExtensions
	{
		// Token: 0x0600189F RID: 6303 RVA: 0x0004E744 File Offset: 0x0004C944
		public static ReadOnlySpan<char> Trim(this ReadOnlySpan<char> span)
		{
			return span.TrimStart().TrimEnd();
		}

		// Token: 0x060018A0 RID: 6304 RVA: 0x0004E754 File Offset: 0x0004C954
		public unsafe static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> span)
		{
			int start = 0;
			while (start < span.Length && char.IsWhiteSpace((char)(*span[start])))
			{
				start++;
			}
			return span.Slice(start);
		}

		// Token: 0x060018A1 RID: 6305 RVA: 0x0004E78C File Offset: 0x0004C98C
		public unsafe static ReadOnlySpan<char> TrimEnd(this ReadOnlySpan<char> span)
		{
			int end = span.Length - 1;
			while (end >= 0 && char.IsWhiteSpace((char)(*span[end])))
			{
				end--;
			}
			return span.Slice(0, end + 1);
		}

		// Token: 0x060018A2 RID: 6306 RVA: 0x0004E7C8 File Offset: 0x0004C9C8
		public static ReadOnlySpan<char> Trim(this ReadOnlySpan<char> span, char trimChar)
		{
			return span.TrimStart(trimChar).TrimEnd(trimChar);
		}

		// Token: 0x060018A3 RID: 6307 RVA: 0x0004E7D8 File Offset: 0x0004C9D8
		public unsafe static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> span, char trimChar)
		{
			int start = 0;
			while (start < span.Length && *span[start] == (ushort)trimChar)
			{
				start++;
			}
			return span.Slice(start);
		}

		// Token: 0x060018A4 RID: 6308 RVA: 0x0004E80C File Offset: 0x0004CA0C
		public unsafe static ReadOnlySpan<char> TrimEnd(this ReadOnlySpan<char> span, char trimChar)
		{
			int end = span.Length - 1;
			while (end >= 0 && *span[end] == (ushort)trimChar)
			{
				end--;
			}
			return span.Slice(0, end + 1);
		}

		// Token: 0x060018A5 RID: 6309 RVA: 0x0004E844 File Offset: 0x0004CA44
		public static ReadOnlySpan<char> Trim(this ReadOnlySpan<char> span, ReadOnlySpan<char> trimChars)
		{
			return span.TrimStart(trimChars).TrimEnd(trimChars);
		}

		// Token: 0x060018A6 RID: 6310 RVA: 0x0004E854 File Offset: 0x0004CA54
		public unsafe static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> span, ReadOnlySpan<char> trimChars)
		{
			if (trimChars.IsEmpty)
			{
				return span.TrimStart();
			}
			int start = 0;
			IL_40:
			while (start < span.Length)
			{
				for (int i = 0; i < trimChars.Length; i++)
				{
					if (*span[start] == *trimChars[i])
					{
						start++;
						goto IL_40;
					}
				}
				break;
			}
			return span.Slice(start);
		}

		// Token: 0x060018A7 RID: 6311 RVA: 0x0004E8B4 File Offset: 0x0004CAB4
		public unsafe static ReadOnlySpan<char> TrimEnd(this ReadOnlySpan<char> span, ReadOnlySpan<char> trimChars)
		{
			if (trimChars.IsEmpty)
			{
				return span.TrimEnd();
			}
			int end = span.Length - 1;
			IL_48:
			while (end >= 0)
			{
				for (int i = 0; i < trimChars.Length; i++)
				{
					if (*span[end] == *trimChars[i])
					{
						end--;
						goto IL_48;
					}
				}
				break;
			}
			return span.Slice(0, end + 1);
		}

		// Token: 0x060018A8 RID: 6312 RVA: 0x0004E918 File Offset: 0x0004CB18
		public unsafe static bool IsWhiteSpace(this ReadOnlySpan<char> span)
		{
			for (int i = 0; i < span.Length; i++)
			{
				if (!char.IsWhiteSpace((char)(*span[i])))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060018A9 RID: 6313 RVA: 0x0004E94C File Offset: 0x0004CB4C
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int IndexOf<[Nullable(0)] T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, T value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOf(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value), span.Length);
			}
			if (typeof(T) == typeof(char))
			{
				return SpanHelpers.IndexOf(Unsafe.As<T, char>(MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, char>(ref value), span.Length);
			}
			return SpanHelpers.IndexOf<T>(MemoryMarshal.GetReference<T>(span), value, span.Length);
		}

		// Token: 0x060018AA RID: 6314 RVA: 0x0004E9E4 File Offset: 0x0004CBE4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOf<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOf(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(value)), value.Length);
			}
			return SpanHelpers.IndexOf<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(value), value.Length);
		}

		// Token: 0x060018AB RID: 6315 RVA: 0x0004EA58 File Offset: 0x0004CC58
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int LastIndexOf<[Nullable(0)] T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, T value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOf(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value), span.Length);
			}
			if (typeof(T) == typeof(char))
			{
				return SpanHelpers.LastIndexOf(Unsafe.As<T, char>(MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, char>(ref value), span.Length);
			}
			return SpanHelpers.LastIndexOf<T>(MemoryMarshal.GetReference<T>(span), value, span.Length);
		}

		// Token: 0x060018AC RID: 6316 RVA: 0x0004EAF0 File Offset: 0x0004CCF0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LastIndexOf<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOf(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(value)), value.Length);
			}
			return SpanHelpers.LastIndexOf<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(value), value.Length);
		}

		// Token: 0x060018AD RID: 6317 RVA: 0x0004EB64 File Offset: 0x0004CD64
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool SequenceEqual<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> other) where T : IEquatable<T>
		{
			int length = span.Length;
			UIntPtr size;
			if (default(T) != null && MemoryExtensions.IsTypeComparableAsBytes<T>(out size))
			{
				return length == other.Length && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(other)), (UIntPtr)((IntPtr)length * (IntPtr)size));
			}
			return length == other.Length && SpanHelpers.SequenceEqual<T>(MemoryMarshal.GetReference<T>(span), MemoryMarshal.GetReference<T>(other), length);
		}

		// Token: 0x060018AE RID: 6318 RVA: 0x0004EBDC File Offset: 0x0004CDDC
		public static int SequenceCompareTo<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> other) where T : IComparable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.SequenceCompareTo(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(other)), other.Length);
			}
			if (typeof(T) == typeof(char))
			{
				return SpanHelpers.SequenceCompareTo(Unsafe.As<T, char>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, char>(MemoryMarshal.GetReference<T>(other)), other.Length);
			}
			return SpanHelpers.SequenceCompareTo<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(other), other.Length);
		}

		// Token: 0x060018AF RID: 6319 RVA: 0x0004EC94 File Offset: 0x0004CE94
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int IndexOf<[Nullable(0)] T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOf(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value), span.Length);
			}
			if (typeof(T) == typeof(char))
			{
				return SpanHelpers.IndexOf(Unsafe.As<T, char>(MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, char>(ref value), span.Length);
			}
			return SpanHelpers.IndexOf<T>(MemoryMarshal.GetReference<T>(span), value, span.Length);
		}

		// Token: 0x060018B0 RID: 6320 RVA: 0x0004ED2C File Offset: 0x0004CF2C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOf<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOf(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(value)), value.Length);
			}
			return SpanHelpers.IndexOf<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(value), value.Length);
		}

		// Token: 0x060018B1 RID: 6321 RVA: 0x0004EDA0 File Offset: 0x0004CFA0
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int LastIndexOf<[Nullable(0)] T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOf(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value), span.Length);
			}
			if (typeof(T) == typeof(char))
			{
				return SpanHelpers.LastIndexOf(Unsafe.As<T, char>(MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, char>(ref value), span.Length);
			}
			return SpanHelpers.LastIndexOf<T>(MemoryMarshal.GetReference<T>(span), value, span.Length);
		}

		// Token: 0x060018B2 RID: 6322 RVA: 0x0004EE38 File Offset: 0x0004D038
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LastIndexOf<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOf(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(value)), value.Length);
			}
			return SpanHelpers.LastIndexOf<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(value), value.Length);
		}

		// Token: 0x060018B3 RID: 6323 RVA: 0x0004EEAC File Offset: 0x0004D0AC
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int IndexOfAny<[Nullable(0)] T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, T value0, T value1) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOfAny(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value0), *Unsafe.As<T, byte>(ref value1), span.Length);
			}
			return SpanHelpers.IndexOfAny<T>(MemoryMarshal.GetReference<T>(span), value0, value1, span.Length);
		}

		// Token: 0x060018B4 RID: 6324 RVA: 0x0004EF10 File Offset: 0x0004D110
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int IndexOfAny<[Nullable(0)] T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, T value0, T value1, T value2) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOfAny(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value0), *Unsafe.As<T, byte>(ref value1), *Unsafe.As<T, byte>(ref value2), span.Length);
			}
			return SpanHelpers.IndexOfAny<T>(MemoryMarshal.GetReference<T>(span), value0, value1, value2, span.Length);
		}

		// Token: 0x060018B5 RID: 6325 RVA: 0x0004EF80 File Offset: 0x0004D180
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOfAny<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> values) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOfAny(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(values)), values.Length);
			}
			return SpanHelpers.IndexOfAny<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(values), values.Length);
		}

		// Token: 0x060018B6 RID: 6326 RVA: 0x0004EFF4 File Offset: 0x0004D1F4
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int IndexOfAny<[Nullable(0)] T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, T value0, T value1) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOfAny(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value0), *Unsafe.As<T, byte>(ref value1), span.Length);
			}
			return SpanHelpers.IndexOfAny<T>(MemoryMarshal.GetReference<T>(span), value0, value1, span.Length);
		}

		// Token: 0x060018B7 RID: 6327 RVA: 0x0004F058 File Offset: 0x0004D258
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int IndexOfAny<[Nullable(0)] T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, T value0, T value1, T value2) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOfAny(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value0), *Unsafe.As<T, byte>(ref value1), *Unsafe.As<T, byte>(ref value2), span.Length);
			}
			return SpanHelpers.IndexOfAny<T>(MemoryMarshal.GetReference<T>(span), value0, value1, value2, span.Length);
		}

		// Token: 0x060018B8 RID: 6328 RVA: 0x0004F0C8 File Offset: 0x0004D2C8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOfAny<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> values) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOfAny(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(values)), values.Length);
			}
			return SpanHelpers.IndexOfAny<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(values), values.Length);
		}

		// Token: 0x060018B9 RID: 6329 RVA: 0x0004F13C File Offset: 0x0004D33C
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int LastIndexOfAny<[Nullable(0)] T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, T value0, T value1) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOfAny(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value0), *Unsafe.As<T, byte>(ref value1), span.Length);
			}
			return SpanHelpers.LastIndexOfAny<T>(MemoryMarshal.GetReference<T>(span), value0, value1, span.Length);
		}

		// Token: 0x060018BA RID: 6330 RVA: 0x0004F1A0 File Offset: 0x0004D3A0
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int LastIndexOfAny<[Nullable(0)] T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, T value0, T value1, T value2) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOfAny(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value0), *Unsafe.As<T, byte>(ref value1), *Unsafe.As<T, byte>(ref value2), span.Length);
			}
			return SpanHelpers.LastIndexOfAny<T>(MemoryMarshal.GetReference<T>(span), value0, value1, value2, span.Length);
		}

		// Token: 0x060018BB RID: 6331 RVA: 0x0004F210 File Offset: 0x0004D410
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LastIndexOfAny<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> values) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOfAny(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(values)), values.Length);
			}
			return SpanHelpers.LastIndexOfAny<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(values), values.Length);
		}

		// Token: 0x060018BC RID: 6332 RVA: 0x0004F284 File Offset: 0x0004D484
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int LastIndexOfAny<[Nullable(0)] T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, T value0, T value1) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOfAny(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value0), *Unsafe.As<T, byte>(ref value1), span.Length);
			}
			return SpanHelpers.LastIndexOfAny<T>(MemoryMarshal.GetReference<T>(span), value0, value1, span.Length);
		}

		// Token: 0x060018BD RID: 6333 RVA: 0x0004F2E8 File Offset: 0x0004D4E8
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int LastIndexOfAny<[Nullable(0)] T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, T value0, T value1, T value2) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOfAny(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value0), *Unsafe.As<T, byte>(ref value1), *Unsafe.As<T, byte>(ref value2), span.Length);
			}
			return SpanHelpers.LastIndexOfAny<T>(MemoryMarshal.GetReference<T>(span), value0, value1, value2, span.Length);
		}

		// Token: 0x060018BE RID: 6334 RVA: 0x0004F358 File Offset: 0x0004D558
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LastIndexOfAny<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> values) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOfAny(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(values)), values.Length);
			}
			return SpanHelpers.LastIndexOfAny<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(values), values.Length);
		}

		// Token: 0x060018BF RID: 6335 RVA: 0x0004F3CC File Offset: 0x0004D5CC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool SequenceEqual<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> other) where T : IEquatable<T>
		{
			int length = span.Length;
			UIntPtr size;
			if (default(T) != null && MemoryExtensions.IsTypeComparableAsBytes<T>(out size))
			{
				return length == other.Length && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(other)), (UIntPtr)((IntPtr)length * (IntPtr)size));
			}
			return length == other.Length && SpanHelpers.SequenceEqual<T>(MemoryMarshal.GetReference<T>(span), MemoryMarshal.GetReference<T>(other), length);
		}

		// Token: 0x060018C0 RID: 6336 RVA: 0x0004F444 File Offset: 0x0004D644
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SequenceCompareTo<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> other) where T : IComparable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.SequenceCompareTo(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(other)), other.Length);
			}
			if (typeof(T) == typeof(char))
			{
				return SpanHelpers.SequenceCompareTo(Unsafe.As<T, char>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, char>(MemoryMarshal.GetReference<T>(other)), other.Length);
			}
			return SpanHelpers.SequenceCompareTo<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(other), other.Length);
		}

		// Token: 0x060018C1 RID: 6337 RVA: 0x0004F4FC File Offset: 0x0004D6FC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool StartsWith<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			int valueLength = value.Length;
			UIntPtr size;
			if (default(T) != null && MemoryExtensions.IsTypeComparableAsBytes<T>(out size))
			{
				return valueLength <= span.Length && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(value)), (UIntPtr)((IntPtr)valueLength * (IntPtr)size));
			}
			return valueLength <= span.Length && SpanHelpers.SequenceEqual<T>(MemoryMarshal.GetReference<T>(span), MemoryMarshal.GetReference<T>(value), valueLength);
		}

		// Token: 0x060018C2 RID: 6338 RVA: 0x0004F574 File Offset: 0x0004D774
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool StartsWith<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			int valueLength = value.Length;
			UIntPtr size;
			if (default(T) != null && MemoryExtensions.IsTypeComparableAsBytes<T>(out size))
			{
				return valueLength <= span.Length && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(value)), (UIntPtr)((IntPtr)valueLength * (IntPtr)size));
			}
			return valueLength <= span.Length && SpanHelpers.SequenceEqual<T>(MemoryMarshal.GetReference<T>(span), MemoryMarshal.GetReference<T>(value), valueLength);
		}

		// Token: 0x060018C3 RID: 6339 RVA: 0x0004F5EC File Offset: 0x0004D7EC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool EndsWith<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			int spanLength = span.Length;
			int valueLength = value.Length;
			UIntPtr size;
			if (default(T) != null && MemoryExtensions.IsTypeComparableAsBytes<T>(out size))
			{
				return valueLength <= spanLength && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(Unsafe.Add<T>(MemoryMarshal.GetReference<T>(span), spanLength - valueLength)), Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(value)), (UIntPtr)((IntPtr)valueLength * (IntPtr)size));
			}
			return valueLength <= spanLength && SpanHelpers.SequenceEqual<T>(Unsafe.Add<T>(MemoryMarshal.GetReference<T>(span), spanLength - valueLength), MemoryMarshal.GetReference<T>(value), valueLength);
		}

		// Token: 0x060018C4 RID: 6340 RVA: 0x0004F670 File Offset: 0x0004D870
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool EndsWith<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			int spanLength = span.Length;
			int valueLength = value.Length;
			UIntPtr size;
			if (default(T) != null && MemoryExtensions.IsTypeComparableAsBytes<T>(out size))
			{
				return valueLength <= spanLength && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(Unsafe.Add<T>(MemoryMarshal.GetReference<T>(span), spanLength - valueLength)), Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(value)), (UIntPtr)((IntPtr)valueLength * (IntPtr)size));
			}
			return valueLength <= spanLength && SpanHelpers.SequenceEqual<T>(Unsafe.Add<T>(MemoryMarshal.GetReference<T>(span), spanLength - valueLength), MemoryMarshal.GetReference<T>(value), valueLength);
		}

		// Token: 0x060018C5 RID: 6341 RVA: 0x0004F6F4 File Offset: 0x0004D8F4
		[NullableContext(2)]
		public static void Reverse<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span)
		{
			if (span.Length <= 1)
			{
				return;
			}
			ref T first = ref MemoryMarshal.GetReference<T>(span);
			ref T last = ref Unsafe.Add<T>(Unsafe.Add<T>(ref first, span.Length), -1);
			do
			{
				T temp = first;
				first = last;
				last = temp;
				first = Unsafe.Add<T>(ref first, 1);
				last = Unsafe.Add<T>(ref last, -1);
			}
			while (Unsafe.IsAddressLessThan<T>(ref first, ref last));
		}

		// Token: 0x060018C6 RID: 6342 RVA: 0x0004F75A File Offset: 0x0004D95A
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Span<T> AsSpan<T>([Nullable(new byte[] { 2, 1 })] this T[] array)
		{
			return new Span<T>(array);
		}

		// Token: 0x060018C7 RID: 6343 RVA: 0x0004F762 File Offset: 0x0004D962
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Span<T> AsSpan<T>([Nullable(new byte[] { 2, 1 })] this T[] array, int start, int length)
		{
			return new Span<T>(array, start, length);
		}

		// Token: 0x060018C8 RID: 6344 RVA: 0x0004F76C File Offset: 0x0004D96C
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Span<T> AsSpan<T>([Nullable(new byte[] { 0, 1 })] this ArraySegment<T> segment)
		{
			return new Span<T>(segment.Array, segment.Offset, segment.Count);
		}

		// Token: 0x060018C9 RID: 6345 RVA: 0x0004F788 File Offset: 0x0004D988
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Span<T> AsSpan<T>([Nullable(new byte[] { 0, 1 })] this ArraySegment<T> segment, int start)
		{
			if ((ulong)start > (ulong)((long)segment.Count))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new Span<T>(segment.Array, segment.Offset + start, segment.Count - start);
		}

		// Token: 0x060018CA RID: 6346 RVA: 0x0004F7BA File Offset: 0x0004D9BA
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Span<T> AsSpan<T>([Nullable(new byte[] { 0, 1 })] this ArraySegment<T> segment, int start, int length)
		{
			if ((ulong)start > (ulong)((long)segment.Count))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			if ((ulong)length > (ulong)((long)(segment.Count - start)))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
			}
			return new Span<T>(segment.Array, segment.Offset + start, length);
		}

		// Token: 0x060018CB RID: 6347 RVA: 0x0004F7F8 File Offset: 0x0004D9F8
		[NullableContext(2)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Memory<T> AsMemory<T>([Nullable(new byte[] { 2, 1 })] this T[] array)
		{
			return new Memory<T>(array);
		}

		// Token: 0x060018CC RID: 6348 RVA: 0x0004F800 File Offset: 0x0004DA00
		[NullableContext(2)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Memory<T> AsMemory<T>([Nullable(new byte[] { 2, 1 })] this T[] array, int start)
		{
			return new Memory<T>(array, start);
		}

		// Token: 0x060018CD RID: 6349 RVA: 0x0004F809 File Offset: 0x0004DA09
		[NullableContext(2)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Memory<T> AsMemory<T>([Nullable(new byte[] { 2, 1 })] this T[] array, int start, int length)
		{
			return new Memory<T>(array, start, length);
		}

		// Token: 0x060018CE RID: 6350 RVA: 0x0004F813 File Offset: 0x0004DA13
		[NullableContext(2)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Memory<T> AsMemory<T>([Nullable(new byte[] { 0, 1 })] this ArraySegment<T> segment)
		{
			return new Memory<T>(segment.Array, segment.Offset, segment.Count);
		}

		// Token: 0x060018CF RID: 6351 RVA: 0x0004F82F File Offset: 0x0004DA2F
		[NullableContext(2)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Memory<T> AsMemory<T>([Nullable(new byte[] { 0, 1 })] this ArraySegment<T> segment, int start)
		{
			if ((ulong)start > (ulong)((long)segment.Count))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new Memory<T>(segment.Array, segment.Offset + start, segment.Count - start);
		}

		// Token: 0x060018D0 RID: 6352 RVA: 0x0004F861 File Offset: 0x0004DA61
		[NullableContext(2)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Memory<T> AsMemory<T>([Nullable(new byte[] { 0, 1 })] this ArraySegment<T> segment, int start, int length)
		{
			if ((ulong)start > (ulong)((long)segment.Count))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			if ((ulong)length > (ulong)((long)(segment.Count - start)))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
			}
			return new Memory<T>(segment.Array, segment.Offset + start, length);
		}

		// Token: 0x060018D1 RID: 6353 RVA: 0x0004F8A0 File Offset: 0x0004DAA0
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CopyTo<T>([Nullable(new byte[] { 2, 1 })] this T[] source, [Nullable(new byte[] { 0, 1 })] Span<T> destination)
		{
			new ReadOnlySpan<T>(source).CopyTo(destination);
		}

		// Token: 0x060018D2 RID: 6354 RVA: 0x0004F8BC File Offset: 0x0004DABC
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CopyTo<T>([Nullable(new byte[] { 2, 1 })] this T[] source, [Nullable(new byte[] { 0, 1 })] Memory<T> destination)
		{
			source.CopyTo(destination.Span);
		}

		// Token: 0x060018D3 RID: 6355 RVA: 0x0004F8CB File Offset: 0x0004DACB
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Overlaps<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> other)
		{
			return span.Overlaps(other);
		}

		// Token: 0x060018D4 RID: 6356 RVA: 0x0004F8D9 File Offset: 0x0004DAD9
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Overlaps<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> other, out int elementOffset)
		{
			return span.Overlaps(other, out elementOffset);
		}

		// Token: 0x060018D5 RID: 6357 RVA: 0x0004F8E8 File Offset: 0x0004DAE8
		[NullableContext(2)]
		public static bool Overlaps<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> other)
		{
			if (span.IsEmpty || other.IsEmpty)
			{
				return false;
			}
			IntPtr byteOffset = Unsafe.ByteOffset<T>(MemoryMarshal.GetReference<T>(span), MemoryMarshal.GetReference<T>(other));
			if (Unsafe.SizeOf<IntPtr>() == 4)
			{
				return (int)byteOffset < span.Length * Unsafe.SizeOf<T>() || (int)byteOffset > -(other.Length * Unsafe.SizeOf<T>());
			}
			return (long)byteOffset < (long)span.Length * (long)Unsafe.SizeOf<T>() || (long)byteOffset > -((long)other.Length * (long)Unsafe.SizeOf<T>());
		}

		// Token: 0x060018D6 RID: 6358 RVA: 0x0004F984 File Offset: 0x0004DB84
		[NullableContext(2)]
		public static bool Overlaps<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> other, out int elementOffset)
		{
			if (span.IsEmpty || other.IsEmpty)
			{
				elementOffset = 0;
				return false;
			}
			IntPtr byteOffset = Unsafe.ByteOffset<T>(MemoryMarshal.GetReference<T>(span), MemoryMarshal.GetReference<T>(other));
			if (Unsafe.SizeOf<IntPtr>() == 4)
			{
				if ((int)byteOffset < span.Length * Unsafe.SizeOf<T>() || (int)byteOffset > -(other.Length * Unsafe.SizeOf<T>()))
				{
					if ((int)byteOffset % Unsafe.SizeOf<T>() != 0)
					{
						ThrowHelper.ThrowArgumentException_OverlapAlignmentMismatch();
					}
					elementOffset = (int)byteOffset / Unsafe.SizeOf<T>();
					return true;
				}
				elementOffset = 0;
				return false;
			}
			else
			{
				if ((long)byteOffset < (long)span.Length * (long)Unsafe.SizeOf<T>() || (long)byteOffset > -((long)other.Length * (long)Unsafe.SizeOf<T>()))
				{
					if ((long)byteOffset % (long)Unsafe.SizeOf<T>() != 0L)
					{
						ThrowHelper.ThrowArgumentException_OverlapAlignmentMismatch();
					}
					elementOffset = (int)((long)byteOffset / (long)Unsafe.SizeOf<T>());
					return true;
				}
				elementOffset = 0;
				return false;
			}
		}

		// Token: 0x060018D7 RID: 6359 RVA: 0x0004FA6E File Offset: 0x0004DC6E
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, IComparable<T> comparable)
		{
			return span.BinarySearch(comparable);
		}

		// Token: 0x060018D8 RID: 6360 RVA: 0x0004FA77 File Offset: 0x0004DC77
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T, [Nullable(0)] TComparable>([Nullable(new byte[] { 0, 1 })] this Span<T> span, TComparable comparable) where TComparable : IComparable<T>
		{
			return span.BinarySearch(comparable);
		}

		// Token: 0x060018D9 RID: 6361 RVA: 0x0004FA85 File Offset: 0x0004DC85
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T, [Nullable(0)] TComparer>([Nullable(new byte[] { 0, 1 })] this Span<T> span, T value, TComparer comparer) where TComparer : IComparer<T>
		{
			return span.BinarySearch(value, comparer);
		}

		// Token: 0x060018DA RID: 6362 RVA: 0x0004FA94 File Offset: 0x0004DC94
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, IComparable<T> comparable)
		{
			return span.BinarySearch(comparable);
		}

		// Token: 0x060018DB RID: 6363 RVA: 0x0004FA9D File Offset: 0x0004DC9D
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T, [Nullable(0)] TComparable>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, TComparable comparable) where TComparable : IComparable<T>
		{
			return span.BinarySearch(comparable);
		}

		// Token: 0x060018DC RID: 6364 RVA: 0x0004FAA8 File Offset: 0x0004DCA8
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T, [Nullable(0)] TComparer>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, T value, TComparer comparer) where TComparer : IComparer<T>
		{
			if (comparer == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparer);
			}
			SpanHelpers.ComparerComparable<T, TComparer> comparable = new SpanHelpers.ComparerComparable<T, TComparer>(value, comparer);
			return span.BinarySearch(comparable);
		}

		// Token: 0x060018DD RID: 6365 RVA: 0x0004FAD4 File Offset: 0x0004DCD4
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsTypeComparableAsBytes<T>([NativeInteger] out UIntPtr size)
		{
			if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
			{
				size = (UIntPtr)((IntPtr)1);
				return true;
			}
			if (typeof(T) == typeof(char) || typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
			{
				size = (UIntPtr)((IntPtr)2);
				return true;
			}
			if (typeof(T) == typeof(int) || typeof(T) == typeof(uint))
			{
				size = (UIntPtr)((IntPtr)4);
				return true;
			}
			if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong))
			{
				size = (UIntPtr)((IntPtr)8);
				return true;
			}
			size = (UIntPtr)((IntPtr)0);
			return false;
		}

		// Token: 0x060018DE RID: 6366 RVA: 0x0004FBF1 File Offset: 0x0004DDF1
		[NullableContext(2)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Span<T> AsSpan<T>([Nullable(new byte[] { 2, 1 })] this T[] array, int start)
		{
			return Span<T>.Create(array, start);
		}

		// Token: 0x060018DF RID: 6367 RVA: 0x0004FBFA File Offset: 0x0004DDFA
		public static bool Contains(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
		{
			return span.IndexOf(value, comparisonType) >= 0;
		}

		// Token: 0x060018E0 RID: 6368 RVA: 0x0004FC0C File Offset: 0x0004DE0C
		public static bool Equals(this ReadOnlySpan<char> span, ReadOnlySpan<char> other, StringComparison comparisonType)
		{
			if (comparisonType == StringComparison.Ordinal)
			{
				return span.SequenceEqual(other);
			}
			if (comparisonType == StringComparison.OrdinalIgnoreCase)
			{
				return span.Length == other.Length && MemoryExtensions.EqualsOrdinalIgnoreCase(span, other);
			}
			return span.ToString().Equals(other.ToString(), comparisonType);
		}

		// Token: 0x060018E1 RID: 6369 RVA: 0x0004FC63 File Offset: 0x0004DE63
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool EqualsOrdinalIgnoreCase(ReadOnlySpan<char> span, ReadOnlySpan<char> other)
		{
			return other.Length == 0 || MemoryExtensions.CompareToOrdinalIgnoreCase(span, other) == 0;
		}

		// Token: 0x060018E2 RID: 6370 RVA: 0x0004FC7A File Offset: 0x0004DE7A
		public static int CompareTo(this ReadOnlySpan<char> span, ReadOnlySpan<char> other, StringComparison comparisonType)
		{
			if (comparisonType == StringComparison.Ordinal)
			{
				return span.SequenceCompareTo(other);
			}
			if (comparisonType == StringComparison.OrdinalIgnoreCase)
			{
				return MemoryExtensions.CompareToOrdinalIgnoreCase(span, other);
			}
			return string.Compare(span.ToString(), other.ToString(), comparisonType);
		}

		// Token: 0x060018E3 RID: 6371 RVA: 0x0004FCB4 File Offset: 0x0004DEB4
		private unsafe static int CompareToOrdinalIgnoreCase(ReadOnlySpan<char> strA, ReadOnlySpan<char> strB)
		{
			int length = Math.Min(strA.Length, strB.Length);
			int range = length;
			fixed (char* reference = MemoryMarshal.GetReference<char>(strA))
			{
				char* ap = reference;
				fixed (char* reference2 = MemoryMarshal.GetReference<char>(strB))
				{
					char* ptr = reference2;
					char* a = ap;
					char* b = ptr;
					while (length != 0 && *a <= '\u007f' && *b <= '\u007f')
					{
						int charA = (int)(*a);
						int charB = (int)(*b);
						if (charA == charB)
						{
							a++;
							b++;
							length--;
						}
						else
						{
							if (charA - 97 <= 25)
							{
								charA -= 32;
							}
							if (charB - 97 <= 25)
							{
								charB -= 32;
							}
							if (charA != charB)
							{
								return charA - charB;
							}
							a++;
							b++;
							length--;
						}
					}
					if (length == 0)
					{
						return strA.Length - strB.Length;
					}
					range -= length;
					return string.Compare(strA.Slice(range).ToString(), strB.Slice(range).ToString(), StringComparison.OrdinalIgnoreCase);
				}
			}
		}

		// Token: 0x060018E4 RID: 6372 RVA: 0x0004FDAD File Offset: 0x0004DFAD
		public static int IndexOf(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
		{
			if (comparisonType == StringComparison.Ordinal)
			{
				return span.IndexOf(value);
			}
			return span.ToString().IndexOf(value.ToString(), comparisonType);
		}

		// Token: 0x060018E5 RID: 6373 RVA: 0x0004FDDC File Offset: 0x0004DFDC
		public static int ToLower(this ReadOnlySpan<char> source, Span<char> destination, [Nullable(1)] CultureInfo culture)
		{
			ThrowHelper.ThrowIfArgumentNull(culture, ExceptionArgument.culture);
			if (destination.Length < source.Length)
			{
				return -1;
			}
			string sourceString = source.ToString();
			culture.TextInfo.ToLower(sourceString).AsSpan().CopyTo(destination);
			return source.Length;
		}

		// Token: 0x060018E6 RID: 6374 RVA: 0x0004FE32 File Offset: 0x0004E032
		public static int ToLowerInvariant(this ReadOnlySpan<char> source, Span<char> destination)
		{
			return source.ToLower(destination, CultureInfo.InvariantCulture);
		}

		// Token: 0x060018E7 RID: 6375 RVA: 0x0004FE40 File Offset: 0x0004E040
		public static int ToUpper(this ReadOnlySpan<char> source, Span<char> destination, [Nullable(1)] CultureInfo culture)
		{
			ThrowHelper.ThrowIfArgumentNull(culture, ExceptionArgument.culture);
			if (destination.Length < source.Length)
			{
				return -1;
			}
			string sourceString = source.ToString();
			culture.TextInfo.ToUpper(sourceString).AsSpan().CopyTo(destination);
			return source.Length;
		}

		// Token: 0x060018E8 RID: 6376 RVA: 0x0004FE96 File Offset: 0x0004E096
		public static int ToUpperInvariant(this ReadOnlySpan<char> source, Span<char> destination)
		{
			return source.ToUpper(destination, CultureInfo.InvariantCulture);
		}

		// Token: 0x060018E9 RID: 6377 RVA: 0x0004FEA4 File Offset: 0x0004E0A4
		public static bool EndsWith(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
		{
			if (comparisonType == StringComparison.Ordinal)
			{
				return span.EndsWith(value);
			}
			if (comparisonType == StringComparison.OrdinalIgnoreCase)
			{
				return value.Length <= span.Length && MemoryExtensions.EqualsOrdinalIgnoreCase(span.Slice(span.Length - value.Length), value);
			}
			string text = span.ToString();
			string valueString = value.ToString();
			return text.EndsWith(valueString, comparisonType);
		}

		// Token: 0x060018EA RID: 6378 RVA: 0x0004FF14 File Offset: 0x0004E114
		public static bool StartsWith(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
		{
			if (comparisonType == StringComparison.Ordinal)
			{
				return span.StartsWith(value);
			}
			if (comparisonType == StringComparison.OrdinalIgnoreCase)
			{
				return value.Length <= span.Length && MemoryExtensions.EqualsOrdinalIgnoreCase(span.Slice(0, value.Length), value);
			}
			string text = span.ToString();
			string valueString = value.ToString();
			return text.StartsWith(valueString, comparisonType);
		}

		// Token: 0x060018EB RID: 6379 RVA: 0x0004FF7C File Offset: 0x0004E17C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<char> AsSpan([Nullable(2)] this string text)
		{
			if (text == null)
			{
				return default(ReadOnlySpan<char>);
			}
			return new ReadOnlySpan<char>(text, (IntPtr)RuntimeHelpers.OffsetToStringData, text.Length);
		}

		// Token: 0x060018EC RID: 6380 RVA: 0x0004FFA8 File Offset: 0x0004E1A8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<char> AsSpan([Nullable(2)] this string text, int start)
		{
			if (text == null)
			{
				if (start != 0)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
				}
				return default(ReadOnlySpan<char>);
			}
			if (start > text.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new ReadOnlySpan<char>(text, (IntPtr)RuntimeHelpers.OffsetToStringData + (IntPtr)(start * 2), text.Length - start);
		}

		// Token: 0x060018ED RID: 6381 RVA: 0x0004FFF4 File Offset: 0x0004E1F4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<char> AsSpan([Nullable(2)] this string text, int start, int length)
		{
			if (text == null)
			{
				if (start != 0 || length != 0)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
				}
				return default(ReadOnlySpan<char>);
			}
			if (start > text.Length || length > text.Length - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new ReadOnlySpan<char>(text, (IntPtr)RuntimeHelpers.OffsetToStringData + (IntPtr)(start * 2), length);
		}

		// Token: 0x060018EE RID: 6382 RVA: 0x00050048 File Offset: 0x0004E248
		public static ReadOnlyMemory<char> AsMemory([Nullable(2)] this string text)
		{
			if (text == null)
			{
				return default(ReadOnlyMemory<char>);
			}
			return new ReadOnlyMemory<char>(text, 0, text.Length);
		}

		// Token: 0x060018EF RID: 6383 RVA: 0x00050070 File Offset: 0x0004E270
		public static ReadOnlyMemory<char> AsMemory([Nullable(2)] this string text, int start)
		{
			if (text == null)
			{
				if (start != 0)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
				}
				return default(ReadOnlyMemory<char>);
			}
			if (start > text.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new ReadOnlyMemory<char>(text, start, text.Length - start);
		}

		// Token: 0x060018F0 RID: 6384 RVA: 0x000500B4 File Offset: 0x0004E2B4
		public static ReadOnlyMemory<char> AsMemory([Nullable(2)] this string text, int start, int length)
		{
			if (text == null)
			{
				if (start != 0 || length != 0)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
				}
				return default(ReadOnlyMemory<char>);
			}
			if (start > text.Length || length > text.Length - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new ReadOnlyMemory<char>(text, start, length);
		}
	}
}
