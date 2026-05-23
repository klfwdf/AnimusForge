using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Buffers
{
	// Token: 0x0200049E RID: 1182
	[Nullable(new byte[] { 0, 1 })]
	internal sealed class TlsOverPerCoreLockedStacksArrayPool<[Nullable(2)] T> : ArrayPool<T>
	{
		// Token: 0x06001A75 RID: 6773 RVA: 0x00056670 File Offset: 0x00054870
		[return: Nullable(new byte[] { 1, 0 })]
		private TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks CreatePerCoreLockedStacks(int bucketIndex)
		{
			TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks inst = new TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks();
			return Interlocked.CompareExchange<TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks>(ref this._buckets[bucketIndex], inst, null) ?? inst;
		}

		// Token: 0x170005D5 RID: 1493
		// (get) Token: 0x06001A76 RID: 6774 RVA: 0x000550CD File Offset: 0x000532CD
		private int Id
		{
			get
			{
				return this.GetHashCode();
			}
		}

		// Token: 0x06001A77 RID: 6775 RVA: 0x0005669C File Offset: 0x0005489C
		[NullableContext(1)]
		public override T[] Rent(int minimumLength)
		{
			int bucketIndex = Utilities.SelectBucketIndex(minimumLength);
			TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[] tlsBuckets = TlsOverPerCoreLockedStacksArrayPool<T>.t_tlsBuckets;
			if (tlsBuckets != null && bucketIndex < tlsBuckets.Length)
			{
				T[] buffer = tlsBuckets[bucketIndex].Array;
				if (buffer != null)
				{
					tlsBuckets[bucketIndex].Array = null;
					return buffer;
				}
			}
			TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks[] perCoreBuckets = this._buckets;
			if (bucketIndex < perCoreBuckets.Length)
			{
				TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks b = perCoreBuckets[bucketIndex];
				if (b != null)
				{
					T[] buffer = b.TryPop();
					if (buffer != null)
					{
						return buffer;
					}
				}
				minimumLength = Utilities.GetMaxSizeForBucket(bucketIndex);
			}
			else
			{
				if (minimumLength == 0)
				{
					return ArrayEx.Empty<T>();
				}
				if (minimumLength < 0)
				{
					throw new ArgumentOutOfRangeException("minimumLength");
				}
			}
			return new T[minimumLength];
		}

		// Token: 0x06001A78 RID: 6776 RVA: 0x0005672C File Offset: 0x0005492C
		[NullableContext(1)]
		public override void Return(T[] array, bool clearArray = false)
		{
			if (array == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
			}
			int bucketIndex = Utilities.SelectBucketIndex(array.Length);
			TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[] tlsBuckets = TlsOverPerCoreLockedStacksArrayPool<T>.t_tlsBuckets ?? this.InitializeTlsBucketsAndTrimming();
			if (bucketIndex < tlsBuckets.Length)
			{
				if (clearArray)
				{
					Array.Clear(array, 0, array.Length);
				}
				if (array.Length != Utilities.GetMaxSizeForBucket(bucketIndex))
				{
					throw new ArgumentException("Buffer not from this pool", "array");
				}
				TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[] array2 = tlsBuckets;
				int num = bucketIndex;
				T[] prev = array2[num].Array;
				array2[num] = new TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray(array);
				if (prev != null)
				{
					(this._buckets[bucketIndex] ?? this.CreatePerCoreLockedStacks(bucketIndex)).TryPush(prev);
				}
			}
		}

		// Token: 0x06001A79 RID: 6777 RVA: 0x000567C0 File Offset: 0x000549C0
		public bool Trim()
		{
			int currentMilliseconds = Environment.TickCount;
			Utilities.MemoryPressure pressure = Utilities.GetMemoryPressure();
			TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks[] perCoreBuckets = this._buckets;
			for (int i = 0; i < perCoreBuckets.Length; i++)
			{
				TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks perCoreLockedStacks = perCoreBuckets[i];
				if (perCoreLockedStacks != null)
				{
					perCoreLockedStacks.Trim(currentMilliseconds, this.Id, pressure, Utilities.GetMaxSizeForBucket(i));
				}
			}
			if (pressure == Utilities.MemoryPressure.High)
			{
				using (IEnumerator<KeyValuePair<TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[], object>> enumerator = this._allTlsBuckets.GetEnumerator<TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[], object>())
				{
					while (enumerator.MoveNext())
					{
						KeyValuePair<TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[], object> tlsBuckets = enumerator.Current;
						Array.Clear(tlsBuckets.Key, 0, tlsBuckets.Key.Length);
					}
					goto IL_143;
				}
			}
			uint num;
			if (pressure == Utilities.MemoryPressure.Medium)
			{
				num = 15000U;
			}
			else
			{
				num = 30000U;
			}
			uint millisecondsThreshold = num;
			using (IEnumerator<KeyValuePair<TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[], object>> enumerator = this._allTlsBuckets.GetEnumerator<TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[], object>())
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[], object> tlsBuckets2 = enumerator.Current;
					TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[] buckets = tlsBuckets2.Key;
					for (int j = 0; j < buckets.Length; j++)
					{
						if (buckets[j].Array != null)
						{
							int lastSeen = buckets[j].MillisecondsTimeStamp;
							if (lastSeen == 0)
							{
								buckets[j].MillisecondsTimeStamp = currentMilliseconds;
							}
							else if ((long)(currentMilliseconds - lastSeen) >= (long)((ulong)millisecondsThreshold))
							{
								Interlocked.Exchange<T[]>(ref buckets[j].Array, null);
							}
						}
					}
				}
			}
			IL_143:
			return !Environment.HasShutdownStarted && !AppDomain.CurrentDomain.IsFinalizingForUnload();
		}

		// Token: 0x06001A7A RID: 6778 RVA: 0x00056944 File Offset: 0x00054B44
		[return: Nullable(new byte[] { 1, 0, 0 })]
		private TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[] InitializeTlsBucketsAndTrimming()
		{
			TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[] tlsBuckets = new TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[27];
			TlsOverPerCoreLockedStacksArrayPool<T>.t_tlsBuckets = tlsBuckets;
			this._allTlsBuckets.Add(tlsBuckets, null);
			if (Interlocked.Exchange(ref this._trimCallbackCreated, 1) == 0)
			{
				Gen2GcCallback.Register((object s) => ((TlsOverPerCoreLockedStacksArrayPool<T>)s).Trim(), this);
			}
			return tlsBuckets;
		}

		// Token: 0x040010FD RID: 4349
		private const int NumBuckets = 27;

		// Token: 0x040010FE RID: 4350
		private const int MaxPerCorePerArraySizeStacks = 64;

		// Token: 0x040010FF RID: 4351
		private const int MaxBuffersPerArraySizePerCore = 8;

		// Token: 0x04001100 RID: 4352
		[Nullable(new byte[] { 2, 0, 0 })]
		[ThreadStatic]
		private static TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[] t_tlsBuckets;

		// Token: 0x04001101 RID: 4353
		[Nullable(new byte[] { 1, 1, 0, 0, 2 })]
		private readonly ConditionalWeakTable<TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[], object> _allTlsBuckets = new ConditionalWeakTable<TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[], object>();

		// Token: 0x04001102 RID: 4354
		[Nullable(new byte[] { 1, 2, 0 })]
		private readonly TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks[] _buckets = new TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks[27];

		// Token: 0x04001103 RID: 4355
		private int _trimCallbackCreated;

		// Token: 0x0200049F RID: 1183
		private sealed class PerCoreLockedStacks
		{
			// Token: 0x06001A7C RID: 6780 RVA: 0x000569C0 File Offset: 0x00054BC0
			public PerCoreLockedStacks()
			{
				TlsOverPerCoreLockedStacksArrayPool<T>.LockedStack[] stacks = new TlsOverPerCoreLockedStacksArrayPool<T>.LockedStack[TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks.s_lockedStackCount];
				for (int i = 0; i < stacks.Length; i++)
				{
					stacks[i] = new TlsOverPerCoreLockedStacksArrayPool<T>.LockedStack();
				}
				this._perCoreStacks = stacks;
			}

			// Token: 0x06001A7D RID: 6781 RVA: 0x000569FC File Offset: 0x00054BFC
			[NullableContext(1)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool TryPush(T[] array)
			{
				TlsOverPerCoreLockedStacksArrayPool<T>.LockedStack[] stacks = this._perCoreStacks;
				int index = EnvironmentEx.CurrentManagedThreadId % TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks.s_lockedStackCount;
				for (int i = 0; i < stacks.Length; i++)
				{
					if (stacks[index].TryPush(array))
					{
						return true;
					}
					if (++index == stacks.Length)
					{
						index = 0;
					}
				}
				return false;
			}

			// Token: 0x06001A7E RID: 6782 RVA: 0x00056A44 File Offset: 0x00054C44
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			[return: Nullable(new byte[] { 2, 1 })]
			public T[] TryPop()
			{
				TlsOverPerCoreLockedStacksArrayPool<T>.LockedStack[] stacks = this._perCoreStacks;
				int index = EnvironmentEx.CurrentManagedThreadId % TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks.s_lockedStackCount;
				for (int i = 0; i < stacks.Length; i++)
				{
					T[] arr;
					if ((arr = stacks[index].TryPop()) != null)
					{
						return arr;
					}
					if (++index == stacks.Length)
					{
						index = 0;
					}
				}
				return null;
			}

			// Token: 0x06001A7F RID: 6783 RVA: 0x00056A90 File Offset: 0x00054C90
			public void Trim(int currentMilliseconds, int id, Utilities.MemoryPressure pressure, int bucketSize)
			{
				TlsOverPerCoreLockedStacksArrayPool<T>.LockedStack[] stacks = this._perCoreStacks;
				for (int i = 0; i < stacks.Length; i++)
				{
					stacks[i].Trim(currentMilliseconds, id, pressure, bucketSize);
				}
			}

			// Token: 0x04001104 RID: 4356
			private static readonly int s_lockedStackCount = Math.Min(Environment.ProcessorCount, 64);

			// Token: 0x04001105 RID: 4357
			[Nullable(new byte[] { 1, 1, 0 })]
			private readonly TlsOverPerCoreLockedStacksArrayPool<T>.LockedStack[] _perCoreStacks;
		}

		// Token: 0x020004A0 RID: 1184
		private sealed class LockedStack
		{
			// Token: 0x06001A81 RID: 6785 RVA: 0x00056AD4 File Offset: 0x00054CD4
			[NullableContext(1)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool TryPush(T[] array)
			{
				bool enqueued = false;
				Monitor.Enter(this);
				T[][] arrays = this._arrays;
				int count = this._count;
				if (count < arrays.Length)
				{
					if (count == 0)
					{
						this._millisecondsTimestamp = 0;
					}
					arrays[count] = array;
					this._count = count + 1;
					enqueued = true;
				}
				Monitor.Exit(this);
				return enqueued;
			}

			// Token: 0x06001A82 RID: 6786 RVA: 0x00056B20 File Offset: 0x00054D20
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			[return: Nullable(new byte[] { 2, 1 })]
			public T[] TryPop()
			{
				T[] arr = null;
				Monitor.Enter(this);
				T[][] arrays = this._arrays;
				int count = this._count - 1;
				if (count < arrays.Length)
				{
					arr = arrays[count];
					arrays[count] = null;
					this._count = count;
				}
				Monitor.Exit(this);
				return arr;
			}

			// Token: 0x06001A83 RID: 6787 RVA: 0x00056B64 File Offset: 0x00054D64
			public void Trim(int currentMilliseconds, int id, Utilities.MemoryPressure pressure, int bucketSize)
			{
				if (this._count == 0)
				{
					return;
				}
				int trimMilliseconds = ((pressure == Utilities.MemoryPressure.High) ? 10000 : 60000);
				lock (this)
				{
					if (this._count != 0)
					{
						if (this._millisecondsTimestamp == 0)
						{
							this._millisecondsTimestamp = currentMilliseconds;
						}
						else if (currentMilliseconds - this._millisecondsTimestamp > trimMilliseconds)
						{
							int trimCount = 1;
							if (pressure != Utilities.MemoryPressure.Medium)
							{
								if (pressure == Utilities.MemoryPressure.High)
								{
									trimCount = 8;
									if (bucketSize > 16384)
									{
										trimCount++;
									}
									if (Unsafe.SizeOf<T>() > 16)
									{
										trimCount++;
									}
									if (Unsafe.SizeOf<T>() > 32)
									{
										trimCount++;
									}
								}
							}
							else
							{
								trimCount = 2;
							}
							while (this._count > 0 && trimCount-- > 0)
							{
								T[][] arrays = this._arrays;
								int num = this._count - 1;
								this._count = num;
								object obj = arrays[num];
								this._arrays[this._count] = null;
							}
							this._millisecondsTimestamp = ((this._count > 0) ? (this._millisecondsTimestamp + trimMilliseconds / 4) : 0);
						}
					}
				}
			}

			// Token: 0x04001106 RID: 4358
			[Nullable(new byte[] { 1, 2, 1 })]
			private readonly T[][] _arrays = new T[8][];

			// Token: 0x04001107 RID: 4359
			private int _count;

			// Token: 0x04001108 RID: 4360
			private int _millisecondsTimestamp;
		}

		// Token: 0x020004A1 RID: 1185
		private struct ThreadLocalArray
		{
			// Token: 0x06001A85 RID: 6789 RVA: 0x00056C88 File Offset: 0x00054E88
			[NullableContext(1)]
			public ThreadLocalArray(T[] array)
			{
				this.Array = array;
				this.MillisecondsTimeStamp = 0;
			}

			// Token: 0x04001109 RID: 4361
			[Nullable(new byte[] { 2, 1 })]
			public T[] Array;

			// Token: 0x0400110A RID: 4362
			public int MillisecondsTimeStamp;
		}
	}
}
