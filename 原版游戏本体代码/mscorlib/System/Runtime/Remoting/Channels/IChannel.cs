using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x0200083D RID: 2109
	[ComVisible(true)]
	public interface IChannel
	{
		// Token: 0x17000EF0 RID: 3824
		// (get) Token: 0x06005A07 RID: 23047
		int ChannelPriority
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000EF1 RID: 3825
		// (get) Token: 0x06005A08 RID: 23048
		string ChannelName
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x06005A09 RID: 23049
		[SecurityCritical]
		string Parse(string url, out string objectURI);
	}
}
