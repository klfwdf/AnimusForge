using System;

namespace System.Collections
{
	// Token: 0x020004A5 RID: 1189
	[__DynamicallyInvokable]
	public interface IStructuralEquatable
	{
		// Token: 0x060038FA RID: 14586
		[__DynamicallyInvokable]
		bool Equals(object other, IEqualityComparer comparer);

		// Token: 0x060038FB RID: 14587
		[__DynamicallyInvokable]
		int GetHashCode(IEqualityComparer comparer);
	}
}
