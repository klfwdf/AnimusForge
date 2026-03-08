using System;
using System.Runtime.CompilerServices;

namespace System.Collections
{
	// Token: 0x02000488 RID: 1160
	internal static class HashHelpers
	{
		// Token: 0x060019E9 RID: 6633 RVA: 0x00054B64 File Offset: 0x00052D64
		public static bool IsPrime(int candidate)
		{
			if ((candidate & 1) != 0)
			{
				int limit = (int)Math.Sqrt((double)candidate);
				for (int divisor = 3; divisor <= limit; divisor += 2)
				{
					if (candidate % divisor == 0)
					{
						return false;
					}
				}
				return true;
			}
			return candidate == 2;
		}

		// Token: 0x060019EA RID: 6634 RVA: 0x00054B98 File Offset: 0x00052D98
		public static int GetPrime(int min)
		{
			if (min < 0)
			{
				throw new ArgumentException("Prime minimum cannot be less than zero");
			}
			foreach (int prime in HashHelpers.s_primes)
			{
				if (prime >= min)
				{
					return prime;
				}
			}
			for (int i = min | 1; i < 2147483647; i += 2)
			{
				if (HashHelpers.IsPrime(i) && (i - 1) % 101 != 0)
				{
					return i;
				}
			}
			return min;
		}

		// Token: 0x060019EB RID: 6635 RVA: 0x00054BF8 File Offset: 0x00052DF8
		public static int ExpandPrime(int oldSize)
		{
			int newSize = 2 * oldSize;
			if (newSize > 2147483587 && 2147483587 > oldSize)
			{
				return 2147483587;
			}
			return HashHelpers.GetPrime(newSize);
		}

		// Token: 0x060019EC RID: 6636 RVA: 0x00054C25 File Offset: 0x00052E25
		public static ulong GetFastModMultiplier(uint divisor)
		{
			return ulong.MaxValue / (ulong)divisor + 1UL;
		}

		// Token: 0x060019ED RID: 6637 RVA: 0x00054C2F File Offset: 0x00052E2F
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint FastMod(uint value, uint divisor, ulong multiplier)
		{
			return (uint)(((multiplier * (ulong)value >> 32) + 1UL) * (ulong)divisor >> 32);
		}

		// Token: 0x040010C7 RID: 4295
		public const uint HashCollisionThreshold = 100U;

		// Token: 0x040010C8 RID: 4296
		public const int MaxPrimeArrayLength = 2147483587;

		// Token: 0x040010C9 RID: 4297
		public const int HashPrime = 101;

		// Token: 0x040010CA RID: 4298
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
