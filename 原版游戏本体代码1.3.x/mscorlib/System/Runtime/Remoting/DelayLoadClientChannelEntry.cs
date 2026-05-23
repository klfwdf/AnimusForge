using System;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting.Channels;
using System.Security;

namespace System.Runtime.Remoting
{
	// Token: 0x020007B0 RID: 1968
	internal class DelayLoadClientChannelEntry
	{
		// Token: 0x06005543 RID: 21827 RVA: 0x0012F0B2 File Offset: 0x0012D2B2
		internal DelayLoadClientChannelEntry(RemotingXmlConfigFileData.ChannelEntry entry, bool ensureSecurity)
		{
			this._entry = entry;
			this._channel = null;
			this._bRegistered = false;
			this._ensureSecurity = ensureSecurity;
		}

		// Token: 0x17000E03 RID: 3587
		// (get) Token: 0x06005544 RID: 21828 RVA: 0x0012F0D6 File Offset: 0x0012D2D6
		internal IChannelSender Channel
		{
			[SecurityCritical]
			get
			{
				if (this._channel == null && !this._bRegistered)
				{
					this._channel = (IChannelSender)RemotingConfigHandler.CreateChannelFromConfigEntry(this._entry);
					this._entry = null;
				}
				return this._channel;
			}
		}

		// Token: 0x06005545 RID: 21829 RVA: 0x0012F10B File Offset: 0x0012D30B
		internal void RegisterChannel()
		{
			ChannelServices.RegisterChannel(this._channel, this._ensureSecurity);
			this._bRegistered = true;
			this._channel = null;
		}

		// Token: 0x0400273F RID: 10047
		private RemotingXmlConfigFileData.ChannelEntry _entry;

		// Token: 0x04002740 RID: 10048
		private IChannelSender _channel;

		// Token: 0x04002741 RID: 10049
		private bool _bRegistered;

		// Token: 0x04002742 RID: 10050
		private bool _ensureSecurity;
	}
}
