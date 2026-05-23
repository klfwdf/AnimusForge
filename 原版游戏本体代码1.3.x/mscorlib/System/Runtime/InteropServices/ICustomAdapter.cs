using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000964 RID: 2404
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public interface ICustomAdapter
	{
		// Token: 0x06006226 RID: 25126
		[__DynamicallyInvokable]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		object GetUnderlyingObject();
	}
}
