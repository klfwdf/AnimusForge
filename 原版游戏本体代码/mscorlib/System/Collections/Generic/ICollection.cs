using System;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
	// Token: 0x020004D0 RID: 1232
	[TypeDependency("System.SZArrayHelper")]
	[__DynamicallyInvokable]
	public interface ICollection<T> : IEnumerable<!0>, IEnumerable
	{
		// Token: 0x170008E6 RID: 2278
		// (get) Token: 0x06003AC4 RID: 15044
		[__DynamicallyInvokable]
		int Count
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x170008E7 RID: 2279
		// (get) Token: 0x06003AC5 RID: 15045
		[__DynamicallyInvokable]
		bool IsReadOnly
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x06003AC6 RID: 15046
		[__DynamicallyInvokable]
		void Add(T item);

		// Token: 0x06003AC7 RID: 15047
		[__DynamicallyInvokable]
		void Clear();

		// Token: 0x06003AC8 RID: 15048
		[__DynamicallyInvokable]
		bool Contains(T item);

		// Token: 0x06003AC9 RID: 15049
		[__DynamicallyInvokable]
		void CopyTo(T[] array, int arrayIndex);

		// Token: 0x06003ACA RID: 15050
		[__DynamicallyInvokable]
		bool Remove(T item);
	}
}
