using System;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x0200083B RID: 2107
	internal class DispatchChannelSinkProvider : IServerChannelSinkProvider
	{
		// Token: 0x060059FC RID: 23036 RVA: 0x0013D596 File Offset: 0x0013B796
		internal DispatchChannelSinkProvider()
		{
		}

		// Token: 0x060059FD RID: 23037 RVA: 0x0013D59E File Offset: 0x0013B79E
		[SecurityCritical]
		public void GetChannelData(IChannelDataStore channelData)
		{
		}

		// Token: 0x060059FE RID: 23038 RVA: 0x0013D5A0 File Offset: 0x0013B7A0
		[SecurityCritical]
		public IServerChannelSink CreateSink(IChannelReceiver channel)
		{
			return new DispatchChannelSink();
		}

		// Token: 0x17000EED RID: 3821
		// (get) Token: 0x060059FF RID: 23039 RVA: 0x0013D5A7 File Offset: 0x0013B7A7
		// (set) Token: 0x06005A00 RID: 23040 RVA: 0x0013D5AA File Offset: 0x0013B7AA
		public IServerChannelSinkProvider Next
		{
			[SecurityCritical]
			get
			{
				return null;
			}
			[SecurityCritical]
			set
			{
				throw new NotSupportedException();
			}
		}
	}
}
