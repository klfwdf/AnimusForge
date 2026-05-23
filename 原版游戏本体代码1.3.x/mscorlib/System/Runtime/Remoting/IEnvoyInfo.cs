using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting
{
	// Token: 0x020007B7 RID: 1975
	[ComVisible(true)]
	public interface IEnvoyInfo
	{
		// Token: 0x17000E17 RID: 3607
		// (get) Token: 0x0600558A RID: 21898
		// (set) Token: 0x0600558B RID: 21899
		IMessageSink EnvoySinks
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}
	}
}
