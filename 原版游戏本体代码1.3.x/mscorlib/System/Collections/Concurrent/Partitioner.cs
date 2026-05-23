using System;
using System.Collections.Generic;
using System.Security.Permissions;

namespace System.Collections.Concurrent
{
	// Token: 0x020004B1 RID: 1201
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public abstract class Partitioner<TSource>
	{
		// Token: 0x0600398F RID: 14735
		[__DynamicallyInvokable]
		public abstract IList<IEnumerator<TSource>> GetPartitions(int partitionCount);

		// Token: 0x170008A2 RID: 2210
		// (get) Token: 0x06003990 RID: 14736 RVA: 0x000DC8AA File Offset: 0x000DAAAA
		[__DynamicallyInvokable]
		public virtual bool SupportsDynamicPartitions
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x06003991 RID: 14737 RVA: 0x000DC8AD File Offset: 0x000DAAAD
		[__DynamicallyInvokable]
		public virtual IEnumerable<TSource> GetDynamicPartitions()
		{
			throw new NotSupportedException(Environment.GetResourceString("Partitioner_DynamicPartitionsNotSupported"));
		}

		// Token: 0x06003992 RID: 14738 RVA: 0x000DC8BE File Offset: 0x000DAABE
		[__DynamicallyInvokable]
		protected Partitioner()
		{
		}
	}
}
