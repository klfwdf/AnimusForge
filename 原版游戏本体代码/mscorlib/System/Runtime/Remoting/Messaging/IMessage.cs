using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000857 RID: 2135
	[ComVisible(true)]
	public interface IMessage
	{
		// Token: 0x17000F24 RID: 3876
		// (get) Token: 0x06005A7D RID: 23165
		IDictionary Properties
		{
			[SecurityCritical]
			get;
		}
	}
}
