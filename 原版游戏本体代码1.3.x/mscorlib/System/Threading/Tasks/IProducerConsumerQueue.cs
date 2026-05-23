using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Threading.Tasks
{
	// Token: 0x02000582 RID: 1410
	internal interface IProducerConsumerQueue<T> : IEnumerable<!0>, IEnumerable
	{
		// Token: 0x06004262 RID: 16994
		void Enqueue(T item);

		// Token: 0x06004263 RID: 16995
		bool TryDequeue(out T result);

		// Token: 0x170009DF RID: 2527
		// (get) Token: 0x06004264 RID: 16996
		bool IsEmpty { get; }

		// Token: 0x170009E0 RID: 2528
		// (get) Token: 0x06004265 RID: 16997
		int Count { get; }

		// Token: 0x06004266 RID: 16998
		int GetCountSafe(object syncObj);
	}
}
