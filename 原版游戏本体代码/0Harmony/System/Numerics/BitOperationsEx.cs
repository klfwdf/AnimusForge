using System;
using System.Runtime.CompilerServices;

namespace System.Numerics
{
	// Token: 0x02000486 RID: 1158
	internal static class BitOperationsEx
	{
		// Token: 0x060019C9 RID: 6601 RVA: 0x00054939 File Offset: 0x00052B39
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPow2(int value)
		{
			return (value & (value - 1)) == 0 && value > 0;
		}

		// Token: 0x060019CA RID: 6602 RVA: 0x00054948 File Offset: 0x00052B48
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPow2(uint value)
		{
			return (value & (value - 1U)) == 0U && value > 0U;
		}

		// Token: 0x060019CB RID: 6603 RVA: 0x00054957 File Offset: 0x00052B57
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPow2(long value)
		{
			return (value & (value - 1L)) == 0L && value > 0L;
		}

		// Token: 0x060019CC RID: 6604 RVA: 0x00054968 File Offset: 0x00052B68
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPow2(ulong value)
		{
			return (value & (value - 1UL)) == 0UL && value > 0UL;
		}

		// Token: 0x060019CD RID: 6605 RVA: 0x00054979 File Offset: 0x00052B79
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPow2([NativeInteger] IntPtr value)
		{
			return (value & (value - (IntPtr)1)) == 0 && value > (IntPtr)0;
		}

		// Token: 0x060019CE RID: 6606 RVA: 0x0005498A File Offset: 0x00052B8A
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPow2([NativeInteger] UIntPtr value)
		{
			return (value & (value - (UIntPtr)((IntPtr)1))) == 0 && value > (UIntPtr)((IntPtr)0);
		}

		// Token: 0x060019CF RID: 6607 RVA: 0x0005499B File Offset: 0x00052B9B
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint RoundUpToPowerOf2(uint value)
		{
			value -= 1U;
			value |= value >> 1;
			value |= value >> 2;
			value |= value >> 4;
			value |= value >> 8;
			value |= value >> 16;
			return value + 1U;
		}

		// Token: 0x060019D0 RID: 6608 RVA: 0x000549C9 File Offset: 0x00052BC9
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong RoundUpToPowerOf2(ulong value)
		{
			value -= 1UL;
			value |= value >> 1;
			value |= value >> 2;
			value |= value >> 4;
			value |= value >> 8;
			value |= value >> 16;
			value |= value >> 32;
			return value + 1UL;
		}

		// Token: 0x060019D1 RID: 6609 RVA: 0x00054A01 File Offset: 0x00052C01
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: NativeInteger]
		public static UIntPtr RoundUpToPowerOf2([NativeInteger] UIntPtr value)
		{
			if (IntPtr.Size == 8)
			{
				return (UIntPtr)BitOperationsEx.RoundUpToPowerOf2((ulong)value);
			}
			return (UIntPtr)BitOperationsEx.RoundUpToPowerOf2((uint)value);
		}

		// Token: 0x060019D2 RID: 6610 RVA: 0x00054A1C File Offset: 0x00052C1C
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LeadingZeroCount(uint value)
		{
			return BitOperations.LeadingZeroCount(value);
		}

		// Token: 0x060019D3 RID: 6611 RVA: 0x00054A24 File Offset: 0x00052C24
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LeadingZeroCount(ulong value)
		{
			return BitOperations.LeadingZeroCount(value);
		}

		// Token: 0x060019D4 RID: 6612 RVA: 0x00054A2C File Offset: 0x00052C2C
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LeadingZeroCount([NativeInteger] UIntPtr value)
		{
			if (IntPtr.Size == 8)
			{
				return BitOperationsEx.LeadingZeroCount((ulong)value);
			}
			return BitOperationsEx.LeadingZeroCount((uint)value);
		}

		// Token: 0x060019D5 RID: 6613 RVA: 0x00054A45 File Offset: 0x00052C45
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Log2(uint value)
		{
			return BitOperations.Log2(value);
		}

		// Token: 0x060019D6 RID: 6614 RVA: 0x00054A4D File Offset: 0x00052C4D
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Log2(ulong value)
		{
			return BitOperations.Log2(value);
		}

		// Token: 0x060019D7 RID: 6615 RVA: 0x00054A55 File Offset: 0x00052C55
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Log2([NativeInteger] UIntPtr value)
		{
			if (IntPtr.Size == 8)
			{
				return BitOperationsEx.Log2((ulong)value);
			}
			return BitOperationsEx.Log2((uint)value);
		}

