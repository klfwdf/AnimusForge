using System;

namespace System
{
	// Token: 0x02000059 RID: 89
	[__DynamicallyInvokable]
	public interface IComparable<in T>
	{
		// Token: 0x06000334 RID: 820
		[__DynamicallyInvokable]
		int CompareTo(T other);
	}
}
