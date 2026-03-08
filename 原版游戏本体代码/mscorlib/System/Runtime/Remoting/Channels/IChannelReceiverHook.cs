using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000840 RID: 2112
	[ComVisible(true)]
	public interface IChannelReceiverHook
	{
		// Token: 0x17000EF3 RID: 3827
		// (get) Token: 0x06005A0F RID: 23055
		string ChannelScheme
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000EF4 RID: 3828
		// (get) Token: 0x06005A10 RID: 23056
		bool WantsToListen
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000EF5 RID: 3829
		// (get) Token: 0x06005A11 RID: 23057
		IServerChannelSink ChannelSinkChain
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x06005A12 RID: 23058
		[SecurityCritical]
		void AddHookChannelUri(string channelUri);
	}
}
