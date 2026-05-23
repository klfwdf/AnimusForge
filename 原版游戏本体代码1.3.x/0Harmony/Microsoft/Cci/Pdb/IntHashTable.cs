using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000430 RID: 1072
	internal class IntHashTable
	{
		// Token: 0x06001779 RID: 6009 RVA: 0x000486F8 File Offset: 0x000468F8
		private static int GetPrime(int minSize)
		{
			if (minSize < 0)
			{
				throw new ArgumentException("Arg_HTCapacityOverflow");
			}
			for (int i = 0; i < IntHashTable.primes.Length; i++)
			{
				int size = IntHashTable.primes[i];
				if (size >= minSize)
				{
					return size;
				}
			}
			throw new ArgumentException("Arg_HTCapacityOverflow");
		}

		// Token: 0x0600177A RID: 6010 RVA: 0x0004873E File Offset: 0x0004693E
		internal IntHashTable()
			: this(0, 100)
		{
		}

		// Token: 0x0600177B RID: 6011 RVA: 0x0004874C File Offset: 0x0004694C
		internal IntHashTable(int capacity, int loadFactorPerc)
		{
			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException("capacity", "ArgumentOutOfRange_NeedNonNegNum");
			}
			if (loadFactorPerc < 10 || loadFactorPerc > 100)
			{
				throw new ArgumentOutOfRangeException("loadFactorPerc", string.Format("ArgumentOutOfRange_IntHashTableLoadFactor", 10, 100));
			}
			this.loadFactorPerc = loadFactorPerc * 72 / 100;
			int hashsize = IntHashTable.GetPrime(capacity / this.loadFactorPerc);
			this.buckets = new IntHashTable.bucket[hashsize];
			this.loadsize = this.loadFactorPerc * hashsize / 100;
			if (this.loadsize >= hashsize)
			{
				this.loadsize = hashsize - 1;
			}
		}

		// Token: 0x0600177C RID: 6012 RVA: 0x000487EC File Offset: 0x000469EC
		private static uint InitHash(int key, int hashsize, out uint seed, out uint incr)
		{
			uint hashcode = (uint)(key & int.MaxValue);
			seed = hashcode;
			incr = 1U + ((seed >> 5) + 1U) % (uint)(hashsize - 1);
			return hashcode;
		}

		// Token: 0x0600177D RID: 6013 RVA: 0x00048813 File Offset: 0x00046A13
		internal void Add(int key, object value)
		{
			this.Insert(key, value, true);
		}

		// Token: 0x17000591 RID: 1425
		internal object this[int key]
		{
			get
			{
				if (key < 0)
				{
					throw new ArgumentException("Argument_KeyLessThanZero");
				}
				IntHashTable.bucket[] lbuckets = this.buckets;
				uint seed;
				uint incr;
				uint hashcode = IntHashTable.InitHash(key, lbuckets.Length, out seed, out incr);
				int ntry = 0;
				IntHashTable.bucket b;
				for (;;)
				{
					int bucketNumber = (int)(seed % (uint)lbuckets.Length);
					b = lbuckets[bucketNumber];
					if (b.val == null)
					{
						break;
					}
					if ((long)(b.hash_coll & 2147483647) == (long)((ulong)hashcode) && key == b.key)
					{
						goto Block_4;
					}
					seed += incr;
					if (b.hash_coll >= 0 || ++ntry >= lbuckets.Length)
					{
						goto IL_81;
					}
				}
				return null;
				Block_4:
				return b.val;
				IL_81:
				return null;
			}
		}

		// Token: 0x0600177F RID: 6015 RVA: 0x000488AF File Offset: 0x00046AAF
		private void expand()
		{
			this.rehash(IntHashTable.GetPrime(1 + this.buckets.Length * 2));
		}

		// Token: 0x06001780 RID: 6016 RVA: 0x000488C8 File Offset: 0x00046AC8
		private void rehash()
		{
			this.rehash(this.buckets.Length);
		}

		// Token: 0x06001781 RID: 6017 RVA: 0x000488D8 File Offset: 0x00046AD8
		private void rehash(int newsize)
		{
			this.occupancy = 0;
			IntHashTable.bucket[] newBuckets = new IntHashTable.bucket[newsize];
			for (int nb = 0; nb < this.buckets.Length; nb++)
			{
				IntHashTable.bucket oldb = this.buckets[nb];
				if (oldb.val != null)
				{
					this.putEntry(newBuckets, oldb.key, oldb.val, oldb.hash_coll & int.MaxValue);
				}
			}
			this.version++;
			this.buckets = newBuckets;
			this.loadsize = this.loadFactorPerc * newsize / 100;
			if (this.loadsize >= newsize)
			{
				this.loadsize = newsize - 1;
			}
		}

		// Token: 0x06001782 RID: 6018 RVA: 0x00048974 File Offset: 0x00046B74
		private void Insert(int key, object nvalue, bool add)
		{
			if (key < 0)
			{
				throw new ArgumentException("Argument_KeyLessThanZero");
			}
			if (nvalue == null)
			{
				throw new ArgumentNullException("nvalue", "ArgumentNull_Value");
			}
			if (this.count >= this.loadsize)
			{
				this.expand();
			}
			else if (this.occupancy > this.loadsize && this.count > 100)
			{
				this.rehash();
			}
			uint seed;
			uint incr;
			uint hashcode = IntHashTable.InitHash(key, this.buckets.Length, out seed, out incr);
			int ntry = 0;
			int emptySlotNumber = -1;
			int bucketNumber;
			for (;;)
			{
				bucketNumber = (int)(seed % (uint)this.buckets.Length);
				if (this.buckets[bucketNumber].val == null)
				{
					break;
				}
				if ((long)(this.buckets[bucketNumber].hash_coll & 2147483647) == (long)((ulong)hashcode) && key == this.buckets[bucketNumber].key)
				{
					goto Block_9;
				}
				if (emptySlotNumber == -1 && this.buckets[bucketNumber].hash_coll >= 0)
				{
					IntHashTable.bucket[] array = this.buckets;
					int num = bucketNumber;
					array[num].hash_coll = array[num].hash_coll | int.MinValue;
					this.occupancy++;
				}
				seed += incr;
				if (++ntry >= this.buckets.Length)
				{
					goto Block_13;
				}
			}
			if (emptySlotNumber != -1)
			{
				bucketNumber = emptySlotNumber;
			}
			this.buckets[bucketNumber].val = nvalue;
			this.buckets[bucketNumber].key = key;
			IntHashTable.bucket[] array2 = this.buckets;
			int num2 = bucketNumber;
			array2[num2].hash_coll = array2[num2].hash_coll | (int)hashcode;
			this.count++;
			this.version++;
			return;
			Block_9:
			if (add)
			{
				throw new ArgumentException("Argument_AddingDuplicate__" + this.buckets[bucketNumber].key.ToString());
			}
			this.buckets[bucketNumber].val = nvalue;
			this.version++;
			return;
			Block_13:
			if (emptySlotNumber != -1)
			{
				this.buckets[emptySlotNumber].val = nvalue;
				this.buckets[emptySlotNumber].key = key;
				IntHashTable.bucket[] array3 = this.buckets;
				int num3 = emptySlotNumber;
				array3[num3].hash_coll = array3[num3].hash_coll | (int)hashcode;
				this.count++;
				this.version++;
				return;
			}
			throw new InvalidOperationException("InvalidOperation_HashInsertFailed");
		}

		// Token: 0x06001783 RID: 6019 RVA: 0x00048BB4 File Offset: 0x00046DB4
		private void putEntry(IntHashTable.bucket[] newBuckets, int key, object nvalue, int hashcode)
		{
			uint seed = (uint)hashcode;
			uint incr = 1U + ((seed >> 5) + 1U) % (uint)(newBuckets.Length - 1);
			int bucketNumber;
			for (;;)
			{
				bucketNumber = (int)(seed % (uint)newBuckets.Length);
				if (newBuckets[bucketNumber].val == null)
				{
					break;
				}
				if (newBuckets[bucketNumber].hash_coll >= 0)
				{
					int num = bucketNumber;
					newBuckets[num].hash_coll = newBuckets[num].hash_coll | int.MinValue;
					this.occupancy++;
				}
				seed += incr;
			}
			newBuckets[bucketNumber].val = nvalue;
			newBuckets[bucketNumber].key = key;
			int num2 = bucketNumber;
			newBuckets[num2].hash_coll = newBuckets[num2].hash_coll | hashcode;
		}

		// Token: 0x04000FF4 RID: 4084
		private static readonly int[] primes = new int[]
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

		// Token: 0x04000FF5 RID: 4085
		private IntHashTable.bucket[] buckets;

		// Token: 0x04000FF6 RID: 4086
		private int count;

		// Token: 0x04000FF7 RID: 4087
		private int occupancy;

		// Token: 0x04000FF8 RID: 4088
		private int loadsize;

		// Token: 0x04000FF9 RID: 4089
		private int loadFactorPerc;

		// Token: 0x04000FFA RID: 4090
		private int version;

		// Token: 0x02000431 RID: 1073
		private struct bucket
		{
			// Token: 0x04000FFB RID: 4091
			internal int key;

			// Token: 0x04000FFC RID: 4092
			internal int hash_coll;

			// Token: 0x04000FFD RID: 4093
			internal object val;
		}
	}
}
