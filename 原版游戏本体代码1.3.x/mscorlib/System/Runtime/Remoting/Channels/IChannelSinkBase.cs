using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000848 RID: 2120
	[ComVisible(true)]
	public interface IChannelSinkBase
	{
		// Token: 0x17000EFA RID: 3834
		// (get) Token: 0x06005A23 RID: 23075
		IDictionary Properties
		{
			[SecurityCritical]
			get;
		}
	}
}
