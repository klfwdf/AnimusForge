using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Buffers
{
	// Token: 0x02000035 RID: 53
	[Nullable(new byte[] { 0, 1 })]
	internal sealed class TlsOverPerCoreLockedStacksArrayPool<[Nullable(2)] T> : ArrayPool<T>
	{
		// Token: 0x06000213 RID: 531 RVA: 0x0000B45C File Offset: 0x0000965C
		[return: Nullable(new byte[] { 1, 0 })]
		private TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks CreatePerCoreLockedStacks(int bucketIndex)
		{
			TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks perCoreLockedStacks = new TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks();
			return Interlocked.CompareExchange<TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks>(ref this._buckets[bucketIndex], perCoreLockedStacks, null) ?? perCoreLockedStacks;
		}

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x06000214 RID: 532 RVA: 0x0000B487 File Offset: 0x00009687
		private int Id
		{
			get
			{
				return this.GetHashCode();
			}
		}

		// Token: 0x06000215 RID: 533 RVA: 0x0000B490 File Offset: 0x00009690
		[NullableContext(1)]
		public override T[] Rent(int minimumLength)
		{
			int num = Utilities.SelectBucketIndex(minimumLength);
			TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[] array = TlsOverPerCoreLockedStacksArrayPool<T>.t_tlsBuckets;
			if (array != null && num < array.Length)
			{
				T[] array2 = array[num].Array;
				if (array2 != null)
				{
					array[num].Array = null;
					return array2;
				}
			}
			TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks[] buckets = this._buckets;
			if (num < buckets.Length)
			{
				TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks perCoreLockedStacks = buckets[num];
				if (perCoreLockedStacks != null)
				{
					T[] array2 = perCoreLockedStacks.TryPop();
					if (array2 != null)
					{
						return array2;
					}
				}
				minimumLength = Utilities.GetMaxSizeForBucket(num);
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

		// Token: 0x06000216 RID: 534 RVA: 0x0000B520 File Offset: 0x00009720
		[NullableContext(1)]
		public override void Return(T[] array, bool clearArray = false)
		{
			if (array == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
			}
			int num = Utilities.SelectBucketIndex(array.Length);
			TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[] array2 = TlsOverPerCoreLockedStacksArrayPool<T>.t_tlsBuckets ?? this.InitializeTlsBucketsAndTrimming();
			if (num < array2.Length)
			{
				if (clearArray)
				{
					Array.Clear(array, 0, array.Length);
				}
				if (array.Length != Utilities.GetMaxSizeForBucket(num))
				{
					throw new ArgumentException("Buffer not from this pool", "array");
				}
				TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[] array3 = array2;
				int num2 = num;
				T[] array4 = array3[num2].Array;
				array3[num2] = new TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray(array);
				if (array4 != null)
				{
					(this._buckets[num] ?? this.CreatePerCoreLockedStacks(num)).TryPush(array4);
				}
			}
		}

		// Token: 0x06000217 RID: 535 RVA: 0x0000B5B4 File Offset: 0x000097B4
		public bool Trim()
		{
			int tickCount = Environment.TickCount;
			Utilities.MemoryPressure memoryPressure = Utilities.GetMemoryPressure();
			TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks[] buckets = this._buckets;
			for (int i = 0; i < buckets.Length; i++)
			{
				TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks perCoreLockedStacks = buckets[i];
				if (perCoreLockedStacks != null)
				{
					perCoreLockedStacks.Trim(tickCount, this.Id, memoryPressure, Utilities.GetMaxSizeForBucket(i));
				}
			}
			if (memoryPressure == Utilities.MemoryPressure.High)
			{
				using (IEnumerator<KeyValuePair<TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[], object>> enumerator = this._allTlsBuckets.GetEnumerator<TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[], object>())
				{
					while (enumerator.MoveNext())
					{
						KeyValuePair<TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[], object> keyValuePair = enumerator.Current;
						Array.Clear(keyValuePair.Key, 0, keyValuePair.Key.Length);
					}
					goto IL_143;
				}
			}
			uint num;
			if (memoryPressure == Utilities.MemoryPressure.Medium)
			{
				num = 15000U;
			}
			else
			{
				num = 30000U;
			}
			uint num2 = num;
			using (IEnumerator<KeyValuePair<TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[], object>> enumerator = this._allTlsBuckets.GetEnumerator<TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[], object>())
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[], object> keyValuePair2 = enumerator.Current;
					TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[] key = keyValuePair2.Key;
					for (int j = 0; j < key.Length; j++)
					{
						if (key[j].Array != null)
						{
							int millisecondsTimeStamp = key[j].MillisecondsTimeStamp;
							if (millisecondsTimeStamp == 0)
							{
								key[j].MillisecondsTimeStamp = tickCount;
							}
							else if ((long)(tickCount - millisecondsTimeStamp) >= (long)((ulong)num2))
							{
								Interlocked.Exchange<T[]>(ref key[j].Array, null);
							}
						}
					}
				}
			}
			IL_143:
			return !Environment.HasShutdownStarted && !AppDomain.CurrentDomain.IsFinalizingForUnload();
		}

		// Token: 0x06000218 RID: 536 RVA: 0x0000B738 File Offset: 0x00009938
		[return: Nullable(new byte[] { 1, 0, 0 })]
		private TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[] InitializeTlsBucketsAndTrimming()
		{
			TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[] array = new TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[27];
			TlsOverPerCoreLockedStacksArrayPool<T>.t_tlsBuckets = array;
			this._allTlsBuckets.Add(array, null);
			if (Interlocked.Exchange(ref this._trimCallbackCreated, 1) == 0)
			{
				Gen2GcCallback.Register((object s) => ((TlsOverPerCoreLockedStacksArrayPool<T>)s).Trim(), this);
			}
			return array;
		}

		// Token: 0x0400006F RID: 111
		private const int NumBuckets = 27;

		// Token: 0x04000070 RID: 112
		private const int MaxPerCorePerArraySizeStacks = 64;

		// Token: 0x04000071 RID: 113
		private const int MaxBuffersPerArraySizePerCore = 8;

		// Token: 0x04000072 RID: 114
		[Nullable(new byte[] { 2, 0, 0 })]
		[ThreadStatic]
		private static TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[] t_tlsBuckets;

		// Token: 0x04000073 RID: 115
		[Nullable(new byte[] { 1, 1, 0, 0, 2 })]
		private readonly ConditionalWeakTable<TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[], object> _allTlsBuckets = new ConditionalWeakTable<TlsOverPerCoreLockedStacksArrayPool<T>.ThreadLocalArray[], object>();

		// Token: 0x04000074 RID: 116
		[Nullable(new byte[] { 1, 2, 0 })]
		private readonly TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks[] _buckets = new TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks[27];

		// Token: 0x04000075 RID: 117
		private int _trimCallbackCreated;

		// Token: 0x02000066 RID: 102
		private sealed class PerCoreLockedStacks
		{
			// Token: 0x060002D7 RID: 727 RVA: 0x0000D000 File Offset: 0x0000B200
			public PerCoreLockedStacks()
			{
				TlsOverPerCoreLockedStacksArrayPool<T>.LockedStack[] array = new TlsOverPerCoreLockedStacksArrayPool<T>.LockedStack[TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks.s_lockedStackCount];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = new TlsOverPerCoreLockedStacksArrayPool<T>.LockedStack();
				}
				this._perCoreStacks = array;
			}

			// Token: 0x060002D8 RID: 728 RVA: 0x0000D03C File Offset: 0x0000B23C
			[NullableContext(1)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool TryPush(T[] array)
			{
				TlsOverPerCoreLockedStacksArrayPool<T>.LockedStack[] perCoreStacks = this._perCoreStacks;
				int num = EnvironmentEx.CurrentManagedThreadId % TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks.s_lockedStackCount;
				for (int i = 0; i < perCoreStacks.Length; i++)
				{
					if (perCoreStacks[num].TryPush(array))
					{
						return true;
					}
					if (++num == perCoreStacks.Length)
					{
						num = 0;
					}
				}
				return false;
			}

			// Token: 0x060002D9 RID: 729 RVA: 0x0000D084 File Offset: 0x0000B284
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			[return: Nullable(new byte[] { 2, 1 })]
			public T[] TryPop()
			{
				TlsOverPerCoreLockedStacksArrayPool<T>.LockedStack[] perCoreStacks = this._perCoreStacks;
				int num = EnvironmentEx.CurrentManagedThreadId % TlsOverPerCoreLockedStacksArrayPool<T>.PerCoreLockedStacks.s_lockedStackCount;
				for (int i = 0; i < perCoreStacks.Length; i++)
				{
					T[] result;
					if ((result = perCoreStacks[num].TryPop()) != null)
					{
						return result;
					}
					if (++num == perCoreStacks.Length)
					{
						num = 0;
					}
				}
				return null;
			}

			// Token: 0x060002DA RID: 730 RVA: 0x0000D0D0 File Offset: 0x0000B2D0
			public void Trim(int currentMilliseconds, int id, Utilities.MemoryPressure pressure, int bucketSize)
			{
				TlsOverPerCoreLockedStacksArrayPool<T>.LockedStack[] perCoreStacks = this._perCoreStacks;
				for (int i = 0; i < perCoreStacks.Length; i++)
				{
					perCoreStacks[i].Trim(currentMilliseconds, id, pressure, bucketSize);
				}
			}

			// Token: 0x040000BF RID: 191
			private static readonly int s_lockedStackCount = Math.Min(Environment.ProcessorCount, 64);

			// Token: 0x040000C0 RID: 192
			[Nullable(new byte[] { 1, 1, 0 })]
			private readonly TlsOverPerCoreLockedStacksArrayPool<T>.LockedStack[] _perCoreStacks;
		}

		// Token: 0x02000067 RID: 103
		private sealed class LockedStack
		{
			// Token: 0x060002DC RID: 732 RVA: 0x0000D114 File Offset: 0x0000B314
			[NullableContext(1)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool TryPush(T[] array)
			{
				bool result = false;
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
					result = true;
				}
				Monitor.Exit(this);
				return result;
			}

			// Token: 0x060002DD RID: 733 RVA: 0x0000D160 File Offset: 0x0000B360
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			[return: Nullable(new byte[] { 2, 1 })]
			public T[] TryPop()
			{
				T[] result = null;
				Monitor.Enter(this);
				T[][] arrays = this._arrays;
				int num = this._count - 1;
				if (num < arrays.Length)
				{
					result = arrays[num];
					arrays[num] = null;
					this._count = num;
				}
				Monitor.Exit(this);
				return result;
			}

			// Token: 0x060002DE RID: 734 RVA: 0x0000D1A4 File Offset: 0x0000B3A4
			public void Trim(int currentMilliseconds, int id, Utilities.MemoryPressure pressure, int bucketSize)
			{
				if (this._count == 0)
				{
					return;
				}
				int num = ((pressure == Utilities.MemoryPressure.High) ? 10000 : 60000);
				lock (this)
				{
					if (this._count != 0)
					{
						if (this._millisecondsTimestamp == 0)
						{
							this._millisecondsTimestamp = currentMilliseconds;
						}
						else if (currentMilliseconds - this._millisecondsTimestamp > num)
						{
							int num2 = 1;
							if (pressure != Utilities.MemoryPressure.Medium)
							{
								if (pressure == Utilities.MemoryPressure.High)
								{
									num2 = 8;
									if (bucketSize > 16384)
									{
										num2++;
									}
									if (Unsafe.SizeOf<T>() > 16)
									{
										num2++;
									}
									if (Unsafe.SizeOf<T>() > 32)
									{
										num2++;
									}
								}
							}
							else
							{
								num2 = 2;
							}
							while (this._count > 0 && num2-- > 0)
							{
								T[][] arrays = this._arrays;
								int num3 = this._count - 1;
								this._count = num3;
								object obj = arrays[num3];
								this._arrays[this._count] = null;
							}
							this._millisecondsTimestamp = ((this._count > 0) ? (this._millisecondsTimestamp + num / 4) : 0);
						}
					}
				}
			}

			// Token: 0x040000C1 RID: 193
			[Nullable(new byte[] { 1, 2, 1 })]
			private readonly T[][] _arrays = new T[8][];

			// Token: 0x040000C2 RID: 194
			private int _count;

			// Token: 0x040000C3 RID: 195
			private int _millisecondsTimestamp;
		}

		// Token: 0x02000068 RID: 104
		private struct ThreadLocalArray
		{
			// Token: 0x060002E0 RID: 736 RVA: 0x0000D2C8 File Offset: 0x0000B4C8
			[NullableContext(1)]
			public ThreadLocalArray(T[] array)
			{
				this.Array = array;
				this.MillisecondsTimeStamp = 0;
			}

			// Token: 0x040000C4 RID: 196
			[Nullable(new byte[] { 2, 1 })]
			public T[] Array;

			// Token: 0x040000C5 RID: 197
			public int MillisecondsTimeStamp;
		}
	}
}
