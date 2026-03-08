using System;
using System.Runtime.Remoting.Channels;
using System.Security;

namespace System.Runtime.Remoting
{
	// Token: 0x020007BA RID: 1978
	[Serializable]
	internal sealed class ChannelInfo : IChannelInfo
	{
		// Token: 0x0600559C RID: 21916 RVA: 0x0013013E File Offset: 0x0012E33E
		[SecurityCritical]
		internal ChannelInfo()
		{
			this.ChannelData = ChannelServices.CurrentChannelData;
		}

		// Token: 0x17000E1C RID: 3612
		// (get) Token: 0x0600559D RID: 21917 RVA: 0x00130151 File Offset: 0x0012E351
		// (set) Token: 0x0600559E RID: 21918 RVA: 0x00130159 File Offset: 0x0012E359
		public object[] ChannelData
		{
			[SecurityCritical]
			get
			{
				return this.channelData;
			}
			[SecurityCritical]
			set
			{
				this.channelData = value;
			}
		}

		// Token: 0x04002769 RID: 10089
		private object[] channelData;
	}
}
