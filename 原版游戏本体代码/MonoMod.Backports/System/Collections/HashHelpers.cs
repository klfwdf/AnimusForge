using System;
using System.Runtime.CompilerServices;

namespace System.Collections
{
	// Token: 0x02000026 RID: 38
	internal static class HashHelpers
	{
		// Token: 0x06000198 RID: 408 RVA: 0x00009AF0 File Offset: 0x00007CF0
		public static bool IsPrime(int candidate)
		{
			if ((candidate & 1) != 0)
			{
				int num = (int)Math.Sqrt((double)candidate);
				for (int i = 3; i <= num; i += 2)
				{
					if (candidate % i == 0)
					{
						return false;
					}
				}
				return true;
			}
			return candidate == 2;
		}

		// Token: 0x06000199 RID: 409 RVA: 0x00009B24 File Offset: 0x00007D24
		public static int GetPrime(int min)
		{
			if (min < 0)
			{
				throw new ArgumentException("Prime minimum cannot be less than zero");
			}
			foreach (int num in HashHelpers.s_primes)
			{
				if (num >= min)
				{
					return num;
				}
			}
			for (int j = min | 1; j < 2147483647; j += 2)
			{
				if (HashHelpers.IsPrime(j) && (j - 1) % 101 != 0)
				{
					return j;
				}
			}
			return min;
		}

		// Token: 0x0600019A RID: 410 RVA: 0x00009B84 File Offset: 0x00007D84
		public static int ExpandPrime(int oldSize)
		{
			int num = 2 * oldSize;
			if (num > 2147483587 && 2147483587 > oldSize)
			{
				return 2147483587;
			}
			return HashHelpers.GetPrime(num);
		}

		// Token: 0x0600019B RID: 411 RVA: 0x00009BB1 File Offset: 0x00007DB1
		public static ulong GetFastModMultiplier(uint divisor)
		{
			return ulong.MaxValue / (ulong)divisor + 1UL;
		}

		// Token: 0x0600019C RID: 412 RVA: 0x00009BBB File Offset: 0x00007DBB
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint FastMod(uint value, uint divisor, ulong multiplier)
		{
			return (uint)(((multiplier * (ulong)value >> 32) + 1UL) * (ulong)divisor >> 32);
		}

		// Token: 0x0400004E RID: 78
		public const uint HashCollisionThreshold = 100U;

		// Token: 0x0400004F RID: 79
		public const int MaxPrimeArrayLength = 2147483587;

		// Token: 0x04000050 RID: 80
		public const int HashPrime = 101;

		// Token: 0x04000051 RID: 81
		[Nullable(1)]
		private static readonly int[] s_primes = new int[]
		{
			3, 7, 11, 17, 23, 29, 37, 47, 59, 71,
			89, 107, 131, 163, 197, 239, 293, 353, 431, 521,
			631, 761, 919, 1103, 1327, 1597, 1931, 2333, 2801, 3371,
			4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591, 17519, 21023,
			25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363,
			156437, 187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403,
			968897, 1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559,
			5999471, 7199369
		};
	}
}
