using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000842 RID: 2114
	[ComVisible(true)]
	public interface IServerChannelSinkProvider
	{
		// Token: 0x06005A16 RID: 23062
		[SecurityCritical]
		void GetChannelData(IChannelDataStore channelData);

		// Token: 0x06005A17 RID: 23063
		[SecurityCritical]
		IServerChannelSink CreateSink(IChannelReceiver channel);

		// Token: 0x17000EF7 RID: 3831
		// (get) Token: 0x06005A18 RID: 23064
		// (set) Token: 0x06005A19 RID: 23065
		IServerChannelSinkProvider Next
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}
	}
}
