using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics
{
	// Token: 0x02000023 RID: 35
	public static class BitOperations
	{
		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000161 RID: 353 RVA: 0x00009612 File Offset: 0x00007812
		private unsafe static ReadOnlySpan<byte> TrailingZeroCountDeBruijn
		{
			get
			{
				return new ReadOnlySpan<byte>((void*)(&<PrivateImplementationDetails>.3BF63951626584EB1653F9B8DBB590A5EE1EAE1135A904B9317C3773896DF076), 32);
			}
		}

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000162 RID: 354 RVA: 0x00009620 File Offset: 0x00007820
		private unsafe static ReadOnlySpan<byte> Log2DeBruijn
		{
			get
			{
				return new ReadOnlySpan<byte>((void*)(&<PrivateImplementationDetails>.4BCD43D478B9229AB7A13406353712C7944B60348C36B4D0E6B789D10F697652), 32);
			}
		}

		// Token: 0x06000163 RID: 355 RVA: 0x0000962E File Offset: 0x0000782E
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

		// Token: 0x06000164 RID: 356 RVA: 0x00009640 File Offset: 0x00007840
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LeadingZeroCount(ulong value)
		{
			uint num = (uint)(value >> 32);
			if (num == 0U)
			{
				return 32 + BitOperations.LeadingZeroCount((uint)value);
			}
			return BitOperations.LeadingZeroCount(num);
		}

		// Token: 0x06000165 RID: 357 RVA: 0x00009667 File Offset: 0x00007867
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Log2(uint value)
		{
			value |= 1U;
			return BitOperations.Log2SoftwareFallback(value);
		}

		// Token: 0x06000166 RID: 358 RVA: 0x00009674 File Offset: 0x00007874
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Log2(ulong value)
		{
			value |= 1UL;
			uint num = (uint)(value >> 32);
			if (num == 0U)
			{
				return BitOperations.Log2((uint)value);
			}
			return 32 + BitOperations.Log2(num);
		}

		// Token: 0x06000167 RID: 359 RVA: 0x000096A4 File Offset: 0x000078A4
		private unsafe static int Log2SoftwareFallback(uint value)
		{
			value |= value >> 1;
			value |= value >> 2;
			value |= value >> 4;
			value |= value >> 8;
			value |= value >> 16;
			return (int)(*Unsafe.AddByteOffset<byte>(MemoryMarshal.GetReference<byte>(BitOperations.Log2DeBruijn), (IntPtr)((int)(value * 130329821U >> 27))));
		}

		// Token: 0x06000168 RID: 360 RVA: 0x000096F4 File Offset: 0x000078F4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int Log2Ceiling(uint value)
		{
			int num = BitOperations.Log2(value);
			if (BitOperations.PopCount(value) != 1)
			{
				num++;
			}
			return num;
		}

		// Token: 0x06000169 RID: 361 RVA: 0x00009718 File Offset: 0x00007918
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int Log2Ceiling(ulong value)
		{
			int num = BitOperations.Log2(value);
			if (BitOperations.PopCount(value) != 1)
			{
				num++;
			}
			return num;
		}

		// Token: 0x0600016A RID: 362 RVA: 0x0000973A File Offset: 0x0000793A
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int PopCount(uint value)
		{
			return BitOperations.<PopCount>g__SoftwareFallback|11_0(value);
		}

		// Token: 0x0600016B RID: 363 RVA: 0x00009742 File Offset: 0x00007942
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

		// Token: 0x0600016C RID: 364 RVA: 0x00009765 File Offset: 0x00007965
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TrailingZeroCount(int value)
		{
			return BitOperations.TrailingZeroCount((uint)value);
		}

		// Token: 0x0600016D RID: 365 RVA: 0x0000976D File Offset: 0x0000796D
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

		// Token: 0x0600016E RID: 366 RVA: 0x00009797 File Offset: 0x00007997
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TrailingZeroCount(long value)
		{
			return BitOperations.TrailingZeroCount((ulong)value);
		}

		// Token: 0x0600016F RID: 367 RVA: 0x000097A0 File Offset: 0x000079A0
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TrailingZeroCount(ulong value)
		{
			uint num = (uint)value;
			if (num == 0U)
			{
				return 32 + BitOperations.TrailingZeroCount((uint)(value >> 32));
			}
			return BitOperations.TrailingZeroCount(num);
		}

		// Token: 0x06000170 RID: 368 RVA: 0x000097C7 File Offset: 0x000079C7
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint RotateLeft(uint value, int offset)
		{
			return (value << offset) | (value >> 32 - offset);
		}

		// Token: 0x06000171 RID: 369 RVA: 0x000097D9 File Offset: 0x000079D9
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong RotateLeft(ulong value, int offset)
		{
			return (value << offset) | (value >> 64 - offset);
		}

		// Token: 0x06000172 RID: 370 RVA: 0x000097EB File Offset: 0x000079EB
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint RotateRight(uint value, int offset)
		{
			return (value >> offset) | (value << 32 - offset);
		}

		// Token: 0x06000173 RID: 371 RVA: 0x000097FD File Offset: 0x000079FD
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong RotateRight(ulong value, int offset)
		{
			return (value >> offset) | (value << 64 - offset);
		}

		// Token: 0x06000174 RID: 372 RVA: 0x0000980F File Offset: 0x00007A0F
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static uint ResetLowestSetBit(uint value)
		{
			return value & (value - 1U);
		}

		// Token: 0x06000175 RID: 373 RVA: 0x00009816 File Offset: 0x00007A16
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static uint ResetBit(uint value, int bitPos)
		{
			return value & ~(1U << bitPos);
		}

		// Token: 0x06000176 RID: 374 RVA: 0x00009821 File Offset: 0x00007A21
		[CompilerGenerated]
		internal static int <PopCount>g__SoftwareFallback|11_0(uint value)
		{
			value -= (value >> 1) & 1431655765U;
			value = (value & 858993459U) + ((value >> 2) & 858993459U);
			value = ((value + (value >> 4)) & 252645135U) * 16843009U >> 24;
			return (int)value;
		}

		// Token: 0x06000177 RID: 375 RVA: 0x0000985C File Offset: 0x00007A5C
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
