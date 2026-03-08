using System;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
	// Token: 0x020004D7 RID: 1239
	[TypeDependency("System.SZArrayHelper")]
	[__DynamicallyInvokable]
	public interface IReadOnlyCollection<out T> : IEnumerable<!0>, IEnumerable
	{
		// Token: 0x170008ED RID: 2285
		// (get) Token: 0x06003ADD RID: 15069
		[__DynamicallyInvokable]
		int Count
		{
			[__DynamicallyInvokable]
			get;
		}
	}
}
