using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x0200083F RID: 2111
	[ComVisible(true)]
	public interface IChannelReceiver : IChannel
	{
		// Token: 0x17000EF2 RID: 3826
		// (get) Token: 0x06005A0B RID: 23051
		object ChannelData
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x06005A0C RID: 23052
		[SecurityCritical]
		string[] GetUrlsForUri(string objectURI);

		// Token: 0x06005A0D RID: 23053
		[SecurityCritical]
		void StartListening(object data);

		// Token: 0x06005A0E RID: 23054
		[SecurityCritical]
		void StopListening(object data);
	}
}
