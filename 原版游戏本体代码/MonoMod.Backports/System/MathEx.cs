using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x02000012 RID: 18
	public static class MathEx
	{
		// Token: 0x06000030 RID: 48 RVA: 0x00002FB6 File Offset: 0x000011B6
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte Clamp(byte value, byte min, byte max)
		{
			if (min > max)
			{
				MathEx.ThrowMinMaxException<byte>(min, max);
			}
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00002FD0 File Offset: 0x000011D0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static decimal Clamp(decimal value, decimal min, decimal max)
		{
			if (min > max)
			{
				MathEx.ThrowMinMaxException<decimal>(min, max);
			}
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00002FF9 File Offset: 0x000011F9
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double Clamp(double value, double min, double max)
		{
			if (min > max)
			{
				MathEx.ThrowMinMaxException<double>(min, max);
			}
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00003013 File Offset: 0x00001213
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short Clamp(short value, short min, short max)
		{
			if (min > max)
			{
				MathEx.ThrowMinMaxException<short>(min, max);
			}
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		// Token: 0x06000034 RID: 52 RVA: 0x0000302D File Offset: 0x0000122D
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Clamp(int value, int min, int max)
		{
			if (min > max)
			{
				MathEx.ThrowMinMaxException<int>(min, max);
			}
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00003047 File Offset: 0x00001247
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long Clamp(long value, long min, long max)
		{
			if (min > max)
			{
				MathEx.ThrowMinMaxException<long>(min, max);
			}
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00003061 File Offset: 0x00001261
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: NativeInteger]
		public static IntPtr Clamp([NativeInteger] IntPtr value, [NativeInteger] IntPtr min, [NativeInteger] IntPtr max)
		{
			if (min > max)
			{
				MathEx.ThrowMinMaxException<IntPtr>(min, max);
			}
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		// Token: 0x06000037 RID: 55 RVA: 0x0000307B File Offset: 0x0000127B
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static sbyte Clamp(sbyte value, sbyte min, sbyte max)
		{
			if (min > max)
			{
				MathEx.ThrowMinMaxException<sbyte>(min, max);
			}
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00003095 File Offset: 0x00001295
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Clamp(float value, float min, float max)
		{
			if (min > max)
			{
				MathEx.ThrowMinMaxException<float>(min, max);
			}
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		// Token: 0x06000039 RID: 57 RVA: 0x000030AF File Offset: 0x000012AF
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort Clamp(ushort value, ushort min, ushort max)
		{
			if (min > max)
			{
				MathEx.ThrowMinMaxException<ushort>(min, max);
			}
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		// Token: 0x0600003A RID: 58 RVA: 0x000030C9 File Offset: 0x000012C9
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint Clamp(uint value, uint min, uint max)
		{
			if (min > max)
			{
				MathEx.ThrowMinMaxException<uint>(min, max);
			}
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		// Token: 0x0600003B RID: 59 RVA: 0x000030E3 File Offset: 0x000012E3
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong Clamp(ulong value, ulong min, ulong max)
		{
			if (min > max)
			{
				MathEx.ThrowMinMaxException<ulong>(min, max);
			}
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		// Token: 0x0600003C RID: 60 RVA: 0x000030FD File Offset: 0x000012FD
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: NativeInteger]
		public static UIntPtr Clamp([NativeInteger] UIntPtr value, [NativeInteger] UIntPtr min, [NativeInteger] UIntPtr max)
		{
			if (min > max)
			{
				MathEx.ThrowMinMaxException<UIntPtr>(min, max);
			}
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00003118 File Offset: 0x00001318
		[NullableContext(1)]
		[DoesNotReturn]
		private static void ThrowMinMaxException<[Nullable(2)] T>(T min, T max)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Minimum ");
			defaultInterpolatedStringHandler.AppendFormatted<T>(min);
			defaultInterpolatedStringHandler.AppendLiteral(" is less than maximum ");
			defaultInterpolatedStringHandler.AppendFormatted<T>(max);
			throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
		}
	}
}
