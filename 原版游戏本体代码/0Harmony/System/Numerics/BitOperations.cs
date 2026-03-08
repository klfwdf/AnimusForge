using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics
{
	// Token: 0x02000485 RID: 1157
	internal static class BitOperations
	{
		// Token: 0x170005BC RID: 1468
		// (get) Token: 0x060019B2 RID: 6578 RVA: 0x000546BA File Offset: 0x000528BA
		private unsafe static ReadOnlySpan<byte> TrailingZeroCountDeBruijn
		{
			get
			{
				return new ReadOnlySpan<byte>((void*)(&<1c2fb156-e9ba-45cc-af54-d5335bdb59af><PrivateImplementationDetails>.3BF63951626584EB1653F9B8DBB590A5EE1EAE1135A904B9317C3773896DF076), 32);
			}
		}

		// Token: 0x170005BD RID: 1469
		// (get) Token: 0x060019B3 RID: 6579 RVA: 0x000546C8 File Offset: 0x000528C8
		private unsafe static ReadOnlySpan<byte> Log2DeBruijn
		{
			get
			{
				return new ReadOnlySpan<byte>((void*)(&<1c2fb156-e9ba-45cc-af54-d5335bdb59af><PrivateImplementationDetails>.4BCD43D478B9229AB7A13406353712C7944B60348C36B4D0E6B789D10F697652), 32);
			}
		}

		// Token: 0x060019B4 RID: 6580 RVA: 0x000546D6 File Offset: 0x000528D6
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LeadingZeroCount(uint value)
		{
			if (value == 0U)
			{
				return 32;
			}
			return 31 ^ BitOperations.Log2SoftwareFallback(value);
		}

		// Token: 0x060019B5 RID: 6581 RVA: 0x000546E8 File Offset: 0x000528E8
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LeadingZeroCount(ulong value)
		{
			uint hi = (uint)(value >> 32);
			if (hi == 0U)
			{
				return 32 + BitOperations.LeadingZeroCount((uint)value);
			}
			return BitOperations.LeadingZeroCount(hi);
		}

		// Token: 0x060019B6 RID: 6582 RVA: 0x0005470F File Offset: 0x0005290F
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Log2(uint value)
		{
			value |= 1U;
			return BitOperations.Log2SoftwareFallback(value);
		}

		// Token: 0x060019B7 RID: 6583 RVA: 0x0005471C File Offset: 0x0005291C
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Log2(ulong value)
		{
			value |= 1UL;
			uint hi = (uint)(value >> 32);
			if (hi == 0U)
			{
				return BitOperations.Log2((uint)value);
			}
			return 32 + BitOperations.Log2(hi);
		}

		// Token: 0x060019B8 RID: 6584 RVA: 0x0005474C File Offset: 0x0005294C
		private unsafe static int Log2SoftwareFallback(uint value)
		{
			value |= value >> 1;
			value |= value >> 2;
			value |= value >> 4;
			value |= value >> 8;
			value |= value >> 16;
			return (int)(*Unsafe.AddByteOffset<byte>(MemoryMarshal.GetReference<byte>(BitOperations.Log2DeBruijn), (IntPtr)((int)(value * 130329821U >> 27))));
		}

		// Token: 0x060019B9 RID: 6585 RVA: 0x0005479C File Offset: 0x0005299C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int Log2Ceiling(uint value)
		{
			int result = BitOperations.Log2(value);
			if (BitOperations.PopCount(value) != 1)
			{
				result++;
			}
			return result;
		}

		// Token: 0x060019BA RID: 6586 RVA: 0x000547C0 File Offset: 0x000529C0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int Log2Ceiling(ulong value)
		{
			int result = BitOperations.Log2(value);
			if (BitOperations.PopCount(value) != 1)
			{
				result++;
			}
			return result;
		}

		// Token: 0x060019BB RID: 6587 RVA: 0x000547E2 File Offset: 0x000529E2
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int PopCount(uint value)
		{
			return BitOperations.<PopCount>g__SoftwareFallback|11_0(value);
		}

		// Token: 0x060019BC RID: 6588 RVA: 0x000547EA File Offset: 0x000529EA
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int PopCount(ulong value)
		{
			if (IntPtr.Size == 8)
			{
				return BitOperations.PopCount((uint)value) + BitOperations.PopCount((uint)(value >> 32));
			}
			return BitOperations.<PopCount>g__SoftwareFallback|12_0(value);
		}

		// Token: 0x060019BD RID: 6589 RVA: 0x0005480D File Offset: 0x00052A0D
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TrailingZeroCount(int value)
		{
			return BitOperations.TrailingZeroCount((uint)value);
		}

		// Token: 0x060019BE RID: 6590 RVA: 0x00054815 File Offset: 0x00052A15
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int TrailingZeroCount(uint value)
		{
			if (value == 0U)
			{
				return 32;
			}
			return (int)(*Unsafe.AddByteOffset<byte>(MemoryMarshal.GetReference<byte>(BitOperations.TrailingZeroCountDeBruijn), (IntPtr)((int)((value & -value) * 125613361U >> 27))));
		}

		// Token: 0x060019BF RID: 6591 RVA: 0x0005483F File Offset: 0x00052A3F
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TrailingZeroCount(long value)
		{
			return BitOperations.TrailingZeroCount((ulong)value);
		}

		// Token: 0x060019C0 RID: 6592 RVA: 0x00054848 File Offset: 0x00052A48
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TrailingZeroCount(ulong value)
		{
			uint lo = (uint)value;
			if (lo == 0U)
			{
				return 32 + BitOperations.TrailingZeroCount((uint)(value >> 32));
			}
			return BitOperations.TrailingZeroCount(lo);
		}

		// Token: 0x060019C1 RID: 6593 RVA: 0x00037DF2 File Offset: 0x00035FF2
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint RotateLeft(uint value, int offset)
		{
			return (value << offset) | (value >> 32 - offset);
		}

		// Token: 0x060019C2 RID: 6594 RVA: 0x0005486F File Offset: 0x00052A6F
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong RotateLeft(ulong value, int offset)
		{
			return (value << offset) | (value >> 64 - offset);
		}

		// Token: 0x060019C3 RID: 6595 RVA: 0x00037DE0 File Offset: 0x00035FE0
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint RotateRight(uint value, int offset)
		{
			return (value >> offset) | (value << 32 - offset);
		}

		// Token: 0x060019C4 RID: 6596 RVA: 0x00054881 File Offset: 0x00052A81
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong RotateRight(ulong value, int offset)
		{
			return (value >> offset) | (value << 64 - offset);
		}

		// Token: 0x060019C5 RID: 6597 RVA: 0x00054893 File Offset: 0x00052A93
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static uint ResetLowestSetBit(uint value)
		{
			return value & (value - 1U);
		}

		// Token: 0x060019C6 RID: 6598 RVA: 0x0005489A File Offset: 0x00052A9A
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static uint ResetBit(uint value, int bitPos)
		{
			return value & ~(1U << bitPos);
		}

		// Token: 0x060019C7 RID: 6599 RVA: 0x000548A5 File Offset: 0x00052AA5
		[CompilerGenerated]
		internal static int <PopCount>g__SoftwareFallback|11_0(uint value)
		{
			value -= (value >> 1) & 1431655765U;
			value = (value & 858993459U) + ((value >> 2) & 858993459U);
			value = ((value + (value >> 4)) & 252645135U) * 16843009U >> 24;
			return (int)value;
		}

		// Token: 0x060019C8 RID: 6600 RVA: 0x000548E0 File Offset: 0x00052AE0
		[CompilerGenerated]
		internal static int <PopCount>g__SoftwareFallback|12_0(ulong value)
		{
			value -= (value >> 1) & 6148914691236517205UL;
			value = (value & 3689348814741910323UL) + ((value >> 2) & 3689348814741910323UL);
			value = ((value + (value >> 4)) & 1085102592571150095UL) * 72340172838076673UL >> 56;
			return (int)value;
		}
	}
}
