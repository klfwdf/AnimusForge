using System;
using System.Runtime.InteropServices;

namespace StbSharp
{
	// Token: 0x0200000D RID: 13
	internal static class CRuntime
	{
		// Token: 0x0600021A RID: 538 RVA: 0x000203E5 File Offset: 0x0001E5E5
		public unsafe static void* malloc(ulong size)
		{
			return CRuntime.malloc((long)size);
		}

		// Token: 0x0600021B RID: 539 RVA: 0x000203F0 File Offset: 0x0001E5F0
		public unsafe static void* malloc(long size)
		{
			return Marshal.AllocHGlobal((int)size).ToPointer();
		}

		// Token: 0x0600021C RID: 540 RVA: 0x0002040C File Offset: 0x0001E60C
		public unsafe static void memcpy(void* a, void* b, long size)
		{
			byte* ptr = (byte*)a;
			byte* ptr2 = (byte*)b;
			for (long num = 0L; num < size; num += 1L)
			{
				*(ptr++) = *(ptr2++);
			}
		}

		// Token: 0x0600021D RID: 541 RVA: 0x00020437 File Offset: 0x0001E637
		public unsafe static void memcpy(void* a, void* b, ulong size)
		{
			CRuntime.memcpy(a, b, (long)size);
		}

		// Token: 0x0600021E RID: 542 RVA: 0x00020444 File Offset: 0x0001E644
		public unsafe static void memmove(void* a, void* b, long size)
		{
			void* ptr = null;
			try
			{
				ptr = CRuntime.malloc(size);
				CRuntime.memcpy(ptr, b, size);
				CRuntime.memcpy(a, ptr, size);
			}
			finally
			{
				if (ptr != null)
				{
					CRuntime.free(ptr);
				}
			}
		}

		// Token: 0x0600021F RID: 543 RVA: 0x0002048C File Offset: 0x0001E68C
		public unsafe static void memmove(void* a, void* b, ulong size)
		{
			CRuntime.memmove(a, b, (long)size);
		}

		// Token: 0x06000220 RID: 544 RVA: 0x00020498 File Offset: 0x0001E698
		public unsafe static int memcmp(void* a, void* b, long size)
		{
			int num = 0;
			byte* ptr = (byte*)a;
			byte* ptr2 = (byte*)b;
			for (long num2 = 0L; num2 < size; num2 += 1L)
			{
				if (*ptr != *ptr2)
				{
					num++;
				}
				ptr++;
				ptr2++;
			}
			return num;
		}

		// Token: 0x06000221 RID: 545 RVA: 0x000204CC File Offset: 0x0001E6CC
		public unsafe static int memcmp(void* a, void* b, ulong size)
		{
			return CRuntime.memcmp(a, b, (long)size);
		}

		// Token: 0x06000222 RID: 546 RVA: 0x000204D8 File Offset: 0x0001E6D8
		public unsafe static int memcmp(byte* a, byte[] b, ulong size)
		{
			void* b2;
			if (b == null || b.Length == 0)
			{
				b2 = null;
			}
			else
			{
				b2 = (void*)(&b[0]);
			}
			return CRuntime.memcmp((void*)a, b2, (long)size);
		}

		// Token: 0x06000223 RID: 547 RVA: 0x00020505 File Offset: 0x0001E705
		public unsafe static void free(void* a)
		{
			Marshal.FreeHGlobal(new IntPtr(a));
		}

		// Token: 0x06000224 RID: 548 RVA: 0x00020514 File Offset: 0x0001E714
		public unsafe static void memset(void* ptr, int value, long size)
		{
			byte* ptr2 = (byte*)ptr;
			byte b = (byte)value;
			for (long num = 0L; num < size; num += 1L)
			{
				*(ptr2++) = b;
			}
		}

		// Token: 0x06000225 RID: 549 RVA: 0x0002053B File Offset: 0x0001E73B
		public unsafe static void memset(void* ptr, int value, ulong size)
		{
			CRuntime.memset(ptr, value, (long)size);
		}

		// Token: 0x06000226 RID: 550 RVA: 0x00020545 File Offset: 0x0001E745
		public static uint _lrotl(uint x, int y)
		{
			return (x << y) | (x >> 32 - y);
		}

		// Token: 0x06000227 RID: 551 RVA: 0x00020558 File Offset: 0x0001E758
		public unsafe static void* realloc(void* a, long newSize)
		{
			if (a == null)
			{
				return CRuntime.malloc(newSize);
			}
			return Marshal.ReAllocHGlobal(new IntPtr(a), new IntPtr(newSize)).ToPointer();
		}

		// Token: 0x06000228 RID: 552 RVA: 0x0002058A File Offset: 0x0001E78A
		public unsafe static void* realloc(void* a, ulong newSize)
		{
			return CRuntime.realloc(a, (long)newSize);
		}

		// Token: 0x06000229 RID: 553 RVA: 0x00020593 File Offset: 0x0001E793
		public static int abs(int v)
		{
			return Math.Abs(v);
		}

		// Token: 0x0600022A RID: 554 RVA: 0x0002059C File Offset: 0x0001E79C
		public unsafe static double frexp(double number, int* exponent)
		{
			long num = BitConverter.DoubleToInt64Bits(number);
			int num2 = (int)((num & 9218868437227405312L) >> 52);
			*exponent = 0;
			if (num2 == 2047 || number == 0.0)
			{
				number += number;
			}
			else
			{
				*exponent = num2 - 1022;
				if (num2 == 0)
				{
					number *= BitConverter.Int64BitsToDouble(4850376798678024192L);
					num = BitConverter.DoubleToInt64Bits(number);
					num2 = (int)((num & 9218868437227405312L) >> 52);
					*exponent = num2 - 1022 - 54;
				}
				number = BitConverter.Int64BitsToDouble((num & -9218868437227405313L) | 4602678819172646912L);
			}
			return number;
		}

