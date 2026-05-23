using System;
using System.Collections.Generic;
using System.Security.Permissions;

namespace System.Collections.Concurrent
{
	// Token: 0x020004B2 RID: 1202
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public abstract class OrderablePartitioner<TSource> : Partitioner<TSource>
	{
		// Token: 0x06003993 RID: 14739 RVA: 0x000DC8C6 File Offset: 0x000DAAC6
		[__DynamicallyInvokable]
		protected OrderablePartitioner(bool keysOrderedInEachPartition, bool keysOrderedAcrossPartitions, bool keysNormalized)
		{
			this.KeysOrderedInEachPartition = keysOrderedInEachPartition;
			this.KeysOrderedAcrossPartitions = keysOrderedAcrossPartitions;
			this.KeysNormalized = keysNormalized;
		}

		// Token: 0x06003994 RID: 14740
		[__DynamicallyInvokable]
		public abstract IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount);

		// Token: 0x06003995 RID: 14741 RVA: 0x000DC8E3 File Offset: 0x000DAAE3
		[__DynamicallyInvokable]
		public virtual IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions()
		{
			throw new NotSupportedException(Environment.GetResourceString("Partitioner_DynamicPartitionsNotSupported"));
		}

		// Token: 0x170008A3 RID: 2211
		// (get) Token: 0x06003996 RID: 14742 RVA: 0x000DC8F4 File Offset: 0x000DAAF4
		// (set) Token: 0x06003997 RID: 14743 RVA: 0x000DC8FC File Offset: 0x000DAAFC
		[__DynamicallyInvokable]
		public bool KeysOrderedInEachPartition
		{
			[__DynamicallyInvokable]
			get;
			private set; }

		// Token: 0x170008A4 RID: 2212
		// (get) Token: 0x06003998 RID: 14744 RVA: 0x000DC905 File Offset: 0x000DAB05
		// (set) Token: 0x06003999 RID: 14745 RVA: 0x000DC90D File Offset: 0x000DAB0D
		[__DynamicallyInvokable]
		public bool KeysOrderedAcrossPartitions
		{
			[__DynamicallyInvokable]
			get;
			private set; }

		// Token: 0x170008A5 RID: 2213
		// (get) Token: 0x0600399A RID: 14746 RVA: 0x000DC916 File Offset: 0x000DAB16
		// (set) Token: 0x0600399B RID: 14747 RVA: 0x000DC91E File Offset: 0x000DAB1E
		[__DynamicallyInvokable]
		public bool KeysNormalized
		{
			[__DynamicallyInvokable]
			get;
			private set; }

		// Token: 0x0600399C RID: 14748 RVA: 0x000DC928 File Offset: 0x000DAB28
		[__DynamicallyInvokable]
		public override IList<IEnumerator<TSource>> GetPartitions(int partitionCount)
		{
			IList<IEnumerator<KeyValuePair<long, TSource>>> orderablePartitions = this.GetOrderablePartitions(partitionCount);
			if (orderablePartitions.Count != partitionCount)
			{
				throw new InvalidOperationException("OrderablePartitioner_GetPartitions_WrongNumberOfPartitions");
			}
			IEnumerator<TSource>[] array = new IEnumerator<!0>[partitionCount];
			for (int i = 0; i < partitionCount; i++)
			{
				array[i] = new OrderablePartitioner<TSource>.EnumeratorDropIndices(orderablePartitions[i]);
			}
			return array;
		}

		// Token: 0x0600399D RID: 14749 RVA: 0x000DC974 File Offset: 0x000DAB74
		[__DynamicallyInvokable]
		public override IEnumerable<TSource> GetDynamicPartitions()
		{
			IEnumerable<KeyValuePair<long, TSource>> orderableDynamicPartitions = this.GetOrderableDynamicPartitions();
			return new OrderablePartitioner<TSource>.EnumerableDropIndices(orderableDynamicPartitions);
		}

		// Token: 0x02000BCA RID: 3018
		private class EnumerableDropIndices : IEnumerable<!0>, IEnumerable, IDisposable
		{
			// Token: 0x06006E94 RID: 28308 RVA: 0x0017DB13 File Offset: 0x0017BD13
			public EnumerableDropIndices(IEnumerable<KeyValuePair<long, TSource>> source)
			{
				this.m_source = source;
			}

			// Token: 0x06006E95 RID: 28309 RVA: 0x0017DB22 File Offset: 0x0017BD22
			public IEnumerator<TSource> GetEnumerator()
			{
				return new OrderablePartitioner<TSource>.EnumeratorDropIndices(this.m_source.GetEnumerator());
			}

			// Token: 0x06006E96 RID: 28310 RVA: 0x0017DB34 File Offset: 0x0017BD34
			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}

			// Token: 0x06006E97 RID: 28311 RVA: 0x0017DB3C File Offset: 0x0017BD3C
			public void Dispose()
			{
				IDisposable disposable = this.m_source as IDisposable;
				if (disposable != null)
				{
					disposable.Dispose();
				}
			}

			// Token: 0x040035B9 RID: 13753
			private readonly IEnumerable<KeyValuePair<long, TSource>> m_source;
		}

		// Token: 0x02000BCB RID: 3019
		private class EnumeratorDropIndices : IEnumerator<!0>, IDisposable, IEnumerator
		{
			// Token: 0x06006E98 RID: 28312 RVA: 0x0017DB5E File Offset: 0x0017BD5E
			public EnumeratorDropIndices(IEnumerator<KeyValuePair<long, TSource>> source)
			{
				this.m_source = source;
			}

			// Token: 0x06006E99 RID: 28313 RVA: 0x0017DB6D File Offset: 0x0017BD6D
			public bool MoveNext()
			{
				return this.m_source.MoveNext();
			}

			// Token: 0x170012E9 RID: 4841
			// (get) Token: 0x06006E9A RID: 28314 RVA: 0x0017DB7C File Offset: 0x0017BD7C
			public TSource Current
			{
				get
				{
					KeyValuePair<long, TSource> keyValuePair = this.m_source.Current;
					return keyValuePair.Value;
				}
			}

			// Token: 0x170012EA RID: 4842
			// (get) Token: 0x06006E9B RID: 28315 RVA: 0x0017DB9C File Offset: 0x0017BD9C
			object IEnumerator.Current
			{
				get
				{
					return this.Current;
				}
			}

			// Token: 0x06006E9C RID: 28316 RVA: 0x0017DBA9 File Offset: 0x0017BDA9
			public void Dispose()
			{
				this.m_source.Dispose();
			}

			// Token: 0x06006E9D RID: 28317 RVA: 0x0017DBB6 File Offset: 0x0017BDB6
			public void Reset()
			{
				this.m_source.Reset();
			}

			// Token: 0x040035BA RID: 13754
			private readonly IEnumerator<KeyValuePair<long, TSource>> m_source;
		}
	}
}
