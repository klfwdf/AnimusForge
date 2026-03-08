using System;
using System.Runtime.CompilerServices;

namespace System.Numerics
{
	// Token: 0x02000024 RID: 36
	public static class BitOperationsEx
	{
		// Token: 0x06000178 RID: 376 RVA: 0x000098B5 File Offset: 0x00007AB5
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPow2(int value)
		{
			return (value & (value - 1)) == 0 && value > 0;
		}

		// Token: 0x06000179 RID: 377 RVA: 0x000098C4 File Offset: 0x00007AC4
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPow2(uint value)
		{
			return (value & (value - 1U)) == 0U && value > 0U;
		}

		// Token: 0x0600017A RID: 378 RVA: 0x000098D3 File Offset: 0x00007AD3
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPow2(long value)
		{
			return (value & (value - 1L)) == 0L && value > 0L;
		}

		// Token: 0x0600017B RID: 379 RVA: 0x000098E4 File Offset: 0x00007AE4
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPow2(ulong value)
		{
			return (value & (value - 1UL)) == 0UL && value > 0UL;
		}

		// Token: 0x0600017C RID: 380 RVA: 0x000098F5 File Offset: 0x00007AF5
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPow2([NativeInteger] IntPtr value)
		{
			return (value & (value - (IntPtr)1)) == 0 && value > (IntPtr)0;
		}

		// Token: 0x0600017D RID: 381 RVA: 0x00009906 File Offset: 0x00007B06
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPow2([NativeInteger] UIntPtr value)
		{
			return (value & (value - (UIntPtr)((IntPtr)1))) == 0 && value > (UIntPtr)((IntPtr)0);
		}

		// Token: 0x0600017E RID: 382 RVA: 0x00009917 File Offset: 0x00007B17
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

		// Token: 0x0600017F RID: 383 RVA: 0x00009945 File Offset: 0x00007B45
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

		// Token: 0x06000180 RID: 384 RVA: 0x0000997D File Offset: 0x00007B7D
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

		// Token: 0x06000181 RID: 385 RVA: 0x00009998 File Offset: 0x00007B98
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LeadingZeroCount(uint value)
		{
			return BitOperations.LeadingZeroCount(value);
		}

		// Token: 0x06000182 RID: 386 RVA: 0x000099A0 File Offset: 0x00007BA0
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LeadingZeroCount(ulong value)
		{
			return BitOperations.LeadingZeroCount(value);
		}

		// Token: 0x06000183 RID: 387 RVA: 0x000099A8 File Offset: 0x00007BA8
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

		// Token: 0x06000184 RID: 388 RVA: 0x000099C1 File Offset: 0x00007BC1
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Log2(uint value)
		{
			return BitOperations.Log2(value);
		}

		// Token: 0x06000185 RID: 389 RVA: 0x000099C9 File Offset: 0x00007BC9
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Log2(ulong value)
		{
			return BitOperations.Log2(value);
		}

		// Token: 0x06000186 RID: 390 RVA: 0x000099D1 File Offset: 0x00007BD1
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

		// Token: 0x06000187 RID: 391 RVA: 0x000099EA File Offset: 0x00007BEA
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int PopCount(uint value)
		{
			return BitOperations.PopCount(value);
		}

		// Token: 0x06000188 RID: 392 RVA: 0x000099F2 File Offset: 0x00007BF2
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int PopCount(ulong value)
		{
			return BitOperations.PopCount(value);
		}

		// Token: 0x06000189 RID: 393 RVA: 0x000099FA File Offset: 0x00007BFA
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

		// Token: 0x0600018A RID: 394 RVA: 0x00009A13 File Offset: 0x00007C13
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TrailingZeroCount(int value)
		{
			return BitOperations.TrailingZeroCount(value);
		}

		// Token: 0x0600018B RID: 395 RVA: 0x00009A1B File Offset: 0x00007C1B
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TrailingZeroCount(uint value)
		{
			return BitOperations.TrailingZeroCount(value);
		}

		// Token: 0x0600018C RID: 396 RVA: 0x00009A23 File Offset: 0x00007C23
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TrailingZeroCount(long value)
		{
			return BitOperations.TrailingZeroCount(value);
		}

		// Token: 0x0600018D RID: 397 RVA: 0x00009A2B File Offset: 0x00007C2B
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TrailingZeroCount(ulong value)
		{
			return BitOperations.TrailingZeroCount(value);
		}

		// Token: 0x0600018E RID: 398 RVA: 0x00009A33 File Offset: 0x00007C33
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TrailingZeroCount([NativeInteger] IntPtr value)
		{
			if (IntPtr.Size == 8)
			{
				return BitOperationsEx.TrailingZeroCount((long)value);
			}
			return BitOperationsEx.TrailingZeroCount((int)value);
		}

		// Token: 0x0600018F RID: 399 RVA: 0x00009A4C File Offset: 0x00007C4C
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

		// Token: 0x06000190 RID: 400 RVA: 0x00009A65 File Offset: 0x00007C65
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint RotateLeft(uint value, int offset)
		{
			return BitOperations.RotateLeft(value, offset);
		}

		// Token: 0x06000191 RID: 401 RVA: 0x00009A6E File Offset: 0x00007C6E
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong RotateLeft(ulong value, int offset)
		{
			return BitOperations.RotateLeft(value, offset);
		}

		// Token: 0x06000192 RID: 402 RVA: 0x00009A77 File Offset: 0x00007C77
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

		// Token: 0x06000193 RID: 403 RVA: 0x00009A94 File Offset: 0x00007C94
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint RotateRight(uint value, int offset)
		{
			return BitOperations.RotateRight(value, offset);
		}

		// Token: 0x06000194 RID: 404 RVA: 0x00009A9D File Offset: 0x00007C9D
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong RotateRight(ulong value, int offset)
		{
			return BitOperations.RotateRight(value, offset);
		}

		// Token: 0x06000195 RID: 405 RVA: 0x00009AA6 File Offset: 0x00007CA6
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
