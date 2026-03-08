using System;

namespace System.Collections.Generic
{
	// Token: 0x020004D5 RID: 1237
	[__DynamicallyInvokable]
	public interface IEqualityComparer<in T>
	{
		// Token: 0x06003AD6 RID: 15062
		[__DynamicallyInvokable]
		bool Equals(T x, T y);

		// Token: 0x06003AD7 RID: 15063
		[__DynamicallyInvokable]
		int GetHashCode(T obj);
	}
}
