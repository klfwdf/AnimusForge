using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting
{
	// Token: 0x020007AB RID: 1963
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("C460E2B4-E199-412a-8456-84DC3E4838C3")]
	[ComVisible(true)]
	public interface IObjectHandle
	{
		// Token: 0x0600550D RID: 21773
		object Unwrap();
	}
}
