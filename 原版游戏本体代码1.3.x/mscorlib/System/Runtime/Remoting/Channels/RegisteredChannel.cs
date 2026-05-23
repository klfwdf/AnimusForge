using System;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000829 RID: 2089
	internal class RegisteredChannel
	{
		// Token: 0x0600598D RID: 22925 RVA: 0x0013BF70 File Offset: 0x0013A170
		internal RegisteredChannel(IChannel chnl)
		{
			this.channel = chnl;
			this.flags = 0;
			if (chnl is IChannelSender)
			{
				this.flags |= 1;
			}
			if (chnl is IChannelReceiver)
			{
				this.flags |= 2;
			}
		}

		// Token: 0x17000ED9 RID: 3801
		// (get) Token: 0x0600598E RID: 22926 RVA: 0x0013BFBF File Offset: 0x0013A1BF
		internal virtual IChannel Channel
		{
			get
			{
				return this.channel;
			}
		}

		// Token: 0x0600598F RID: 22927 RVA: 0x0013BFC7 File Offset: 0x0013A1C7
		internal virtual bool IsSender()
		{
			return (this.flags & 1) > 0;
		}

		// Token: 0x06005990 RID: 22928 RVA: 0x0013BFD4 File Offset: 0x0013A1D4
		internal virtual bool IsReceiver()
		{
			return (this.flags & 2) > 0;
		}

		// Token: 0x040028CA RID: 10442
		private IChannel channel;

		// Token: 0x040028CB RID: 10443
		private byte flags;

		// Token: 0x040028CC RID: 10444
		private const byte SENDER = 1;

		// Token: 0x040028CD RID: 10445
		private const byte RECEIVER = 2;
	}
}
