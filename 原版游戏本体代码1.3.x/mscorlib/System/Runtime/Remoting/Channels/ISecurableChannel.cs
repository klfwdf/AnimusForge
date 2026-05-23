using System;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000854 RID: 2132
	public interface ISecurableChannel
	{
		// Token: 0x17000F1A RID: 3866
		// (get) Token: 0x06005A66 RID: 23142
		// (set) Token: 0x06005A67 RID: 23143
		bool IsSecured
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}
	}
}
