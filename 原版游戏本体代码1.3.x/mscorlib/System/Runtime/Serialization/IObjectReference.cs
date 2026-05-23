using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Serialization
{
	// Token: 0x02000734 RID: 1844
	[ComVisible(true)]
	public interface IObjectReference
	{
		// Token: 0x060051C0 RID: 20928
		[SecurityCritical]
		object GetRealObject(StreamingContext context);
	}
}
