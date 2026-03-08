using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Threading;

namespace System.Collections
{
	// Token: 0x02000498 RID: 1176
	[FriendAccessAllowed]
	internal static class HashHelpers
	{
		// Token: 0x1700086A RID: 2154
		// (get) Token: 0x06003899 RID: 14489 RVA: 0x000D9AAC File Offset: 0x000D7CAC
		internal static ConditionalWeakTable<object, SerializationInfo> SerializationInfoTable
		{
			get
			{
				if (HashHelpers.s_SerializationInfoTable == null)
				{
					ConditionalWeakTable<object, SerializationInfo> value = new ConditionalWeakTable<object, SerializationInfo>();
					Interlocked.CompareExchange<ConditionalWeakTable<object, SerializationInfo>>(ref HashHelpers.s_SerializationInfoTable, value, null);
				}
				return HashHelpers.s_SerializationInfoTable;
			}
		}

		// Token: 0x0600389A RID: 14490 RVA: 0x000D9AD8 File Offset: 0x000D7CD8
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
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

		// Token: 0x0600389B RID: 14491 RVA: 0x000D9B0C File Offset: 0x000D7D0C
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static int GetPrime(int min)
		{
			if (min < 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_HTCapacityOverflow"));
			}
			for (int i = 0; i < HashHelpers.primes.Length; i++)
			{
				int num = HashHelpers.primes[i];
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

		// Token: 0x0600389C RID: 14492 RVA: 0x000D9B72 File Offset: 0x000D7D72
		public static int GetMinPrime()
		{
			return HashHelpers.primes[0];
		}

		// Token: 0x0600389D RID: 14493 RVA: 0x000D9B7C File Offset: 0x000D7D7C
		public static int ExpandPrime(int oldSize)
		{
			int num = 2 * oldSize;
			if (num > 2146435069 && 2146435069 > oldSize)
			{
				return 2146435069;
			}
			return HashHelpers.GetPrime(num);
		}

		// Token: 0x0600389E RID: 14494 RVA: 0x000D9BA9 File Offset: 0x000D7DA9
		public static bool IsWellKnownEqualityComparer(object comparer)
		{
			return comparer == null || comparer == EqualityComparer<string>.Default || comparer is IWellKnownStringEqualityComparer;
		}

		// Token: 0x0600389F RID: 14495 RVA: 0x000D9BC4 File Offset: 0x000D7DC4
		public static IEqualityComparer GetRandomizedEqualityComparer(object comparer)
		{
			if (comparer == null)
			{
				return new RandomizedObjectEqualityComparer();
			}
			if (comparer == EqualityComparer<string>.Default)
			{
				return new RandomizedStringEqualityComparer();
			}
			IWellKnownStringEqualityComparer wellKnownStringEqualityComparer = comparer as IWellKnownStringEqualityComparer;
			if (wellKnownStringEqualityComparer != null)
			{
				return wellKnownStringEqualityComparer.GetRandomizedEqualityComparer();
			}
			return null;
		}

		// Token: 0x060038A0 RID: 14496 RVA: 0x000D9BFC File Offset: 0x000D7DFC
		public static object GetEqualityComparerForSerialization(object comparer)
		{
			if (comparer == null)
			{
				return null;
			}
			IWellKnownStringEqualityComparer wellKnownStringEqualityComparer = comparer as IWellKnownStringEqualityComparer;
			if (wellKnownStringEqualityComparer != null)
			{
				return wellKnownStringEqualityComparer.GetEqualityComparerForSerialization();
			}
			return comparer;
		}

		// Token: 0x060038A1 RID: 14497 RVA: 0x000D9C20 File Offset: 0x000D7E20
		internal static long GetEntropy()
		{
			object obj = HashHelpers.lockObj;
			long result;
			lock (obj)
			{
				if (HashHelpers.currentIndex == 1024)
				{
					if (HashHelpers.rng == null)
					{
						HashHelpers.rng = RandomNumberGenerator.Create();
						HashHelpers.data = new byte[1024];
					}
					HashHelpers.rng.GetBytes(HashHelpers.data);
					HashHelpers.currentIndex = 0;
				}
				long num = BitConverter.ToInt64(HashHelpers.data, HashHelpers.currentIndex);
				HashHelpers.currentIndex += 8;
				result = num;
			}
			return result;
		}

		// Token: 0x040018F7 RID: 6391
		public const int HashCollisionThreshold = 100;

		// Token: 0x040018F8 RID: 6392
		public static bool s_UseRandomizedStringHashing = string.UseRandomizedHashing();

		// Token: 0x040018F9 RID: 6393
		public static readonly int[] primes = new int[]
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

		// Token: 0x040018FA RID: 6394
		private static ConditionalWeakTable<object, SerializationInfo> s_SerializationInfoTable;

		// Token: 0x040018FB RID: 6395
		public const int MaxPrimeArrayLength = 2146435069;

		// Token: 0x040018FC RID: 6396
		private const int bufferSize = 1024;

		// Token: 0x040018FD RID: 6397
		private static RandomNumberGenerator rng;

		// Token: 0x040018FE RID: 6398
		private static byte[] data;

		// Token: 0x040018FF RID: 6399
		private static int currentIndex = 1024;

		// Token: 0x04001900 RID: 6400
		private static readonly object lockObj = new object();
	}
}
