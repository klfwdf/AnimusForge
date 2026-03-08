using System;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
	// Token: 0x020004D6 RID: 1238
	[TypeDependency("System.SZArrayHelper")]
	[__DynamicallyInvokable]
	public interface IList<T> : ICollection<!0>, IEnumerable<!0>, IEnumerable
	{
		// Token: 0x170008EC RID: 2284
		[__DynamicallyInvokable]
		T this[int index]
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x06003ADA RID: 15066
		[__DynamicallyInvokable]
		int IndexOf(T item);

		// Token: 0x06003ADB RID: 15067
		[__DynamicallyInvokable]
		void Insert(int index, T item);

		// Token: 0x06003ADC RID: 15068
		[__DynamicallyInvokable]
		void RemoveAt(int index);
	}
}
