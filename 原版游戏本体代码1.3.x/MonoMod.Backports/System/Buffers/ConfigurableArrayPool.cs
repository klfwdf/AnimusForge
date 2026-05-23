using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Buffers
{
	// Token: 0x0200002A RID: 42
	[NullableContext(1)]
	[Nullable(new byte[] { 0, 1 })]
	internal sealed class ConfigurableArrayPool<[Nullable(2)] T> : ArrayPool<T>
	{
		// Token: 0x060001B1 RID: 433 RVA: 0x00009F89 File Offset: 0x00008189
		internal ConfigurableArrayPool()
			: this(1048576, 50)
		{
		}

		// Token: 0x060001B2 RID: 434 RVA: 0x00009F98 File Offset: 0x00008198
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
			int id = this.Id;
			ConfigurableArrayPool<T>.Bucket[] array = new ConfigurableArrayPool<T>.Bucket[Utilities.SelectBucketIndex(maxArrayLength) + 1];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new ConfigurableArrayPool<T>.Bucket(Utilities.GetMaxSizeForBucket(i), maxArraysPerBucket, id);
			}
			this._buckets = array;
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x060001B3 RID: 435 RVA: 0x0000A01D File Offset: 0x0000821D
		private int Id
		{
			get
			{
				return this.GetHashCode();
			}
		}

		// Token: 0x060001B4 RID: 436 RVA: 0x0000A028 File Offset: 0x00008228
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
			int num = Utilities.SelectBucketIndex(minimumLength);
			T[] array;
			if (num < this._buckets.Length)
			{
				int num2 = num;
				for (;;)
				{
					array = this._buckets[num2].Rent();
					if (array != null)
					{
						break;
					}
					if (++num2 >= this._buckets.Length || num2 == num + 2)
					{
						goto IL_54;
					}
				}
				return array;
				IL_54:
				array = new T[this._buckets[num]._bufferLength];
			}
			else
			{
				array = new T[minimumLength];
			}
			return array;
		}

		// Token: 0x060001B5 RID: 437 RVA: 0x0000A0A8 File Offset: 0x000082A8
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
			int num = Utilities.SelectBucketIndex(array.Length);
			if (num < this._buckets.Length)
			{
				if (clearArray)
				{
					Array.Clear(array, 0, array.Length);
				}
				this._buckets[num].Return(array);
			}
		}

		// Token: 0x04000053 RID: 83
		private const int DefaultMaxArrayLength = 1048576;

		// Token: 0x04000054 RID: 84
		private const int DefaultMaxNumberOfArraysPerBucket = 50;

		// Token: 0x04000055 RID: 85
		[Nullable(new byte[] { 1, 1, 0 })]
		private readonly ConfigurableArrayPool<T>.Bucket[] _buckets;

		// Token: 0x02000062 RID: 98
		[NullableContext(0)]
		private sealed class Bucket
		{
			// Token: 0x060002CE RID: 718 RVA: 0x0000CE4D File Offset: 0x0000B04D
			internal Bucket(int bufferLength, int numberOfBuffers, int poolId)
			{
				this._lock = new SpinLock(Debugger.IsAttached);
				this._buffers = new T[numberOfBuffers][];
				this._bufferLength = bufferLength;
				this._poolId = poolId;
			}

			// Token: 0x17000044 RID: 68
			// (get) Token: 0x060002CF RID: 719 RVA: 0x0000CE7F File Offset: 0x0000B07F
			internal int Id
			{
				get
				{
					return this.GetHashCode();
				}
			}

			// Token: 0x060002D0 RID: 720 RVA: 0x0000CE88 File Offset: 0x0000B088
			[return: Nullable(new byte[] { 2, 1 })]
			internal T[] Rent()
			{
				T[][] buffers = this._buffers;
				T[] array = null;
				bool flag = false;
				bool flag2 = false;
				try
				{
					this._lock.Enter(ref flag);
					if (this._index < buffers.Length)
					{
						array = buffers[this._index];
						T[][] array2 = buffers;
						int index = this._index;
						this._index = index + 1;
						array2[index] = null;
						flag2 = array == null;
					}
				}
				finally
				{
					if (flag)
					{
						this._lock.Exit(false);
					}
				}
				if (flag2)
				{
					array = new T[this._bufferLength];
				}
				return array;
			}

			// Token: 0x060002D1 RID: 721 RVA: 0x0000CF14 File Offset: 0x0000B114
			[NullableContext(1)]
			internal void Return(T[] array)
			{
				if (array.Length != this._bufferLength)
				{
					throw new ArgumentException("Buffer not from this pool", "array");
				}
				bool flag = false;
				try
				{
					this._lock.Enter(ref flag);
					bool flag2 = this._index != 0;
					if (flag2)
					{
						T[][] buffers = this._buffers;
						int num = this._index - 1;
						this._index = num;
						buffers[num] = array;
					}
				}
				finally
				{
					if (flag)
					{
						this._lock.Exit(false);
					}
				}
			}

			// Token: 0x040000B0 RID: 176
			internal readonly int _bufferLength;

			// Token: 0x040000B1 RID: 177
			[Nullable(new byte[] { 1, 2, 1 })]
			private readonly T[][] _buffers;

			// Token: 0x040000B2 RID: 178
			private readonly int _poolId;

			// Token: 0x040000B3 RID: 179
			private SpinLock _lock;

			// Token: 0x040000B4 RID: 180
			private int _index;
		}
	}
}
