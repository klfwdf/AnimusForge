using System;

namespace System
{
	// Token: 0x02000104 RID: 260
	[__DynamicallyInvokable]
	public interface IServiceProvider
	{
		// Token: 0x06000FD0 RID: 4048
		[__DynamicallyInvokable]
		object GetService(Type serviceType);
	}
}
