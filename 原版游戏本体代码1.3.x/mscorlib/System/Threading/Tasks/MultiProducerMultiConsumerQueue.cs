using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Threading.Tasks
{
	// Token: 0x02000583 RID: 1411
	[DebuggerDisplay("Count = {Count}")]
	internal sealed class MultiProducerMultiConsumerQueue<T> : ConcurrentQueue<T>, IProducerConsumerQueue<T>, IEnumerable<!0>, IEnumerable
	{
		// Token: 0x06004267 RID: 16999 RVA: 0x000F733B File Offset: 0x000F553B
		void IProducerConsumerQueue<!0>.Enqueue(T item)
		{
			base.Enqueue(item);
		}

		// Token: 0x06004268 RID: 17000 RVA: 0x000F7344 File Offset: 0x000F5544
		bool IProducerConsumerQueue<!0>.TryDequeue(out T result)
		{
			return base.TryDequeue(out result);
		}

		// Token: 0x170009E1 RID: 2529
		// (get) Token: 0x06004269 RID: 17001 RVA: 0x000F734D File Offset: 0x000F554D
		bool IProducerConsumerQueue<!0>.IsEmpty
		{
			get
			{
				return base.IsEmpty;
			}
		}

		// Token: 0x170009E2 RID: 2530
		// (get) Token: 0x0600426A RID: 17002 RVA: 0x000F7355 File Offset: 0x000F5555
		int IProducerConsumerQueue<!0>.Count
		{
			get
			{
				return base.Count;
			}
		}

		// Token: 0x0600426B RID: 17003 RVA: 0x000F735D File Offset: 0x000F555D
		int IProducerConsumerQueue<!0>.GetCountSafe(object syncObj)
		{
			return base.Count;
		}
	}
}
