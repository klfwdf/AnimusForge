using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen;

internal readonly struct FrozenHashTable
{
	private readonly struct Bucket(int startIndex, int count)
	{
		public readonly int StartIndex = startIndex;

		public readonly int EndIndex = startIndex + count - 1;
	}

	private readonly Bucket[] _buckets;

	private readonly ulong _fastModMultiplier;

	public int Count => HashCodes.Length;

	internal int[] HashCodes { get; }

	private FrozenHashTable(int[] hashCodes, Bucket[] buckets, ulong fastModMultiplier)
	{
		HashCodes = hashCodes;
		_buckets = buckets;
		_fastModMultiplier = fastModMultiplier;
	}

	public static FrozenHashTable Create(Span<int> hashCodes, bool hashCodesAreUnique = false)
	{
		int num = CalcNumBuckets(hashCodes, hashCodesAreUnique);
		ulong fastModMultiplier = System.Collections.HashHelpers.GetFastModMultiplier((uint)num);
		int[] array = ArrayPool<int>.Shared.Rent(num + hashCodes.Length);
		Span<int> span = array.AsSpan(0, num);
		Span<int> span2 = array.AsSpan(num, hashCodes.Length);
		span.Fill(-1);
		for (int i = 0; i < hashCodes.Length; i++)
		{
			int index = (int)System.Collections.HashHelpers.FastMod((uint)hashCodes[i], (uint)span.Length, fastModMultiplier);
			ref int reference = ref span[index];
			span2[i] = reference;
			reference = i;
		}
		int[] array2 = new int[hashCodes.Length];
		Bucket[] array3 = new Bucket[span.Length];
		int num2 = 0;
		for (int j = 0; j < array3.Length; j++)
		{
			int num3 = span[j];
			if (num3 >= 0)
			{
				int num4 = 0;
				int num5 = num3;
				num3 = num2;
				while (num5 >= 0)
				{
					ref int reference2 = ref hashCodes[num5];
					array2[num2] = reference2;
					reference2 = num2;
					num2++;
					num4++;
					num5 = span2[num5];
				}
				array3[j] = new Bucket(num3, num4);
			}
		}
		ArrayPool<int>.Shared.Return(array);
		return new FrozenHashTable(array2, array3, fastModMultiplier);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void FindMatchingEntries(int hashCode, out int startIndex, out int endIndex)
	{
		Bucket[] buckets = _buckets;
		ref Bucket reference = ref buckets[System.Collections.HashHelpers.FastMod((uint)hashCode, (uint)buckets.Length, _fastModMultiplier)];
		startIndex = reference.StartIndex;
		endIndex = reference.EndIndex;
	}

	private static int CalcNumBuckets(ReadOnlySpan<int> hashCodes, bool hashCodesAreUnique)
	{
		HashSet<int> hashSet = null;
		int num = hashCodes.Length;
		if (!hashCodesAreUnique)
		{
			hashSet = new HashSet<int>();
			ReadOnlySpan<int> readOnlySpan = hashCodes;
			for (int i = 0; i < readOnlySpan.Length; i++)
			{
				int item = readOnlySpan[i];
				hashSet.Add(item);
			}
			num = hashSet.Count;
		}
		int num2 = num * 2;
		ReadOnlySpan<int> primes = System.Collections.HashHelpers.Primes;
		int j;
		for (j = 0; (uint)j < (uint)primes.Length && num2 > primes[j]; j++)
		{
		}
		if (j >= primes.Length)
		{
			return System.Collections.HashHelpers.GetPrime(num);
		}
		int num3 = num * ((num >= 1000) ? 3 : 16);
		int k;
		for (k = j; (uint)k < (uint)primes.Length && num3 > primes[k]; k++)
		{
		}
		if (k < primes.Length)
		{
			num3 = primes[k - 1];
		}
		int[] seenBuckets = ArrayPool<int>.Shared.Rent(num3 / 32 + 1);
		int result = num3;
		int bestNumCollisions = num;
		int numBuckets = 0;
		int numCollisions = 0;
		for (int l = j; l < k; l++)
		{
			numBuckets = primes[l];
			Array.Clear(seenBuckets, 0, Math.Min(numBuckets, seenBuckets.Length));
			numCollisions = 0;
			if (hashSet != null && num != hashCodes.Length)
			{
				using HashSet<int>.Enumerator enumerator = hashSet.GetEnumerator();
				while (enumerator.MoveNext() && IsBucketFirstVisit(enumerator.Current))
				{
				}
			}
			else
			{
				ReadOnlySpan<int> readOnlySpan = hashCodes;
				for (int i = 0; i < readOnlySpan.Length && IsBucketFirstVisit(readOnlySpan[i]); i++)
				{
				}
			}
			if (numCollisions < bestNumCollisions)
			{
				result = numBuckets;
				if ((double)numCollisions / (double)num <= 0.05)
				{
					break;
				}
				bestNumCollisions = numCollisions;
			}
		}
		ArrayPool<int>.Shared.Return(seenBuckets);
		return result;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IsBucketFirstVisit(int code)
		{
			uint num4 = (uint)code % (uint)numBuckets;
			if ((seenBuckets[num4 / 32] & (1 << (int)num4)) != 0)
			{
				numCollisions++;
				if (numCollisions >= bestNumCollisions)
				{
					return false;
				}
			}
			else
			{
				seenBuckets[num4 / 32] |= 1 << (int)num4;
			}
			return true;
		}
	}
}
