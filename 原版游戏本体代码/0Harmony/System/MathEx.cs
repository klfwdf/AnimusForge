using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x0200046D RID: 1133
	internal static class MathEx
	{
		// Token: 0x06001877 RID: 6263 RVA: 0x0004DF4A File Offset: 0x0004C14A
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

		// Token: 0x06001878 RID: 6264 RVA: 0x0004DF64 File Offset: 0x0004C164
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

		// Token: 0x06001879 RID: 6265 RVA: 0x0004DF8D File Offset: 0x0004C18D
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

		// Token: 0x0600187A RID: 6266 RVA: 0x0004DFA7 File Offset: 0x0004C1A7
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

		// Token: 0x0600187B RID: 6267 RVA: 0x0004DFC1 File Offset: 0x0004C1C1
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

		// Token: 0x0600187C RID: 6268 RVA: 0x0004DFDB File Offset: 0x0004C1DB
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

		// Token: 0x0600187D RID: 6269 RVA: 0x0004DFF5 File Offset: 0x0004C1F5
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

		// Token: 0x0600187E RID: 6270 RVA: 0x0004E00F File Offset: 0x0004C20F
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

		// Token: 0x0600187F RID: 6271 RVA: 0x0004E029 File Offset: 0x0004C229
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

		// Token: 0x06001880 RID: 6272 RVA: 0x0004E043 File Offset: 0x0004C243
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

		// Token: 0x06001881 RID: 6273 RVA: 0x0004E05D File Offset: 0x0004C25D
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

		// Token: 0x06001882 RID: 6274 RVA: 0x0004E077 File Offset: 0x0004C277
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

		// Token: 0x06001883 RID: 6275 RVA: 0x0004E091 File Offset: 0x0004C291
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

		// Token: 0x06001884 RID: 6276 RVA: 0x0004E0AC File Offset: 0x0004C2AC
		[NullableContext(1)]
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
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
