using System;
using System.Runtime.InteropServices;

namespace System.Collections
{
	// Token: 0x020004A0 RID: 1184
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public interface IEqualityComparer
	{
		// Token: 0x060038BF RID: 14527
		[__DynamicallyInvokable]
		bool Equals(object x, object y);

		// Token: 0x060038C0 RID: 14528
		[__DynamicallyInvokable]
		int GetHashCode(object obj);
	}
}
