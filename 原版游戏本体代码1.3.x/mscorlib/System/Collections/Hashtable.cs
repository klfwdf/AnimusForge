using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Collections
{
	// Token: 0x02000497 RID: 1175
	[DebuggerTypeProxy(typeof(Hashtable.HashtableDebugView))]
	[DebuggerDisplay("Count = {Count}")]
	[ComVisible(true)]
	[Serializable]
	public class Hashtable : IDictionary, ICollection, IEnumerable, ISerializable, IDeserializationCallback, ICloneable
	{
		// Token: 0x1700085F RID: 2143
		// (get) Token: 0x06003860 RID: 14432 RVA: 0x000D84F2 File Offset: 0x000D66F2
		// (set) Token: 0x06003861 RID: 14433 RVA: 0x000D852C File Offset: 0x000D672C
		[Obsolete("Please use EqualityComparer property.")]
		protected IHashCodeProvider hcp
		{
			get
			{
				if (this._keycomparer is CompatibleComparer)
				{
					return ((CompatibleComparer)this._keycomparer).HashCodeProvider;
				}
				if (this._keycomparer == null)
				{
					return null;
				}
				throw new ArgumentException(Environment.GetResourceString("Arg_CannotMixComparisonInfrastructure"));
			}
			set
			{
				if (this._keycomparer is CompatibleComparer)
				{
					CompatibleComparer compatibleComparer = (CompatibleComparer)this._keycomparer;
					this._keycomparer = new CompatibleComparer(compatibleComparer.Comparer, value);
					return;
				}
				if (this._keycomparer == null)
				{
					this._keycomparer = new CompatibleComparer(null, value);
					return;
				}
				throw new ArgumentException(Environment.GetResourceString("Arg_CannotMixComparisonInfrastructure"));
			}
		}

		// Token: 0x17000860 RID: 2144
		// (get) Token: 0x06003862 RID: 14434 RVA: 0x000D858A File Offset: 0x000D678A
		// (set) Token: 0x06003863 RID: 14435 RVA: 0x000D85C4 File Offset: 0x000D67C4
		[Obsolete("Please use KeyComparer properties.")]
		protected IComparer comparer
		{
			get
			{
				if (this._keycomparer is CompatibleComparer)
				{
					return ((CompatibleComparer)this._keycomparer).Comparer;
				}
				if (this._keycomparer == null)
				{
					return null;
				}
				throw new ArgumentException(Environment.GetResourceString("Arg_CannotMixComparisonInfrastructure"));
			}
			set
			{
				if (this._keycomparer is CompatibleComparer)
				{
					CompatibleComparer compatibleComparer = (CompatibleComparer)this._keycomparer;
					this._keycomparer = new CompatibleComparer(value, compatibleComparer.HashCodeProvider);
					return;
				}
				if (this._keycomparer == null)
				{
					this._keycomparer = new CompatibleComparer(value, null);
					return;
				}
				throw new ArgumentException(Environment.GetResourceString("Arg_CannotMixComparisonInfrastructure"));
			}
		}

		// Token: 0x17000861 RID: 2145
		// (get) Token: 0x06003864 RID: 14436 RVA: 0x000D8622 File Offset: 0x000D6822
		protected IEqualityComparer EqualityComparer
		{
			get
			{
				return this._keycomparer;
			}
		}

		// Token: 0x06003865 RID: 14437 RVA: 0x000D862A File Offset: 0x000D682A
		internal Hashtable(bool trash)
		{
		}

		// Token: 0x06003866 RID: 14438 RVA: 0x000D8632 File Offset: 0x000D6832
		public Hashtable()
			: this(0, 1f)
		{
		}

		// Token: 0x06003867 RID: 14439 RVA: 0x000D8640 File Offset: 0x000D6840
		public Hashtable(int capacity)
			: this(capacity, 1f)
		{
		}

		// Token: 0x06003868 RID: 14440 RVA: 0x000D8650 File Offset: 0x000D6850
		public Hashtable(int capacity, float loadFactor)
		{
			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (loadFactor < 0.1f || loadFactor > 1f)
			{
				throw new ArgumentOutOfRangeException("loadFactor", Environment.GetResourceString("ArgumentOutOfRange_HashtableLoadFactor", new object[] { 0.1, 1.0 }));
			}
			this.loadFactor = 0.72f * loadFactor;
			double num = (double)((float)capacity / this.loadFactor);
			if (num > 2147483647.0)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_HTCapacityOverflow"));
			}
			int num2 = ((num > 3.0) ? HashHelpers.GetPrime((int)num) : 3);
			this.buckets = new Hashtable.bucket[num2];
			this.loadsize = (int)(this.loadFactor * (float)num2);
			this.isWriterInProgress = false;
		}

		// Token: 0x06003869 RID: 14441 RVA: 0x000D8739 File Offset: 0x000D6939
		[Obsolete("Please use Hashtable(int, float, IEqualityComparer) instead.")]
		public Hashtable(int capacity, float loadFactor, IHashCodeProvider hcp, IComparer comparer)
			: this(capacity, loadFactor)
		{
			if (hcp == null && comparer == null)
			{
				this._keycomparer = null;
				return;
			}
			this._keycomparer = new CompatibleComparer(comparer, hcp);
		}

		// Token: 0x0600386A RID: 14442 RVA: 0x000D8760 File Offset: 0x000D6960
		public Hashtable(int capacity, float loadFactor, IEqualityComparer equalityComparer)
			: this(capacity, loadFactor)
		{
			this._keycomparer = equalityComparer;
		}

		// Token: 0x0600386B RID: 14443 RVA: 0x000D8771 File Offset: 0x000D6971
		[Obsolete("Please use Hashtable(IEqualityComparer) instead.")]
		public Hashtable(IHashCodeProvider hcp, IComparer comparer)
			: this(0, 1f, hcp, comparer)
		{
		}

		// Token: 0x0600386C RID: 14444 RVA: 0x000D8781 File Offset: 0x000D6981
		public Hashtable(IEqualityComparer equalityComparer)
			: this(0, 1f, equalityComparer)
		{
		}

		// Token: 0x0600386D RID: 14445 RVA: 0x000D8790 File Offset: 0x000D6990
		[Obsolete("Please use Hashtable(int, IEqualityComparer) instead.")]
		public Hashtable(int capacity, IHashCodeProvider hcp, IComparer comparer)
			: this(capacity, 1f, hcp, comparer)
		{
		}

		// Token: 0x0600386E RID: 14446 RVA: 0x000D87A0 File Offset: 0x000D69A0
		public Hashtable(int capacity, IEqualityComparer equalityComparer)
			: this(capacity, 1f, equalityComparer)
		{
		}

		// Token: 0x0600386F RID: 14447 RVA: 0x000D87AF File Offset: 0x000D69AF
		public Hashtable(IDictionary d)
			: this(d, 1f)
		{
		}

		// Token: 0x06003870 RID: 14448 RVA: 0x000D87BD File Offset: 0x000D69BD
		public Hashtable(IDictionary d, float loadFactor)
			: this(d, loadFactor, null)
		{
		}

		// Token: 0x06003871 RID: 14449 RVA: 0x000D87C8 File Offset: 0x000D69C8
		[Obsolete("Please use Hashtable(IDictionary, IEqualityComparer) instead.")]
		public Hashtable(IDictionary d, IHashCodeProvider hcp, IComparer comparer)
			: this(d, 1f, hcp, comparer)
		{
		}

		// Token: 0x06003872 RID: 14450 RVA: 0x000D87D8 File Offset: 0x000D69D8
		public Hashtable(IDictionary d, IEqualityComparer equalityComparer)
			: this(d, 1f, equalityComparer)
		{
		}

		// Token: 0x06003873 RID: 14451 RVA: 0x000D87E8 File Offset: 0x000D69E8
		[Obsolete("Please use Hashtable(IDictionary, float, IEqualityComparer) instead.")]
		public Hashtable(IDictionary d, float loadFactor, IHashCodeProvider hcp, IComparer comparer)
			: this((d != null) ? d.Count : 0, loadFactor, hcp, comparer)
		{
			if (d == null)
			{
				throw new ArgumentNullException("d", Environment.GetResourceString("ArgumentNull_Dictionary"));
			}
			IDictionaryEnumerator enumerator = d.GetEnumerator();
			while (enumerator.MoveNext())
			{
				this.Add(enumerator.Key, enumerator.Value);
			}
		}

		// Token: 0x06003874 RID: 14452 RVA: 0x000D8848 File Offset: 0x000D6A48
		public Hashtable(IDictionary d, float loadFactor, IEqualityComparer equalityComparer)
			: this((d != null) ? d.Count : 0, loadFactor, equalityComparer)
		{
			if (d == null)
			{
				throw new ArgumentNullException("d", Environment.GetResourceString("ArgumentNull_Dictionary"));
			}
			IDictionaryEnumerator enumerator = d.GetEnumerator();
			while (enumerator.MoveNext())
			{
				this.Add(enumerator.Key, enumerator.Value);
			}
		}

		// Token: 0x06003875 RID: 14453 RVA: 0x000D88A4 File Offset: 0x000D6AA4
		protected Hashtable(SerializationInfo info, StreamingContext context)
		{
			HashHelpers.SerializationInfoTable.Add(this, info);
		}

		// Token: 0x06003876 RID: 14454 RVA: 0x000D88B8 File Offset: 0x000D6AB8
		private uint InitHash(object key, int hashsize, out uint seed, out uint incr)
		{
			uint num = (uint)(this.GetHash(key) & int.MaxValue);
			seed = num;
			incr = 1U + seed * 101U % (uint)(hashsize - 1);
			return num;
		}

		// Token: 0x06003877 RID: 14455 RVA: 0x000D88E5 File Offset: 0x000D6AE5
		public virtual void Add(object key, object value)
		{
			this.Insert(key, value, true);
		}

		// Token: 0x06003878 RID: 14456 RVA: 0x000D88F0 File Offset: 0x000D6AF0
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public virtual void Clear()
		{
			if (this.count == 0 && this.occupancy == 0)
			{
				return;
			}
			Thread.BeginCriticalRegion();
			this.isWriterInProgress = true;
			for (int i = 0; i < this.buckets.Length; i++)
			{
				this.buckets[i].hash_coll = 0;
				this.buckets[i].key = null;
				this.buckets[i].val = null;
			}
			this.count = 0;
			this.occupancy = 0;
			this.UpdateVersion();
			this.isWriterInProgress = false;
			Thread.EndCriticalRegion();
		}

		// Token: 0x06003879 RID: 14457 RVA: 0x000D8988 File Offset: 0x000D6B88
		public virtual object Clone()
		{
			Hashtable.bucket[] array = this.buckets;
			Hashtable hashtable = new Hashtable(this.count, this._keycomparer);
			hashtable.version = this.version;
			hashtable.loadFactor = this.loadFactor;
			hashtable.count = 0;
			int i = array.Length;
			while (i > 0)
			{
				i--;
				object key = array[i].key;
				if (key != null && key != array)
				{
					hashtable[key] = array[i].val;
				}
			}
			return hashtable;
		}

		// Token: 0x0600387A RID: 14458 RVA: 0x000D8A07 File Offset: 0x000D6C07
		public virtual bool Contains(object key)
		{
			return this.ContainsKey(key);
		}

		// Token: 0x0600387B RID: 14459 RVA: 0x000D8A10 File Offset: 0x000D6C10
		public virtual bool ContainsKey(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
			}
			Hashtable.bucket[] array = this.buckets;
			uint num2;
			uint num3;
			uint num = this.InitHash(key, array.Length, out num2, out num3);
			int num4 = 0;
			int num5 = (int)(num2 % (uint)array.Length);
			for (;;)
			{
				Hashtable.bucket bucket = array[num5];
				if (bucket.key == null)
				{
					break;
				}
				if ((long)(bucket.hash_coll & 2147483647) == (long)((ulong)num) && this.KeyEquals(bucket.key, key))
				{
					return true;
				}
				num5 = (int)(((long)num5 + (long)((ulong)num3)) % (long)((ulong)array.Length));
				if (bucket.hash_coll >= 0 || ++num4 >= array.Length)
				{
					return false;
				}
			}
			return false;
		}

		// Token: 0x0600387C RID: 14460 RVA: 0x000D8AB4 File Offset: 0x000D6CB4
		public virtual bool ContainsValue(object value)
		{
			if (value == null)
			{
				int num = this.buckets.Length;
				while (--num >= 0)
				{
					if (this.buckets[num].key != null && this.buckets[num].key != this.buckets && this.buckets[num].val == null)
					{
						return true;
					}
				}
			}
			else
			{
				int num2 = this.buckets.Length;
				while (--num2 >= 0)
				{
					object val = this.buckets[num2].val;
					if (val != null && val.Equals(value))
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x0600387D RID: 14461 RVA: 0x000D8B50 File Offset: 0x000D6D50
		private void CopyKeys(Array array, int arrayIndex)
		{
			Hashtable.bucket[] array2 = this.buckets;
			int num = array2.Length;
			while (--num >= 0)
			{
				object key = array2[num].key;
				if (key != null && key != this.buckets)
				{
					array.SetValue(key, arrayIndex++);
				}
			}
		}

		// Token: 0x0600387E RID: 14462 RVA: 0x000D8B98 File Offset: 0x000D6D98
		private void CopyEntries(Array array, int arrayIndex)
		{
			Hashtable.bucket[] array2 = this.buckets;
			int num = array2.Length;
			while (--num >= 0)
			{
				object key = array2[num].key;
				if (key != null && key != this.buckets)
				{
					DictionaryEntry dictionaryEntry = new DictionaryEntry(key, array2[num].val);
					array.SetValue(dictionaryEntry, arrayIndex++);
				}
			}
		}

		// Token: 0x0600387F RID: 14463 RVA: 0x000D8BFC File Offset: 0x000D6DFC
		public virtual void CopyTo(Array array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array", Environment.GetResourceString("ArgumentNull_Array"));
			}
			if (array.Rank != 1)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
			}
			if (arrayIndex < 0)
			{
				throw new ArgumentOutOfRangeException("arrayIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (array.Length - arrayIndex < this.Count)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_ArrayPlusOffTooSmall"));
			}
			this.CopyEntries(array, arrayIndex);
		}

		// Token: 0x06003880 RID: 14464 RVA: 0x000D8C7C File Offset: 0x000D6E7C
		internal virtual KeyValuePairs[] ToKeyValuePairsArray()
		{
			KeyValuePairs[] array = new KeyValuePairs[this.count];
			int num = 0;
			Hashtable.bucket[] array2 = this.buckets;
			int num2 = array2.Length;
			while (--num2 >= 0)
			{
				object key = array2[num2].key;
				if (key != null && key != this.buckets)
				{
					array[num++] = new KeyValuePairs(key, array2[num2].val);
				}
			}
			return array;
		}

		// Token: 0x06003881 RID: 14465 RVA: 0x000D8CE4 File Offset: 0x000D6EE4
		private void CopyValues(Array array, int arrayIndex)
		{
			Hashtable.bucket[] array2 = this.buckets;
			int num = array2.Length;
			while (--num >= 0)
			{
				object key = array2[num].key;
				if (key != null && key != this.buckets)
				{
					array.SetValue(array2[num].val, arrayIndex++);
				}
			}
		}

		// Token: 0x17000862 RID: 2146
		public virtual object this[object key]
		{
			get
			{
				if (key == null)
				{
					throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
				}
				Hashtable.bucket[] array = this.buckets;
				uint num2;
				uint num3;
				uint num = this.InitHash(key, array.Length, out num2, out num3);
				int num4 = 0;
				int num5 = (int)(num2 % (uint)array.Length);
				Hashtable.bucket bucket;
				for (;;)
				{
					int num6 = 0;
					int num7;
					do
					{
						num7 = this.version;
						bucket = array[num5];
						if (++num6 % 8 == 0)
						{
							Thread.Sleep(1);
						}
					}
					while (this.isWriterInProgress || num7 != this.version);
					if (bucket.key == null)
					{
						break;
					}
					if ((long)(bucket.hash_coll & 2147483647) == (long)((ulong)num) && this.KeyEquals(bucket.key, key))
					{
						goto Block_7;
					}
					num5 = (int)(((long)num5 + (long)((ulong)num3)) % (long)((ulong)array.Length));
					if (bucket.hash_coll >= 0 || ++num4 >= array.Length)
					{
						goto IL_D2;
					}
				}
				return null;
				Block_7:
				return bucket.val;
				IL_D2:
				return null;
			}
			set
			{
				this.Insert(key, value, false);
			}
		}

		// Token: 0x06003884 RID: 14468 RVA: 0x000D8E24 File Offset: 0x000D7024
		private void expand()
		{
			int newsize = HashHelpers.ExpandPrime(this.buckets.Length);
			this.rehash(newsize, false);
		}

		// Token: 0x06003885 RID: 14469 RVA: 0x000D8E47 File Offset: 0x000D7047
		private void rehash()
		{
			this.rehash(this.buckets.Length, false);
		}

		// Token: 0x06003886 RID: 14470 RVA: 0x000D8E58 File Offset: 0x000D7058
		private void UpdateVersion()
		{
			this.version++;
		}

		// Token: 0x06003887 RID: 14471 RVA: 0x000D8E6C File Offset: 0x000D706C
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		private void rehash(int newsize, bool forceNewHashCode)
		{
			this.occupancy = 0;
			Hashtable.bucket[] newBuckets = new Hashtable.bucket[newsize];
			for (int i = 0; i < this.buckets.Length; i++)
			{
				Hashtable.bucket bucket = this.buckets[i];
				if (bucket.key != null && bucket.key != this.buckets)
				{
					int hashcode = (forceNewHashCode ? this.GetHash(bucket.key) : bucket.hash_coll) & int.MaxValue;
					this.putEntry(newBuckets, bucket.key, bucket.val, hashcode);
				}
			}
			Thread.BeginCriticalRegion();
			this.isWriterInProgress = true;
			this.buckets = newBuckets;
			this.loadsize = (int)(this.loadFactor * (float)newsize);
			this.UpdateVersion();
			this.isWriterInProgress = false;
			Thread.EndCriticalRegion();
		}

		// Token: 0x06003888 RID: 14472 RVA: 0x000D8F28 File Offset: 0x000D7128
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Hashtable.HashtableEnumerator(this, 3);
		}

		// Token: 0x06003889 RID: 14473 RVA: 0x000D8F31 File Offset: 0x000D7131
		public virtual IDictionaryEnumerator GetEnumerator()
		{
			return new Hashtable.HashtableEnumerator(this, 3);
		}

		// Token: 0x0600388A RID: 14474 RVA: 0x000D8F3A File Offset: 0x000D713A
		protected virtual int GetHash(object key)
		{
			if (this._keycomparer != null)
			{
				return this._keycomparer.GetHashCode(key);
			}
			return key.GetHashCode();
		}

		// Token: 0x17000863 RID: 2147
		// (get) Token: 0x0600388B RID: 14475 RVA: 0x000D8F57 File Offset: 0x000D7157
		public virtual bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000864 RID: 2148
		// (get) Token: 0x0600388C RID: 14476 RVA: 0x000D8F5A File Offset: 0x000D715A
		public virtual bool IsFixedSize
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000865 RID: 2149
		// (get) Token: 0x0600388D RID: 14477 RVA: 0x000D8F5D File Offset: 0x000D715D
		public virtual bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		// Token: 0x0600388E RID: 14478 RVA: 0x000D8F60 File Offset: 0x000D7160
		protected virtual bool KeyEquals(object item, object key)
		{
			if (this.buckets == item)
			{
				return false;
			}
			if (item == key)
			{
				return true;
			}
			if (this._keycomparer != null)
			{
				return this._keycomparer.Equals(item, key);
			}
			return item != null && item.Equals(key);
		}

		// Token: 0x17000866 RID: 2150
		// (get) Token: 0x0600388F RID: 14479 RVA: 0x000D8F95 File Offset: 0x000D7195
		public virtual ICollection Keys
		{
			get
			{
				if (this.keys == null)
				{
					this.keys = new Hashtable.KeyCollection(this);
				}
				return this.keys;
			}
		}

		// Token: 0x17000867 RID: 2151
		// (get) Token: 0x06003890 RID: 14480 RVA: 0x000D8FB1 File Offset: 0x000D71B1
		public virtual ICollection Values
		{
			get
			{
				if (this.values == null)
				{
					this.values = new Hashtable.ValueCollection(this);
				}
				return this.values;
			}
		}

		// Token: 0x06003891 RID: 14481 RVA: 0x000D8FD0 File Offset: 0x000D71D0
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		private void Insert(object key, object nvalue, bool add)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
			}
			if (this.count >= this.loadsize)
			{
				this.expand();
			}
			else if (this.occupancy > this.loadsize && this.count > 100)
			{
				this.rehash();
			}
			uint num2;
			uint num3;
			uint num = this.InitHash(key, this.buckets.Length, out num2, out num3);
			int num4 = 0;
			int num5 = -1;
			int num6 = (int)(num2 % (uint)this.buckets.Length);
			for (;;)
			{
				if (num5 == -1 && this.buckets[num6].key == this.buckets && this.buckets[num6].hash_coll < 0)
				{
					num5 = num6;
				}
				if (this.buckets[num6].key == null || (this.buckets[num6].key == this.buckets && ((long)this.buckets[num6].hash_coll & (long)((ulong)(-2147483648))) == 0L))
				{
					break;
				}
				if ((long)(this.buckets[num6].hash_coll & 2147483647) == (long)((ulong)num) && this.KeyEquals(this.buckets[num6].key, key))
				{
					goto Block_15;
				}
				if (num5 == -1 && this.buckets[num6].hash_coll >= 0)
				{
					Hashtable.bucket[] array = this.buckets;
					int num7 = num6;
					array[num7].hash_coll = array[num7].hash_coll | int.MinValue;
					this.occupancy++;
				}
				num6 = (int)(((long)num6 + (long)((ulong)num3)) % (long)((ulong)this.buckets.Length));
				if (++num4 >= this.buckets.Length)
				{
					goto Block_22;
				}
			}
			if (num5 != -1)
			{
				num6 = num5;
			}
			Thread.BeginCriticalRegion();
			this.isWriterInProgress = true;
			this.buckets[num6].val = nvalue;
			this.buckets[num6].key = key;
			Hashtable.bucket[] array2 = this.buckets;
			int num8 = num6;
			array2[num8].hash_coll = array2[num8].hash_coll | (int)num;
			this.count++;
			this.UpdateVersion();
			this.isWriterInProgress = false;
			Thread.EndCriticalRegion();
			if (num4 > 100 && HashHelpers.IsWellKnownEqualityComparer(this._keycomparer) && (this._keycomparer == null || !(this._keycomparer is RandomizedObjectEqualityComparer)))
			{
				this._keycomparer = HashHelpers.GetRandomizedEqualityComparer(this._keycomparer);
				this.rehash(this.buckets.Length, true);
			}
			return;
			Block_15:
			if (add)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_AddingDuplicate__", new object[]
				{
					this.buckets[num6].key,
					key
				}));
			}
			Thread.BeginCriticalRegion();
			this.isWriterInProgress = true;
			this.buckets[num6].val = nvalue;
			this.UpdateVersion();
			this.isWriterInProgress = false;
			Thread.EndCriticalRegion();
			if (num4 > 100 && HashHelpers.IsWellKnownEqualityComparer(this._keycomparer) && (this._keycomparer == null || !(this._keycomparer is RandomizedObjectEqualityComparer)))
			{
				this._keycomparer = HashHelpers.GetRandomizedEqualityComparer(this._keycomparer);
				this.rehash(this.buckets.Length, true);
			}
			return;
			Block_22:
			if (num5 != -1)
			{
				Thread.BeginCriticalRegion();
				this.isWriterInProgress = true;
				this.buckets[num5].val = nvalue;
				this.buckets[num5].key = key;
				Hashtable.bucket[] array3 = this.buckets;
				int num9 = num5;
				array3[num9].hash_coll = array3[num9].hash_coll | (int)num;
				this.count++;
				this.UpdateVersion();
				this.isWriterInProgress = false;
				Thread.EndCriticalRegion();
				if (this.buckets.Length > 100 && HashHelpers.IsWellKnownEqualityComparer(this._keycomparer) && (this._keycomparer == null || !(this._keycomparer is RandomizedObjectEqualityComparer)))
				{
					this._keycomparer = HashHelpers.GetRandomizedEqualityComparer(this._keycomparer);
					this.rehash(this.buckets.Length, true);
				}
				return;
			}
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_HashInsertFailed"));
		}

		// Token: 0x06003892 RID: 14482 RVA: 0x000D93BC File Offset: 0x000D75BC
		private void putEntry(Hashtable.bucket[] newBuckets, object key, object nvalue, int hashcode)
		{
			uint num = (uint)(1 + hashcode * 101 % (newBuckets.Length - 1));
			int num2 = hashcode % newBuckets.Length;
			while (newBuckets[num2].key != null && newBuckets[num2].key != this.buckets)
			{
				if (newBuckets[num2].hash_coll >= 0)
				{
					int num3 = num2;
					newBuckets[num3].hash_coll = newBuckets[num3].hash_coll | int.MinValue;
					this.occupancy++;
				}
				num2 = (int)(((long)num2 + (long)((ulong)num)) % (long)((ulong)newBuckets.Length));
			}
			newBuckets[num2].val = nvalue;
			newBuckets[num2].key = key;
			int num4 = num2;
			newBuckets[num4].hash_coll = newBuckets[num4].hash_coll | hashcode;
		}

		// Token: 0x06003893 RID: 14483 RVA: 0x000D9470 File Offset: 0x000D7670
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public virtual void Remove(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
			}
			uint num2;
			uint num3;
			uint num = this.InitHash(key, this.buckets.Length, out num2, out num3);
			int num4 = 0;
			int num5 = (int)(num2 % (uint)this.buckets.Length);
			for (;;)
			{
				Hashtable.bucket bucket = this.buckets[num5];
				if ((long)(bucket.hash_coll & 2147483647) == (long)((ulong)num) && this.KeyEquals(bucket.key, key))
				{
					break;
				}
				num5 = (int)(((long)num5 + (long)((ulong)num3)) % (long)((ulong)this.buckets.Length));
				if (bucket.hash_coll >= 0 || ++num4 >= this.buckets.Length)
				{
					return;
				}
			}
			Thread.BeginCriticalRegion();
			this.isWriterInProgress = true;
			Hashtable.bucket[] array = this.buckets;
			int num6 = num5;
			array[num6].hash_coll = array[num6].hash_coll & int.MinValue;
			if (this.buckets[num5].hash_coll != 0)
			{
				this.buckets[num5].key = this.buckets;
			}
			else
			{
				this.buckets[num5].key = null;
			}
			this.buckets[num5].val = null;
			this.count--;
			this.UpdateVersion();
			this.isWriterInProgress = false;
			Thread.EndCriticalRegion();
		}

		// Token: 0x17000868 RID: 2152
		// (get) Token: 0x06003894 RID: 14484 RVA: 0x000D95BD File Offset: 0x000D77BD
		public virtual object SyncRoot
		{
			get
			{
				if (this._syncRoot == null)
				{
					Interlocked.CompareExchange<object>(ref this._syncRoot, new object(), null);
				}
				return this._syncRoot;
			}
		}

		// Token: 0x17000869 RID: 2153
		// (get) Token: 0x06003895 RID: 14485 RVA: 0x000D95DF File Offset: 0x000D77DF
		public virtual int Count
		{
			get
			{
				return this.count;
			}
		}

		// Token: 0x06003896 RID: 14486 RVA: 0x000D95E7 File Offset: 0x000D77E7
		[HostProtection(SecurityAction.LinkDemand, Synchronization = true)]
		public static Hashtable Synchronized(Hashtable table)
		{
			if (table == null)
			{
				throw new ArgumentNullException("table");
			}
			return new Hashtable.SyncHashtable(table);
		}

		// Token: 0x06003897 RID: 14487 RVA: 0x000D9600 File Offset: 0x000D7800
		[SecurityCritical]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			object syncRoot = this.SyncRoot;
			lock (syncRoot)
			{
				int num = this.version;
				info.AddValue("LoadFactor", this.loadFactor);
				info.AddValue("Version", this.version);
				IEqualityComparer equalityComparer = (IEqualityComparer)HashHelpers.GetEqualityComparerForSerialization(this._keycomparer);
				if (equalityComparer == null)
				{
					info.AddValue("Comparer", null, typeof(IComparer));
					info.AddValue("HashCodeProvider", null, typeof(IHashCodeProvider));
				}
				else if (equalityComparer is CompatibleComparer)
				{
					CompatibleComparer compatibleComparer = equalityComparer as CompatibleComparer;
					info.AddValue("Comparer", compatibleComparer.Comparer, typeof(IComparer));
					info.AddValue("HashCodeProvider", compatibleComparer.HashCodeProvider, typeof(IHashCodeProvider));
				}
				else
				{
					info.AddValue("KeyComparer", equalityComparer, typeof(IEqualityComparer));
				}
				info.AddValue("HashSize", this.buckets.Length);
				object[] array = new object[this.count];
				object[] array2 = new object[this.count];
				this.CopyKeys(array, 0);
				this.CopyValues(array2, 0);
				info.AddValue("Keys", array, typeof(object[]));
				info.AddValue("Values", array2, typeof(object[]));
				if (this.version != num)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
				}
			}
		}

		// Token: 0x06003898 RID: 14488 RVA: 0x000D97AC File Offset: 0x000D79AC
		public virtual void OnDeserialization(object sender)
		{
			if (this.buckets != null)
			{
				return;
			}
			SerializationInfo serializationInfo;
			HashHelpers.SerializationInfoTable.TryGetValue(this, out serializationInfo);
			if (serializationInfo == null)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_InvalidOnDeser"));
			}
			int num = 0;
			IComparer comparer = null;
			IHashCodeProvider hashCodeProvider = null;
			object[] array = null;
			object[] array2 = null;
			SerializationInfoEnumerator enumerator = serializationInfo.GetEnumerator();
			while (enumerator.MoveNext())
			{
				string name = enumerator.Name;
				uint num2 = <PrivateImplementationDetails>.ComputeStringHash(name);
				if (num2 <= 1613443821U)
				{
					if (num2 != 891156946U)
					{
						if (num2 != 1228509323U)
						{
							if (num2 == 1613443821U)
							{
								if (name == "Keys")
								{
									array = (object[])serializationInfo.GetValue("Keys", typeof(object[]));
								}
							}
						}
						else if (name == "KeyComparer")
						{
							this._keycomparer = (IEqualityComparer)serializationInfo.GetValue("KeyComparer", typeof(IEqualityComparer));
						}
					}
					else if (name == "Comparer")
					{
						comparer = (IComparer)serializationInfo.GetValue("Comparer", typeof(IComparer));
					}
				}
				else if (num2 <= 2484309429U)
				{
					if (num2 != 2370642523U)
					{
						if (num2 == 2484309429U)
						{
							if (name == "HashCodeProvider")
							{
								hashCodeProvider = (IHashCodeProvider)serializationInfo.GetValue("HashCodeProvider", typeof(IHashCodeProvider));
							}
						}
					}
					else if (name == "Values")
					{
						array2 = (object[])serializationInfo.GetValue("Values", typeof(object[]));
					}
				}
				else if (num2 != 3356145248U)
				{
					if (num2 == 3483216242U)
					{
						if (name == "LoadFactor")
						{
							this.loadFactor = serializationInfo.GetSingle("LoadFactor");
						}
					}
				}
				else if (name == "HashSize")
				{
					num = serializationInfo.GetInt32("HashSize");
				}
			}
			this.loadsize = (int)(this.loadFactor * (float)num);
			if (this._keycomparer == null && (comparer != null || hashCodeProvider != null))
			{
				this._keycomparer = new CompatibleComparer(comparer, hashCodeProvider);
			}
			this.buckets = new Hashtable.bucket[num];
			if (array == null)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_MissingKeys"));
			}
			if (array2 == null)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_MissingValues"));
			}
			if (array.Length != array2.Length)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_KeyValueDifferentSizes"));
			}
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] == null)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_NullKey"));
				}
				this.Insert(array[i], array2[i], true);
			}
			this.version = serializationInfo.GetInt32("Version");
			HashHelpers.SerializationInfoTable.Remove(this);
		}

		// Token: 0x040018E2 RID: 6370
		internal const int HashPrime = 101;

		// Token: 0x040018E3 RID: 6371
		private const int InitialSize = 3;

		// Token: 0x040018E4 RID: 6372
		private const string LoadFactorName = "LoadFactor";

		// Token: 0x040018E5 RID: 6373
		private const string VersionName = "Version";

		// Token: 0x040018E6 RID: 6374
		private const string ComparerName = "Comparer";

		// Token: 0x040018E7 RID: 6375
		private const string HashCodeProviderName = "HashCodeProvider";

		// Token: 0x040018E8 RID: 6376
		private const string HashSizeName = "HashSize";

		// Token: 0x040018E9 RID: 6377
		private const string KeysName = "Keys";

		// Token: 0x040018EA RID: 6378
		private const string ValuesName = "Values";

		// Token: 0x040018EB RID: 6379
		private const string KeyComparerName = "KeyComparer";

		// Token: 0x040018EC RID: 6380
		private Hashtable.bucket[] buckets;

		// Token: 0x040018ED RID: 6381
		private int count;

		// Token: 0x040018EE RID: 6382
		private int occupancy;

		// Token: 0x040018EF RID: 6383
		private int loadsize;

		// Token: 0x040018F0 RID: 6384
		private float loadFactor;

		// Token: 0x040018F1 RID: 6385
		private volatile int version;

		// Token: 0x040018F2 RID: 6386
		private volatile bool isWriterInProgress;

		// Token: 0x040018F3 RID: 6387
		private ICollection keys;

		// Token: 0x040018F4 RID: 6388
		private ICollection values;

		// Token: 0x040018F5 RID: 6389
		private IEqualityComparer _keycomparer;

		// Token: 0x040018F6 RID: 6390
		private object _syncRoot;

		// Token: 0x02000BB7 RID: 2999
		private struct bucket
		{
			// Token: 0x04003571 RID: 13681
			public object key;

			// Token: 0x04003572 RID: 13682
			public object val;

			// Token: 0x04003573 RID: 13683
			public int hash_coll;
		}

		// Token: 0x02000BB8 RID: 3000
		[Serializable]
		private class KeyCollection : ICollection, IEnumerable
		{
			// Token: 0x06006DF5 RID: 28149 RVA: 0x0017BD10 File Offset: 0x00179F10
			internal KeyCollection(Hashtable hashtable)
			{
				this._hashtable = hashtable;
			}

			// Token: 0x06006DF6 RID: 28150 RVA: 0x0017BD20 File Offset: 0x00179F20
			public virtual void CopyTo(Array array, int arrayIndex)
			{
				if (array == null)
				{
					throw new ArgumentNullException("array");
				}
				if (array.Rank != 1)
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
				}
				if (arrayIndex < 0)
				{
					throw new ArgumentOutOfRangeException("arrayIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (array.Length - arrayIndex < this._hashtable.count)
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_ArrayPlusOffTooSmall"));
				}
				this._hashtable.CopyKeys(array, arrayIndex);
			}

			// Token: 0x06006DF7 RID: 28151 RVA: 0x0017BD9F File Offset: 0x00179F9F
			public virtual IEnumerator GetEnumerator()
			{
				return new Hashtable.HashtableEnumerator(this._hashtable, 1);
			}

			// Token: 0x170012B0 RID: 4784
			// (get) Token: 0x06006DF8 RID: 28152 RVA: 0x0017BDAD File Offset: 0x00179FAD
			public virtual bool IsSynchronized
			{
				get
				{
					return this._hashtable.IsSynchronized;
				}
			}

			// Token: 0x170012B1 RID: 4785
			// (get) Token: 0x06006DF9 RID: 28153 RVA: 0x0017BDBA File Offset: 0x00179FBA
			public virtual object SyncRoot
			{
				get
				{
					return this._hashtable.SyncRoot;
				}
			}

			// Token: 0x170012B2 RID: 4786
			// (get) Token: 0x06006DFA RID: 28154 RVA: 0x0017BDC7 File Offset: 0x00179FC7
			public virtual int Count
			{
				get
				{
					return this._hashtable.count;
				}
			}

			// Token: 0x04003574 RID: 13684
			private Hashtable _hashtable;
		}

		// Token: 0x02000BB9 RID: 3001
		[Serializable]
		private class ValueCollection : ICollection, IEnumerable
		{
			// Token: 0x06006DFB RID: 28155 RVA: 0x0017BDD4 File Offset: 0x00179FD4
			internal ValueCollection(Hashtable hashtable)
			{
				this._hashtable = hashtable;
			}

			// Token: 0x06006DFC RID: 28156 RVA: 0x0017BDE4 File Offset: 0x00179FE4
			public virtual void CopyTo(Array array, int arrayIndex)
			{
				if (array == null)
				{
					throw new ArgumentNullException("array");
				}
				if (array.Rank != 1)
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
				}
				if (arrayIndex < 0)
				{
					throw new ArgumentOutOfRangeException("arrayIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (array.Length - arrayIndex < this._hashtable.count)
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_ArrayPlusOffTooSmall"));
				}
				this._hashtable.CopyValues(array, arrayIndex);
			}

			// Token: 0x06006DFD RID: 28157 RVA: 0x0017BE63 File Offset: 0x0017A063
			public virtual IEnumerator GetEnumerator()
			{
				return new Hashtable.HashtableEnumerator(this._hashtable, 2);
			}

			// Token: 0x170012B3 RID: 4787
			// (get) Token: 0x06006DFE RID: 28158 RVA: 0x0017BE71 File Offset: 0x0017A071
			public virtual bool IsSynchronized
			{
				get
				{
					return this._hashtable.IsSynchronized;
				}
			}

			// Token: 0x170012B4 RID: 4788
			// (get) Token: 0x06006DFF RID: 28159 RVA: 0x0017BE7E File Offset: 0x0017A07E
			public virtual object SyncRoot
			{
				get
				{
					return this._hashtable.SyncRoot;
				}
			}

			// Token: 0x170012B5 RID: 4789
			// (get) Token: 0x06006E00 RID: 28160 RVA: 0x0017BE8B File Offset: 0x0017A08B
			public virtual int Count
			{
				get
				{
					return this._hashtable.count;
				}
			}

			// Token: 0x04003575 RID: 13685
			private Hashtable _hashtable;
		}

		// Token: 0x02000BBA RID: 3002
		[Serializable]
		private class SyncHashtable : Hashtable, IEnumerable
		{
			// Token: 0x06006E01 RID: 28161 RVA: 0x0017BE98 File Offset: 0x0017A098
			internal SyncHashtable(Hashtable table)
				: base(false)
			{
				this._table = table;
			}

			// Token: 0x06006E02 RID: 28162 RVA: 0x0017BEA8 File Offset: 0x0017A0A8
			internal SyncHashtable(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
				this._table = (Hashtable)info.GetValue("ParentTable", typeof(Hashtable));
				if (this._table == null)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientState"));
				}
			}

			// Token: 0x06006E03 RID: 28163 RVA: 0x0017BEF8 File Offset: 0x0017A0F8
			[SecurityCritical]
			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				if (info == null)
				{
					throw new ArgumentNullException("info");
				}
				object syncRoot = this._table.SyncRoot;
				lock (syncRoot)
				{
					info.AddValue("ParentTable", this._table, typeof(Hashtable));
				}
			}

			// Token: 0x170012B6 RID: 4790
			// (get) Token: 0x06006E04 RID: 28164 RVA: 0x0017BF60 File Offset: 0x0017A160
			public override int Count
			{
				get
				{
					return this._table.Count;
				}
			}

			// Token: 0x170012B7 RID: 4791
			// (get) Token: 0x06006E05 RID: 28165 RVA: 0x0017BF6D File Offset: 0x0017A16D
			public override bool IsReadOnly
			{
				get
				{
					return this._table.IsReadOnly;
				}
			}

			// Token: 0x170012B8 RID: 4792
			// (get) Token: 0x06006E06 RID: 28166 RVA: 0x0017BF7A File Offset: 0x0017A17A
			public override bool IsFixedSize
			{
				get
				{
					return this._table.IsFixedSize;
				}
			}

			// Token: 0x170012B9 RID: 4793
			// (get) Token: 0x06006E07 RID: 28167 RVA: 0x0017BF87 File Offset: 0x0017A187
			public override bool IsSynchronized
			{
				get
				{
					return true;
				}
			}

			// Token: 0x170012BA RID: 4794
			public override object this[object key]
			{
				get
				{
					return this._table[key];
				}
				set
				{
					object syncRoot = this._table.SyncRoot;
					lock (syncRoot)
					{
						this._table[key] = value;
					}
				}
			}

			// Token: 0x170012BB RID: 4795
			// (get) Token: 0x06006E0A RID: 28170 RVA: 0x0017BFE4 File Offset: 0x0017A1E4
			public override object SyncRoot
			{
				get
				{
					return this._table.SyncRoot;
				}
			}

			// Token: 0x06006E0B RID: 28171 RVA: 0x0017BFF4 File Offset: 0x0017A1F4
			public override void Add(object key, object value)
			{
				object syncRoot = this._table.SyncRoot;
				lock (syncRoot)
				{
					this._table.Add(key, value);
				}
			}

			// Token: 0x06006E0C RID: 28172 RVA: 0x0017C040 File Offset: 0x0017A240
			public override void Clear()
			{
				object syncRoot = this._table.SyncRoot;
				lock (syncRoot)
				{
					this._table.Clear();
				}
			}

			// Token: 0x06006E0D RID: 28173 RVA: 0x0017C08C File Offset: 0x0017A28C
			public override bool Contains(object key)
			{
				return this._table.Contains(key);
			}

			// Token: 0x06006E0E RID: 28174 RVA: 0x0017C09A File Offset: 0x0017A29A
			public override bool ContainsKey(object key)
			{
				if (key == null)
				{
					throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
				}
				return this._table.ContainsKey(key);
			}

			// Token: 0x06006E0F RID: 28175 RVA: 0x0017C0C0 File Offset: 0x0017A2C0
			public override bool ContainsValue(object key)
			{
				object syncRoot = this._table.SyncRoot;
				bool result;
				lock (syncRoot)
				{
					result = this._table.ContainsValue(key);
				}
				return result;
			}

			// Token: 0x06006E10 RID: 28176 RVA: 0x0017C110 File Offset: 0x0017A310
			public override void CopyTo(Array array, int arrayIndex)
			{
				object syncRoot = this._table.SyncRoot;
				lock (syncRoot)
				{
					this._table.CopyTo(array, arrayIndex);
				}
			}

			// Token: 0x06006E11 RID: 28177 RVA: 0x0017C15C File Offset: 0x0017A35C
			public override object Clone()
			{
				object syncRoot = this._table.SyncRoot;
				object result;
				lock (syncRoot)
				{
					result = Hashtable.Synchronized((Hashtable)this._table.Clone());
				}
				return result;
			}

			// Token: 0x06006E12 RID: 28178 RVA: 0x0017C1B4 File Offset: 0x0017A3B4
			IEnumerator IEnumerable.GetEnumerator()
			{
				return this._table.GetEnumerator();
			}

			// Token: 0x06006E13 RID: 28179 RVA: 0x0017C1C1 File Offset: 0x0017A3C1
			public override IDictionaryEnumerator GetEnumerator()
			{
				return this._table.GetEnumerator();
			}

			// Token: 0x170012BC RID: 4796
			// (get) Token: 0x06006E14 RID: 28180 RVA: 0x0017C1D0 File Offset: 0x0017A3D0
			public override ICollection Keys
			{
				get
				{
					object syncRoot = this._table.SyncRoot;
					ICollection keys;
					lock (syncRoot)
					{
						keys = this._table.Keys;
					}
					return keys;
				}
			}

			// Token: 0x170012BD RID: 4797
			// (get) Token: 0x06006E15 RID: 28181 RVA: 0x0017C21C File Offset: 0x0017A41C
			public override ICollection Values
			{
				get
				{
					object syncRoot = this._table.SyncRoot;
					ICollection values;
					lock (syncRoot)
					{
						values = this._table.Values;
					}
					return values;
				}
			}

			// Token: 0x06006E16 RID: 28182 RVA: 0x0017C268 File Offset: 0x0017A468
			public override void Remove(object key)
			{
				object syncRoot = this._table.SyncRoot;
				lock (syncRoot)
				{
					this._table.Remove(key);
				}
			}

			// Token: 0x06006E17 RID: 28183 RVA: 0x0017C2B4 File Offset: 0x0017A4B4
			public override void OnDeserialization(object sender)
			{
			}

			// Token: 0x06006E18 RID: 28184 RVA: 0x0017C2B6 File Offset: 0x0017A4B6
			internal override KeyValuePairs[] ToKeyValuePairsArray()
			{
				return this._table.ToKeyValuePairsArray();
			}

			// Token: 0x04003576 RID: 13686
			protected Hashtable _table;
		}

		// Token: 0x02000BBB RID: 3003
		[Serializable]
		private class HashtableEnumerator : IDictionaryEnumerator, IEnumerator, ICloneable
		{
			// Token: 0x06006E19 RID: 28185 RVA: 0x0017C2C3 File Offset: 0x0017A4C3
			internal HashtableEnumerator(Hashtable hashtable, int getObjRetType)
			{
				this.hashtable = hashtable;
				this.bucket = hashtable.buckets.Length;
				this.version = hashtable.version;
				this.current = false;
				this.getObjectRetType = getObjRetType;
			}

			// Token: 0x06006E1A RID: 28186 RVA: 0x0017C2FC File Offset: 0x0017A4FC
			public object Clone()
			{
				return base.MemberwiseClone();
			}

			// Token: 0x170012BE RID: 4798
			// (get) Token: 0x06006E1B RID: 28187 RVA: 0x0017C304 File Offset: 0x0017A504
			public virtual object Key
			{
				get
				{
					if (!this.current)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumNotStarted"));
					}
					return this.currentKey;
				}
			}

			// Token: 0x06006E1C RID: 28188 RVA: 0x0017C324 File Offset: 0x0017A524
			public virtual bool MoveNext()
			{
				if (this.version != this.hashtable.version)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
				}
				while (this.bucket > 0)
				{
					this.bucket--;
					object key = this.hashtable.buckets[this.bucket].key;
					if (key != null && key != this.hashtable.buckets)
					{
						this.currentKey = key;
						this.currentValue = this.hashtable.buckets[this.bucket].val;
						this.current = true;
						return true;
					}
				}
				this.current = false;
				return false;
			}

			// Token: 0x170012BF RID: 4799
			// (get) Token: 0x06006E1D RID: 28189 RVA: 0x0017C3D3 File Offset: 0x0017A5D3
			public virtual DictionaryEntry Entry
			{
				get
				{
					if (!this.current)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
					}
					return new DictionaryEntry(this.currentKey, this.currentValue);
				}
			}

			// Token: 0x170012C0 RID: 4800
			// (get) Token: 0x06006E1E RID: 28190 RVA: 0x0017C400 File Offset: 0x0017A600
			public virtual object Current
			{
				get
				{
					if (!this.current)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
					}
					if (this.getObjectRetType == 1)
					{
						return this.currentKey;
					}
					if (this.getObjectRetType == 2)
					{
						return this.currentValue;
					}
					return new DictionaryEntry(this.currentKey, this.currentValue);
				}
			}

			// Token: 0x170012C1 RID: 4801
			// (get) Token: 0x06006E1F RID: 28191 RVA: 0x0017C45B File Offset: 0x0017A65B
			public virtual object Value
			{
				get
				{
					if (!this.current)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
					}
					return this.currentValue;
				}
			}

			// Token: 0x06006E20 RID: 28192 RVA: 0x0017C47C File Offset: 0x0017A67C
			public virtual void Reset()
			{
				if (this.version != this.hashtable.version)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
				}
				this.current = false;
				this.bucket = this.hashtable.buckets.Length;
				this.currentKey = null;
				this.currentValue = null;
			}

			// Token: 0x04003577 RID: 13687
			private Hashtable hashtable;

			// Token: 0x04003578 RID: 13688
			private int bucket;

			// Token: 0x04003579 RID: 13689
			private int version;

			// Token: 0x0400357A RID: 13690
			private bool current;

			// Token: 0x0400357B RID: 13691
			private int getObjectRetType;

			// Token: 0x0400357C RID: 13692
			private object currentKey;

			// Token: 0x0400357D RID: 13693
			private object currentValue;

			// Token: 0x0400357E RID: 13694
			internal const int Keys = 1;

			// Token: 0x0400357F RID: 13695
			internal const int Values = 2;

			// Token: 0x04003580 RID: 13696
			internal const int DictEntry = 3;
		}

		// Token: 0x02000BBC RID: 3004
		internal class HashtableDebugView
		{
			// Token: 0x06006E21 RID: 28193 RVA: 0x0017C4D6 File Offset: 0x0017A6D6
			public HashtableDebugView(Hashtable hashtable)
			{
				if (hashtable == null)
				{
					throw new ArgumentNullException("hashtable");
				}
				this.hashtable = hashtable;
			}

			// Token: 0x170012C2 RID: 4802
			// (get) Token: 0x06006E22 RID: 28194 RVA: 0x0017C4F3 File Offset: 0x0017A6F3
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public KeyValuePairs[] Items
			{
				get
				{
					return this.hashtable.ToKeyValuePairsArray();
				}
			}

			// Token: 0x04003581 RID: 13697
			private Hashtable hashtable;
		}
	}
}