		// Token: 0x0600022B RID: 555 RVA: 0x0002063C File Offset: 0x0001E83C
		public static double pow(double a, double b)
		{
			return Math.Pow(a, b);
		}

		// Token: 0x0600022C RID: 556 RVA: 0x00020645 File Offset: 0x0001E845
		public static float fabs(double a)
		{
			return (float)Math.Abs(a);
		}

		// Token: 0x0600022D RID: 557 RVA: 0x0002064E File Offset: 0x0001E84E
		public static double ceil(double a)
		{
			return Math.Ceiling(a);
		}

		// Token: 0x0600022E RID: 558 RVA: 0x00020656 File Offset: 0x0001E856
		public static double floor(double a)
		{
			return Math.Floor(a);
		}

		// Token: 0x0600022F RID: 559 RVA: 0x0002065E File Offset: 0x0001E85E
		public static double log(double value)
		{
			return Math.Log(value);
		}

		// Token: 0x06000230 RID: 560 RVA: 0x00020666 File Offset: 0x0001E866
		public static double exp(double value)
		{
			return Math.Exp(value);
		}

		// Token: 0x06000231 RID: 561 RVA: 0x0002066E File Offset: 0x0001E86E
		public static double cos(double value)
		{
			return Math.Cos(value);
		}

		// Token: 0x06000232 RID: 562 RVA: 0x00020676 File Offset: 0x0001E876
		public static double acos(double value)
		{
			return Math.Acos(value);
		}

		// Token: 0x06000233 RID: 563 RVA: 0x0002067E File Offset: 0x0001E87E
		public static double sin(double value)
		{
			return Math.Sin(value);
		}

		// Token: 0x06000234 RID: 564 RVA: 0x00020686 File Offset: 0x0001E886
		public static double ldexp(double number, int exponent)
		{
			return number * Math.Pow(2.0, (double)exponent);
		}

		// Token: 0x06000235 RID: 565 RVA: 0x0002069C File Offset: 0x0001E89C
		private unsafe static void qsortSwap(byte* data, long size, long pos1, long pos2)
		{
			byte* ptr = data + size * pos1;
			byte* ptr2 = data + size * pos2;
			for (long num = 0L; num < size; num += 1L)
			{
				byte b = *ptr;
				*ptr = *ptr2;
				*ptr2 = b;
				ptr++;
				ptr2++;
			}
		}

		// Token: 0x06000236 RID: 566 RVA: 0x000206D8 File Offset: 0x0001E8D8
		private unsafe static long qsortPartition(byte* data, long size, CRuntime.QSortComparer comparer, long left, long right)
		{
			void* b = (void*)(data + size * left);
			long num = left - 1L;
			long num2 = right + 1L;
			for (;;)
			{
				num += 1L;
				if (comparer((void*)(data + size * num), b) >= 0)
				{
					do
					{
						num2 -= 1L;
					}
					while (comparer((void*)(data + size * num2), b) > 0);
					if (num >= num2)
					{
						break;
					}
					CRuntime.qsortSwap(data, size, num, num2);
				}
			}
			return num2;
		}

		// Token: 0x06000237 RID: 567 RVA: 0x00020734 File Offset: 0x0001E934
		private unsafe static void qsortInternal(byte* data, long size, CRuntime.QSortComparer comparer, long left, long right)
		{
			if (left < right)
			{
				long num = CRuntime.qsortPartition(data, size, comparer, left, right);
				CRuntime.qsortInternal(data, size, comparer, left, num);
				CRuntime.qsortInternal(data, size, comparer, num + 1L, right);
			}
		}

		// Token: 0x06000238 RID: 568 RVA: 0x0002076A File Offset: 0x0001E96A
		public unsafe static void qsort(void* data, ulong count, ulong size, CRuntime.QSortComparer comparer)
		{
			CRuntime.qsortInternal((byte*)data, (long)size, comparer, 0L, (long)(count - 1UL));
		}

		// Token: 0x06000239 RID: 569 RVA: 0x0002077A File Offset: 0x0001E97A
		public static double sqrt(double val)
		{
			return Math.Sqrt(val);
		}

		// Token: 0x0600023A RID: 570 RVA: 0x00020782 File Offset: 0x0001E982
		public static double fmod(double x, double y)
		{
			return x % y;
		}

		// Token: 0x0600023B RID: 571 RVA: 0x00020788 File Offset: 0x0001E988
		public unsafe static ulong strlen(sbyte* str)
		{
			sbyte* ptr = str;
			while (*ptr != 0)
			{
				ptr++;
			}
			return (ulong)(ptr - str - 1);
		}

		// Token: 0x040000C4 RID: 196
		public const long DBL_EXP_MASK = 9218868437227405312L;

		// Token: 0x040000C5 RID: 197
		public const int DBL_MANT_BITS = 52;

		// Token: 0x040000C6 RID: 198
		public const long DBL_SGN_MASK = -9223372036854775808L;

		// Token: 0x040000C7 RID: 199
		public const long DBL_MANT_MASK = 4503599627370495L;

		// Token: 0x040000C8 RID: 200
		public const long DBL_EXP_CLR_MASK = -9218868437227405313L;

		// Token: 0x0200004C RID: 76
		// (Invoke) Token: 0x0600026E RID: 622
		public unsafe delegate int QSortComparer(void* a, void* b);
	}
}
