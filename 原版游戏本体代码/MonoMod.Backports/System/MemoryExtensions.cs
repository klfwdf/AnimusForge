using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x02000015 RID: 21
	public static class MemoryExtensions
	{
		// Token: 0x06000058 RID: 88 RVA: 0x000037B0 File Offset: 0x000019B0
		public static ReadOnlySpan<char> Trim(this ReadOnlySpan<char> span)
		{
			return span.TrimStart().TrimEnd();
		}

		// Token: 0x06000059 RID: 89 RVA: 0x000037C0 File Offset: 0x000019C0
		public unsafe static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> span)
		{
			int num = 0;
			while (num < span.Length && char.IsWhiteSpace((char)(*span[num])))
			{
				num++;
			}
			return span.Slice(num);
		}

		// Token: 0x0600005A RID: 90 RVA: 0x000037F8 File Offset: 0x000019F8
		public unsafe static ReadOnlySpan<char> TrimEnd(this ReadOnlySpan<char> span)
		{
			int num = span.Length - 1;
			while (num >= 0 && char.IsWhiteSpace((char)(*span[num])))
			{
				num--;
			}
			return span.Slice(0, num + 1);
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00003834 File Offset: 0x00001A34
		public static ReadOnlySpan<char> Trim(this ReadOnlySpan<char> span, char trimChar)
		{
			return span.TrimStart(trimChar).TrimEnd(trimChar);
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00003844 File Offset: 0x00001A44
		public unsafe static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> span, char trimChar)
		{
			int num = 0;
			while (num < span.Length && *span[num] == (ushort)trimChar)
			{
				num++;
			}
			return span.Slice(num);
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00003878 File Offset: 0x00001A78
		public unsafe static ReadOnlySpan<char> TrimEnd(this ReadOnlySpan<char> span, char trimChar)
		{
			int num = span.Length - 1;
			while (num >= 0 && *span[num] == (ushort)trimChar)
			{
				num--;
			}
			return span.Slice(0, num + 1);
		}

		// Token: 0x0600005E RID: 94 RVA: 0x000038B0 File Offset: 0x00001AB0
		public static ReadOnlySpan<char> Trim(this ReadOnlySpan<char> span, ReadOnlySpan<char> trimChars)
		{
			return span.TrimStart(trimChars).TrimEnd(trimChars);
		}

		// Token: 0x0600005F RID: 95 RVA: 0x000038C0 File Offset: 0x00001AC0
		public unsafe static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> span, ReadOnlySpan<char> trimChars)
		{
			if (trimChars.IsEmpty)
			{
				return span.TrimStart();
			}
			int i = 0;
			IL_40:
			while (i < span.Length)
			{
				for (int j = 0; j < trimChars.Length; j++)
				{
					if (*span[i] == *trimChars[j])
					{
						i++;
						goto IL_40;
					}
				}
				break;
			}
			return span.Slice(i);
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00003920 File Offset: 0x00001B20
		public unsafe static ReadOnlySpan<char> TrimEnd(this ReadOnlySpan<char> span, ReadOnlySpan<char> trimChars)
		{
			if (trimChars.IsEmpty)
			{
				return span.TrimEnd();
			}
			int i = span.Length - 1;
			IL_48:
			while (i >= 0)
			{
				for (int j = 0; j < trimChars.Length; j++)
				{
					if (*span[i] == *trimChars[j])
					{
						i--;
						goto IL_48;
					}
				}
				break;
			}
			return span.Slice(0, i + 1);
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00003984 File Offset: 0x00001B84
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

		// Token: 0x06000062 RID: 98 RVA: 0x000039B8 File Offset: 0x00001BB8
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

		// Token: 0x06000063 RID: 99 RVA: 0x00003A50 File Offset: 0x00001C50
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOf<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOf(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(value)), value.Length);
			}
			return SpanHelpers.IndexOf<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(value), value.Length);
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00003AC4 File Offset: 0x00001CC4
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

		// Token: 0x06000065 RID: 101 RVA: 0x00003B5C File Offset: 0x00001D5C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LastIndexOf<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOf(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(value)), value.Length);
			}
			return SpanHelpers.LastIndexOf<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(value), value.Length);
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00003BD0 File Offset: 0x00001DD0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool SequenceEqual<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> other) where T : IEquatable<T>
		{
			int length = span.Length;
			UIntPtr uintPtr;
			if (default(T) != null && MemoryExtensions.IsTypeComparableAsBytes<T>(out uintPtr))
			{
				return length == other.Length && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(other)), (UIntPtr)((IntPtr)length * (IntPtr)uintPtr));
			}
			return length == other.Length && SpanHelpers.SequenceEqual<T>(MemoryMarshal.GetReference<T>(span), MemoryMarshal.GetReference<T>(other), length);
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00003C48 File Offset: 0x00001E48
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

		// Token: 0x06000068 RID: 104 RVA: 0x00003D00 File Offset: 0x00001F00
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

		// Token: 0x06000069 RID: 105 RVA: 0x00003D98 File Offset: 0x00001F98
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOf<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOf(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(value)), value.Length);
			}
			return SpanHelpers.IndexOf<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(value), value.Length);
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00003E0C File Offset: 0x0000200C
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

		// Token: 0x0600006B RID: 107 RVA: 0x00003EA4 File Offset: 0x000020A4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LastIndexOf<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOf(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(value)), value.Length);
			}
			return SpanHelpers.LastIndexOf<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(value), value.Length);
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00003F18 File Offset: 0x00002118
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

		// Token: 0x0600006D RID: 109 RVA: 0x00003F7C File Offset: 0x0000217C
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

		// Token: 0x0600006E RID: 110 RVA: 0x00003FEC File Offset: 0x000021EC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOfAny<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> values) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOfAny(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(values)), values.Length);
			}
			return SpanHelpers.IndexOfAny<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(values), values.Length);
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00004060 File Offset: 0x00002260
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

		// Token: 0x06000070 RID: 112 RVA: 0x000040C4 File Offset: 0x000022C4
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

		// Token: 0x06000071 RID: 113 RVA: 0x00004134 File Offset: 0x00002334
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOfAny<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> values) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOfAny(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(values)), values.Length);
			}
			return SpanHelpers.IndexOfAny<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(values), values.Length);
		}

		// Token: 0x06000072 RID: 114 RVA: 0x000041A8 File Offset: 0x000023A8
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

		// Token: 0x06000073 RID: 115 RVA: 0x0000420C File Offset: 0x0000240C
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

		// Token: 0x06000074 RID: 116 RVA: 0x0000427C File Offset: 0x0000247C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LastIndexOfAny<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> values) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOfAny(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(values)), values.Length);
			}
			return SpanHelpers.LastIndexOfAny<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(values), values.Length);
		}

		// Token: 0x06000075 RID: 117 RVA: 0x000042F0 File Offset: 0x000024F0
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

		// Token: 0x06000076 RID: 118 RVA: 0x00004354 File Offset: 0x00002554
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

		// Token: 0x06000077 RID: 119 RVA: 0x000043C4 File Offset: 0x000025C4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LastIndexOfAny<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> values) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOfAny(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(values)), values.Length);
			}
			return SpanHelpers.LastIndexOfAny<T>(MemoryMarshal.GetReference<T>(span), span.Length, MemoryMarshal.GetReference<T>(values), values.Length);
		}

		// Token: 0x06000078 RID: 120 RVA: 0x00004438 File Offset: 0x00002638
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool SequenceEqual<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> other) where T : IEquatable<T>
		{
			int length = span.Length;
			UIntPtr uintPtr;
			if (default(T) != null && MemoryExtensions.IsTypeComparableAsBytes<T>(out uintPtr))
			{
				return length == other.Length && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(other)), (UIntPtr)((IntPtr)length * (IntPtr)uintPtr));
			}
			return length == other.Length && SpanHelpers.SequenceEqual<T>(MemoryMarshal.GetReference<T>(span), MemoryMarshal.GetReference<T>(other), length);
		}

		// Token: 0x06000079 RID: 121 RVA: 0x000044B0 File Offset: 0x000026B0
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

		// Token: 0x0600007A RID: 122 RVA: 0x00004568 File Offset: 0x00002768
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool StartsWith<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			int length = value.Length;
			UIntPtr uintPtr;
			if (default(T) != null && MemoryExtensions.IsTypeComparableAsBytes<T>(out uintPtr))
			{
				return length <= span.Length && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(value)), (UIntPtr)((IntPtr)length * (IntPtr)uintPtr));
			}
			return length <= span.Length && SpanHelpers.SequenceEqual<T>(MemoryMarshal.GetReference<T>(span), MemoryMarshal.GetReference<T>(value), length);
		}

		// Token: 0x0600007B RID: 123 RVA: 0x000045E0 File Offset: 0x000027E0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool StartsWith<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			int length = value.Length;
			UIntPtr uintPtr;
			if (default(T) != null && MemoryExtensions.IsTypeComparableAsBytes<T>(out uintPtr))
			{
				return length <= span.Length && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(span)), Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(value)), (UIntPtr)((IntPtr)length * (IntPtr)uintPtr));
			}
			return length <= span.Length && SpanHelpers.SequenceEqual<T>(MemoryMarshal.GetReference<T>(span), MemoryMarshal.GetReference<T>(value), length);
		}

		// Token: 0x0600007C RID: 124 RVA: 0x00004658 File Offset: 0x00002858
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool EndsWith<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			int length = span.Length;
			int length2 = value.Length;
			UIntPtr uintPtr;
			if (default(T) != null && MemoryExtensions.IsTypeComparableAsBytes<T>(out uintPtr))
			{
				return length2 <= length && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(Unsafe.Add<T>(MemoryMarshal.GetReference<T>(span), length - length2)), Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(value)), (UIntPtr)((IntPtr)length2 * (IntPtr)uintPtr));
			}
			return length2 <= length && SpanHelpers.SequenceEqual<T>(Unsafe.Add<T>(MemoryMarshal.GetReference<T>(span), length - length2), MemoryMarshal.GetReference<T>(value), length2);
		}

		// Token: 0x0600007D RID: 125 RVA: 0x000046DC File Offset: 0x000028DC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool EndsWith<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			int length = span.Length;
			int length2 = value.Length;
			UIntPtr uintPtr;
			if (default(T) != null && MemoryExtensions.IsTypeComparableAsBytes<T>(out uintPtr))
			{
				return length2 <= length && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(Unsafe.Add<T>(MemoryMarshal.GetReference<T>(span), length - length2)), Unsafe.As<T, byte>(MemoryMarshal.GetReference<T>(value)), (UIntPtr)((IntPtr)length2 * (IntPtr)uintPtr));
			}
			return length2 <= length && SpanHelpers.SequenceEqual<T>(Unsafe.Add<T>(MemoryMarshal.GetReference<T>(span), length - length2), MemoryMarshal.GetReference<T>(value), length2);
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00004760 File Offset: 0x00002960
		[NullableContext(2)]
		public static void Reverse<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span)
		{
			if (span.Length <= 1)
			{
				return;
			}
			ref T ptr = ref MemoryMarshal.GetReference<T>(span);
			ref T ptr2 = ref Unsafe.Add<T>(Unsafe.Add<T>(ref ptr, span.Length), -1);
			do
			{
				T t = ptr;
				ptr = ptr2;
				ptr2 = t;
				ptr = Unsafe.Add<T>(ref ptr, 1);
				ptr2 = Unsafe.Add<T>(ref ptr2, -1);
			}
			while (Unsafe.IsAddressLessThan<T>(ref ptr, ref ptr2));
		}

		// Token: 0x0600007F RID: 127 RVA: 0x000047C6 File Offset: 0x000029C6
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Span<T> AsSpan<T>([Nullable(new byte[] { 2, 1 })] this T[] array)
		{
			return new Span<T>(array);
		}

		// Token: 0x06000080 RID: 128 RVA: 0x000047CE File Offset: 0x000029CE
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Span<T> AsSpan<T>([Nullable(new byte[] { 2, 1 })] this T[] array, int start, int length)
		{
			return new Span<T>(array, start, length);
		}

		// Token: 0x06000081 RID: 129 RVA: 0x000047D8 File Offset: 0x000029D8
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Span<T> AsSpan<T>([Nullable(new byte[] { 0, 1 })] this ArraySegment<T> segment)
		{
			return new Span<T>(segment.Array, segment.Offset, segment.Count);
		}

		// Token: 0x06000082 RID: 130 RVA: 0x000047F4 File Offset: 0x000029F4
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

		// Token: 0x06000083 RID: 131 RVA: 0x00004826 File Offset: 0x00002A26
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

		// Token: 0x06000084 RID: 132 RVA: 0x00004864 File Offset: 0x00002A64
		[NullableContext(2)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Memory<T> AsMemory<T>([Nullable(new byte[] { 2, 1 })] this T[] array)
		{
			return new Memory<T>(array);
		}

		// Token: 0x06000085 RID: 133 RVA: 0x0000486C File Offset: 0x00002A6C
		[NullableContext(2)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Memory<T> AsMemory<T>([Nullable(new byte[] { 2, 1 })] this T[] array, int start)
		{
			return new Memory<T>(array, start);
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00004875 File Offset: 0x00002A75
		[NullableContext(2)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Memory<T> AsMemory<T>([Nullable(new byte[] { 2, 1 })] this T[] array, int start, int length)
		{
			return new Memory<T>(array, start, length);
		}

		// Token: 0x06000087 RID: 135 RVA: 0x0000487F File Offset: 0x00002A7F
		[NullableContext(2)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Memory<T> AsMemory<T>([Nullable(new byte[] { 0, 1 })] this ArraySegment<T> segment)
		{
			return new Memory<T>(segment.Array, segment.Offset, segment.Count);
		}

		// Token: 0x06000088 RID: 136 RVA: 0x0000489B File Offset: 0x00002A9B
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

		// Token: 0x06000089 RID: 137 RVA: 0x000048CD File Offset: 0x00002ACD
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

		// Token: 0x0600008A RID: 138 RVA: 0x0000490C File Offset: 0x00002B0C
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CopyTo<T>([Nullable(new byte[] { 2, 1 })] this T[] source, [Nullable(new byte[] { 0, 1 })] Span<T> destination)
		{
			new ReadOnlySpan<T>(source).CopyTo(destination);
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00004928 File Offset: 0x00002B28
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CopyTo<T>([Nullable(new byte[] { 2, 1 })] this T[] source, [Nullable(new byte[] { 0, 1 })] Memory<T> destination)
		{
			source.CopyTo(destination.Span);
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00004937 File Offset: 0x00002B37
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Overlaps<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> other)
		{
			return span.Overlaps(other);
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00004945 File Offset: 0x00002B45
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Overlaps<T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> other, out int elementOffset)
		{
			return span.Overlaps(other, out elementOffset);
		}

		// Token: 0x0600008E RID: 142 RVA: 0x00004954 File Offset: 0x00002B54
		[NullableContext(2)]
		public static bool Overlaps<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> other)
		{
			if (span.IsEmpty || other.IsEmpty)
			{
				return false;
			}
			IntPtr value = Unsafe.ByteOffset<T>(MemoryMarshal.GetReference<T>(span), MemoryMarshal.GetReference<T>(other));
			if (Unsafe.SizeOf<IntPtr>() == 4)
			{
				return (int)value < span.Length * Unsafe.SizeOf<T>() || (int)value > -(other.Length * Unsafe.SizeOf<T>());
			}
			return (long)value < (long)span.Length * (long)Unsafe.SizeOf<T>() || (long)value > -((long)other.Length * (long)Unsafe.SizeOf<T>());
		}

		// Token: 0x0600008F RID: 143 RVA: 0x000049F0 File Offset: 0x00002BF0
		[NullableContext(2)]
		public static bool Overlaps<T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> other, out int elementOffset)
		{
			if (span.IsEmpty || other.IsEmpty)
			{
				elementOffset = 0;
				return false;
			}
			IntPtr value = Unsafe.ByteOffset<T>(MemoryMarshal.GetReference<T>(span), MemoryMarshal.GetReference<T>(other));
			if (Unsafe.SizeOf<IntPtr>() == 4)
			{
				if ((int)value < span.Length * Unsafe.SizeOf<T>() || (int)value > -(other.Length * Unsafe.SizeOf<T>()))
				{
					if ((int)value % Unsafe.SizeOf<T>() != 0)
					{
						ThrowHelper.ThrowArgumentException_OverlapAlignmentMismatch();
					}
					elementOffset = (int)value / Unsafe.SizeOf<T>();
					return true;
				}
				elementOffset = 0;
				return false;
			}
			else
			{
				if ((long)value < (long)span.Length * (long)Unsafe.SizeOf<T>() || (long)value > -((long)other.Length * (long)Unsafe.SizeOf<T>()))
				{
					if ((long)value % (long)Unsafe.SizeOf<T>() != 0L)
					{
						ThrowHelper.ThrowArgumentException_OverlapAlignmentMismatch();
					}
					elementOffset = (int)((long)value / (long)Unsafe.SizeOf<T>());
					return true;
				}
				elementOffset = 0;
				return false;
			}
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00004ADA File Offset: 0x00002CDA
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T>([Nullable(new byte[] { 0, 1 })] this Span<T> span, IComparable<T> comparable)
		{
			return span.BinarySearch(comparable);
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00004AE3 File Offset: 0x00002CE3
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T, [Nullable(0)] TComparable>([Nullable(new byte[] { 0, 1 })] this Span<T> span, TComparable comparable) where TComparable : IComparable<T>
		{
			return span.BinarySearch(comparable);
		}

		// Token: 0x06000092 RID: 146 RVA: 0x00004AF1 File Offset: 0x00002CF1
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T, [Nullable(0)] TComparer>([Nullable(new byte[] { 0, 1 })] this Span<T> span, T value, TComparer comparer) where TComparer : IComparer<T>
		{
			return span.BinarySearch(value, comparer);
		}

		// Token: 0x06000093 RID: 147 RVA: 0x00004B00 File Offset: 0x00002D00
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, IComparable<T> comparable)
		{
			return span.BinarySearch(comparable);
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00004B09 File Offset: 0x00002D09
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T, [Nullable(0)] TComparable>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, TComparable comparable) where TComparable : IComparable<T>
		{
			return span.BinarySearch(comparable);
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00004B14 File Offset: 0x00002D14
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

		// Token: 0x06000096 RID: 150 RVA: 0x00004B40 File Offset: 0x00002D40
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

		// Token: 0x06000097 RID: 151 RVA: 0x00004C5D File Offset: 0x00002E5D
		[NullableContext(2)]
		[return: Nullable(new byte[] { 0, 1 })]
		public static Span<T> AsSpan<T>([Nullable(new byte[] { 2, 1 })] this T[] array, int start)
		{
			return Span<T>.Create(array, start);
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00004C66 File Offset: 0x00002E66
		public static bool Contains(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
		{
			return span.IndexOf(value, comparisonType) >= 0;
		}

		// Token: 0x06000099 RID: 153 RVA: 0x00004C78 File Offset: 0x00002E78
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

		// Token: 0x0600009A RID: 154 RVA: 0x00004CCF File Offset: 0x00002ECF
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool EqualsOrdinalIgnoreCase(ReadOnlySpan<char> span, ReadOnlySpan<char> other)
		{
			return other.Length == 0 || MemoryExtensions.CompareToOrdinalIgnoreCase(span, other) == 0;
		}

		// Token: 0x0600009B RID: 155 RVA: 0x00004CE6 File Offset: 0x00002EE6
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

		// Token: 0x0600009C RID: 156 RVA: 0x00004D20 File Offset: 0x00002F20
		private unsafe static int CompareToOrdinalIgnoreCase(ReadOnlySpan<char> strA, ReadOnlySpan<char> strB)
		{
			int num = Math.Min(strA.Length, strB.Length);
			int num2 = num;
			fixed (char* reference = MemoryMarshal.GetReference<char>(strA))
			{
				char* ptr = reference;
				fixed (char* reference2 = MemoryMarshal.GetReference<char>(strB))
				{
					char* ptr2 = reference2;
					char* ptr3 = ptr;
					char* ptr4 = ptr2;
					while (num != 0 && *ptr3 <= '\u007f' && *ptr4 <= '\u007f')
					{
						int num3 = (int)(*ptr3);
						int num4 = (int)(*ptr4);
						if (num3 == num4)
						{
							ptr3++;
							ptr4++;
							num--;
						}
						else
						{
							if (num3 - 97 <= 25)
							{
								num3 -= 32;
							}
							if (num4 - 97 <= 25)
							{
								num4 -= 32;
							}
							if (num3 != num4)
							{
								return num3 - num4;
							}
							ptr3++;
							ptr4++;
							num--;
						}
					}
					if (num == 0)
					{
						return strA.Length - strB.Length;
					}
					num2 -= num;
					return string.Compare(strA.Slice(num2).ToString(), strB.Slice(num2).ToString(), StringComparison.OrdinalIgnoreCase);
				}
			}
		}

		// Token: 0x0600009D RID: 157 RVA: 0x00004E19 File Offset: 0x00003019
		public static int IndexOf(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
		{
			if (comparisonType == StringComparison.Ordinal)
			{
				return span.IndexOf(value);
			}
			return span.ToString().IndexOf(value.ToString(), comparisonType);
		}

		// Token: 0x0600009E RID: 158 RVA: 0x00004E48 File Offset: 0x00003048
		public static int ToLower(this ReadOnlySpan<char> source, Span<char> destination, [Nullable(1)] CultureInfo culture)
		{
			ThrowHelper.ThrowIfArgumentNull(culture, ExceptionArgument.culture);
			if (destination.Length < source.Length)
			{
				return -1;
			}
			string str = source.ToString();
			culture.TextInfo.ToLower(str).AsSpan().CopyTo(destination);
			return source.Length;
		}

		// Token: 0x0600009F RID: 159 RVA: 0x00004E9E File Offset: 0x0000309E
		public static int ToLowerInvariant(this ReadOnlySpan<char> source, Span<char> destination)
		{
			return source.ToLower(destination, CultureInfo.InvariantCulture);
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x00004EAC File Offset: 0x000030AC
		public static int ToUpper(this ReadOnlySpan<char> source, Span<char> destination, [Nullable(1)] CultureInfo culture)
		{
			ThrowHelper.ThrowIfArgumentNull(culture, ExceptionArgument.culture);
			if (destination.Length < source.Length)
			{
				return -1;
			}
			string str = source.ToString();
			culture.TextInfo.ToUpper(str).AsSpan().CopyTo(destination);
			return source.Length;
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x00004F02 File Offset: 0x00003102
		public static int ToUpperInvariant(this ReadOnlySpan<char> source, Span<char> destination)
		{
			return source.ToUpper(destination, CultureInfo.InvariantCulture);
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00004F10 File Offset: 0x00003110
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
			string value2 = value.ToString();
			return text.EndsWith(value2, comparisonType);
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x00004F80 File Offset: 0x00003180
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
			string value2 = value.ToString();
			return text.StartsWith(value2, comparisonType);
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x00004FE8 File Offset: 0x000031E8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<char> AsSpan([Nullable(2)] this string text)
		{
			if (text == null)
			{
				return default(ReadOnlySpan<char>);
			}
			return new ReadOnlySpan<char>(text, (IntPtr)RuntimeHelpers.OffsetToStringData, text.Length);
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00005014 File Offset: 0x00003214
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

		// Token: 0x060000A6 RID: 166 RVA: 0x00005060 File Offset: 0x00003260
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

		// Token: 0x060000A7 RID: 167 RVA: 0x000050B4 File Offset: 0x000032B4
		public static ReadOnlyMemory<char> AsMemory([Nullable(2)] this string text)
		{
			if (text == null)
			{
				return default(ReadOnlyMemory<char>);
			}
			return new ReadOnlyMemory<char>(text, 0, text.Length);
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x000050DC File Offset: 0x000032DC
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

		// Token: 0x060000A9 RID: 169 RVA: 0x00005120 File Offset: 0x00003320
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
