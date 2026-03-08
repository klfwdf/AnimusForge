using System;
using System.Collections.Generic;

namespace System.Collections.Concurrent
{
	// Token: 0x020004AB RID: 1195
	[__DynamicallyInvokable]
	public interface IProducerConsumerCollection<T> : IEnumerable<!0>, IEnumerable, ICollection
	{
		// Token: 0x06003923 RID: 14627
		[__DynamicallyInvokable]
		void CopyTo(T[] array, int index);

		// Token: 0x06003924 RID: 14628
		[__DynamicallyInvokable]
		bool TryAdd(T item);

		// Token: 0x06003925 RID: 14629
		[__DynamicallyInvokable]
		bool TryTake(out T item);

		// Token: 0x06003926 RID: 14630
		[__DynamicallyInvokable]
		T[] ToArray();
	}
}
