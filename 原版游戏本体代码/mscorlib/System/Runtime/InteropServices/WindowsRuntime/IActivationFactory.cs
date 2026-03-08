using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009E2 RID: 2530
	[Guid("00000035-0000-0000-C000-000000000046")]
	[__DynamicallyInvokable]
	[ComImport]
	public interface IActivationFactory
	{
		// Token: 0x06006481 RID: 25729
		[__DynamicallyInvokable]
		object ActivateInstance();
	}
}
