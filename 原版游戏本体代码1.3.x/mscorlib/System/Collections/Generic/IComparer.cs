using System;

namespace System.Collections.Generic
{
	// Token: 0x020004D1 RID: 1233
	[__DynamicallyInvokable]
	public interface IComparer<in T>
	{
		// Token: 0x06003ACB RID: 15051
		[__DynamicallyInvokable]
		int Compare(T x, T y);
	}
}
