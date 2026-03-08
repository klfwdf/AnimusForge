using System;

namespace System
{
	// Token: 0x020000F4 RID: 244
	[__DynamicallyInvokable]
	public interface IProgress<in T>
	{
		// Token: 0x06000F09 RID: 3849
		[__DynamicallyInvokable]
		void Report(T value);
	}
}
