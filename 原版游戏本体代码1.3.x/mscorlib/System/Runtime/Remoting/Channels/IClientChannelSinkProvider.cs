using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000841 RID: 2113
	[ComVisible(true)]
	public interface IClientChannelSinkProvider
	{
		// Token: 0x06005A13 RID: 23059
		[SecurityCritical]
		IClientChannelSink CreateSink(IChannelSender channel, string url, object remoteChannelData);

		// Token: 0x17000EF6 RID: 3830
		// (get) Token: 0x06005A14 RID: 23060
		// (set) Token: 0x06005A15 RID: 23061
		IClientChannelSinkProvider Next
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}
	}
}