		// Token: 0x060019D8 RID: 6616 RVA: 0x00054A6E File Offset: 0x00052C6E
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int PopCount(uint value)
		{
			return BitOperations.PopCount(value);
		}

		// Token: 0x060019D9 RID: 6617 RVA: 0x00054A76 File Offset: 0x00052C76
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int PopCount(ulong value)
		{
			return BitOperations.PopCount(value);
		}

		// Token: 0x060019DA RID: 6618 RVA: 0x00054A7E File Offset: 0x00052C7E
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int PopCount([NativeInteger] UIntPtr value)
		{
			if (IntPtr.Size == 8)
			{
				return BitOperationsEx.PopCount((ulong)value);
			}
			return BitOperationsEx.PopCount((uint)value);
		}

		// Token: 0x060019DB RID: 6619 RVA: 0x00054A97 File Offset: 0x00052C97
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TrailingZeroCount(int value)
		{
			return BitOperations.TrailingZeroCount(value);
		}

		// Token: 0x060019DC RID: 6620 RVA: 0x0005480D File Offset: 0x00052A0D
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TrailingZeroCount(uint value)
		{
			return BitOperations.TrailingZeroCount(value);
		}

		// Token: 0x060019DD RID: 6621 RVA: 0x00054A9F File Offset: 0x00052C9F
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TrailingZeroCount(long value)
		{
			return BitOperations.TrailingZeroCount(value);
		}

		// Token: 0x060019DE RID: 6622 RVA: 0x0005483F File Offset: 0x00052A3F
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TrailingZeroCount(ulong value)
		{
			return BitOperations.TrailingZeroCount(value);
		}

		// Token: 0x060019DF RID: 6623 RVA: 0x00054AA7 File Offset: 0x00052CA7
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TrailingZeroCount([NativeInteger] IntPtr value)
		{
			if (IntPtr.Size == 8)
			{
				return BitOperationsEx.TrailingZeroCount((long)value);
			}
			return BitOperationsEx.TrailingZeroCount((int)value);
		}

		// Token: 0x060019E0 RID: 6624 RVA: 0x00054AC0 File Offset: 0x00052CC0
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TrailingZeroCount([NativeInteger] UIntPtr value)
		{
			if (IntPtr.Size == 8)
			{
				return BitOperationsEx.TrailingZeroCount((ulong)value);
			}
			return BitOperationsEx.TrailingZeroCount((uint)value);
		}

		// Token: 0x060019E1 RID: 6625 RVA: 0x00054AD9 File Offset: 0x00052CD9
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint RotateLeft(uint value, int offset)
		{
			return BitOperations.RotateLeft(value, offset);
		}

		// Token: 0x060019E2 RID: 6626 RVA: 0x00054AE2 File Offset: 0x00052CE2
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong RotateLeft(ulong value, int offset)
		{
			return BitOperations.RotateLeft(value, offset);
		}

		// Token: 0x060019E3 RID: 6627 RVA: 0x00054AEB File Offset: 0x00052CEB
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: NativeInteger]
		public static UIntPtr RotateLeft([NativeInteger] UIntPtr value, int offset)
		{
			if (IntPtr.Size == 8)
			{
				return (UIntPtr)BitOperationsEx.RotateLeft((ulong)value, offset);
			}
			return (UIntPtr)BitOperationsEx.RotateLeft((uint)value, offset);
		}

		// Token: 0x060019E4 RID: 6628 RVA: 0x00054B08 File Offset: 0x00052D08
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint RotateRight(uint value, int offset)
		{
			return BitOperations.RotateRight(value, offset);
		}

		// Token: 0x060019E5 RID: 6629 RVA: 0x00054B11 File Offset: 0x00052D11
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong RotateRight(ulong value, int offset)
		{
			return BitOperations.RotateRight(value, offset);
		}

		// Token: 0x060019E6 RID: 6630 RVA: 0x00054B1A File Offset: 0x00052D1A
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: NativeInteger]
		public static UIntPtr RotateRight([NativeInteger] UIntPtr value, int offset)
		{
			if (IntPtr.Size == 8)
			{
				return (UIntPtr)BitOperationsEx.RotateRight((ulong)value, offset);
			}
			return (UIntPtr)BitOperationsEx.RotateRight((uint)value, offset);
		}
	}
}
