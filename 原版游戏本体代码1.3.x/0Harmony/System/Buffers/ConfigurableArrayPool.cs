using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Buffers
{
	// Token: 0x0200048F RID: 1167
	[NullableContext(1)]
	[Nullable(new byte[] { 0, 1 })]
	internal sealed class ConfigurableArrayPool<[Nullable(2)] T> : ArrayPool<T>
	{
		// Token: 0x06001A0A RID: 6666 RVA: 0x00055039 File Offset: 0x00053239
		internal ConfigurableArrayPool()
			: this(1048576, 50)
		{
		}

		// Token: 0x06001A0B RID: 6667 RVA: 0x00055048 File Offset: 0x00053248
		internal ConfigurableArrayPool(int maxArrayLength, int maxArraysPerBucket)
		{
			if (maxArrayLength <= 0)
			{
				throw new ArgumentOutOfRangeException("maxArrayLength");
			}
			if (maxArraysPerBucket <= 0)
			{
				throw new ArgumentOutOfRangeException("maxArraysPerBucket");
			}
			if (maxArrayLength > 1073741824)
			{
				maxArrayLength = 1073741824;
			}
			else if (maxArrayLength < 16)
			{
				maxArrayLength = 16;
			}
			int poolId = this.Id;
			ConfigurableArrayPool<T>.Bucket[] buckets = new ConfigurableArrayPool<T>.Bucket[Utilities.SelectBucketIndex(maxArrayLength) + 1];
			for (int i = 0; i < buckets.Length; i++)
			{
				buckets[i] = new ConfigurableArrayPool<T>.Bucket(Utilities.GetMaxSizeForBucket(i), maxArraysPerBucket, poolId);
			}
			this._buckets = buckets;
		}

		// Token: 0x170005BF RID: 1471
		// (get) Token: 0x06001A0C RID: 6668 RVA: 0x000550CD File Offset: 0x000532CD
		private int Id
		{
			get
			{
				return this.GetHashCode();
			}
		}

		// Token: 0x06001A0D RID: 6669 RVA: 0x000550D8 File Offset: 0x000532D8
		public override T[] Rent(int minimumLength)
		{
			if (minimumLength < 0)
			{
				throw new ArgumentOutOfRangeException("minimumLength");
			}
			if (minimumLength == 0)
			{
				return ArrayEx.Empty<T>();
			}
			int index = Utilities.SelectBucketIndex(minimumLength);
			T[] buffer;
			if (index < this._buckets.Length)
			{
				int i = index;
				for (;;)
				{
					buffer = this._buckets[i].Rent();
					if (buffer != null)
					{
						break;
					}
					if (++i >= this._buckets.Length || i == index + 2)
					{
						goto IL_54;
					}
				}
				return buffer;
				IL_54:
				buffer = new T[this._buckets[index]._bufferLength];
			}
			else
			{
				buffer = new T[minimumLength];
			}
			return buffer;
		}

		// Token: 0x06001A0E RID: 6670 RVA: 0x00055158 File Offset: 0x00053358
		public override void Return(T[] array, bool clearArray = false)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (array.Length == 0)
			{
				return;
			}
			int bucket = Utilities.SelectBucketIndex(array.Length);
			if (bucket < this._buckets.Length)
			{
				if (clearArray)
				{
					Array.Clear(array, 0, array.Length);
				}
				this._buckets[bucket].Return(array);
			}
		}

		// Token: 0x040010D2 RID: 4306
		private const int DefaultMaxArrayLength = 1048576;

		// Token: 0x040010D3 RID: 4307
		private const int DefaultMaxNumberOfArraysPerBucket = 50;

		// Token: 0x040010D4 RID: 4308
		[Nullable(new byte[] { 1, 1, 0 })]
		private readonly ConfigurableArrayPool<T>.Bucket[] _buckets;

		// Token: 0x02000490 RID: 1168
		[NullableContext(0)]
		private sealed class Bucket
		{
			// Token: 0x06001A0F RID: 6671 RVA: 0x000551A9 File Offset: 0x000533A9
			internal Bucket(int bufferLength, int numberOfBuffers, int poolId)
			{
				this._lock = new SpinLock(Debugger.IsAttached);
				this._buffers = new T[numberOfBuffers][];
				this._bufferLength = bufferLength;
				this._poolId = poolId;
			}

			// Token: 0x170005C0 RID: 1472
			// (get) Token: 0x06001A10 RID: 6672 RVA: 0x000550CD File Offset: 0x000532CD
			internal int Id
			{
				get
				{
					return this.GetHashCode();
				}
			}

			// Token: 0x06001A11 RID: 6673 RVA: 0x000551DC File Offset: 0x000533DC
			[return: Nullable(new byte[] { 2, 1 })]
			internal T[] Rent()
			{
				T[][] buffers = this._buffers;
				T[] buffer = null;
				bool lockTaken = false;
				bool allocateBuffer = false;
				try
				{
					this._lock.Enter(ref lockTaken);
					if (this._index < buffers.Length)
					{
						buffer = buffers[this._index];
						T[][] array = buffers;
						int index = this._index;
						this._index = index + 1;
						array[index] = null;
						allocateBuffer = buffer == null;
					}
				}
				finally
				{
					if (lockTaken)
					{
						this._lock.Exit(false);
					}
				}
				if (allocateBuffer)
				{
					buffer = new T[this._bufferLength];
				}
				return buffer;
			}

			// Token: 0x06001A12 RID: 6674 RVA: 0x00055268 File Offset: 0x00053468
			[NullableContext(1)]
			internal void Return(T[] array)
			{
				if (array.Length != this._bufferLength)
				{
					throw new ArgumentException("Buffer not from this pool", "array");
				}
				bool lockTaken = false;
				try
				{
					this._lock.Enter(ref lockTaken);
					bool returned = this._index != 0;
					if (returned)
					{
						T[][] buffers = this._buffers;
						int num = this._index - 1;
						this._index = num;
						buffers[num] = array;
					}
				}
				finally
				{
					if (lockTaken)
					{
						this._lock.Exit(false);
					}
				}
			}

			// Token: 0x040010D5 RID: 4309
			internal readonly int _bufferLength;

			// Token: 0x040010D6 RID: 4310
			[Nullable(new byte[] { 1, 2, 1 })]
			private readonly T[][] _buffers;

			// Token: 0x040010D7 RID: 4311
			private readonly int _poolId;

			// Token: 0x040010D8 RID: 4312
			private SpinLock _lock;

			// Token: 0x040010D9 RID: 4313
			private int _index;
		}
	}
}
