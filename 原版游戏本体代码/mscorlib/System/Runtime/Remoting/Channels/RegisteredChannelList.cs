using System;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x0200082A RID: 2090
	internal class RegisteredChannelList
	{
		// Token: 0x06005991 RID: 22929 RVA: 0x0013BFE1 File Offset: 0x0013A1E1
		internal RegisteredChannelList()
		{
			this._channels = new RegisteredChannel[0];
		}

		// Token: 0x06005992 RID: 22930 RVA: 0x0013BFF5 File Offset: 0x0013A1F5
		internal RegisteredChannelList(RegisteredChannel[] channels)
		{
			this._channels = channels;
		}

		// Token: 0x17000EDA RID: 3802
		// (get) Token: 0x06005993 RID: 22931 RVA: 0x0013C004 File Offset: 0x0013A204
		internal RegisteredChannel[] RegisteredChannels
		{
			get
			{
				return this._channels;
			}
		}

		// Token: 0x17000EDB RID: 3803
		// (get) Token: 0x06005994 RID: 22932 RVA: 0x0013C00C File Offset: 0x0013A20C
		internal int Count
		{
			get
			{
				if (this._channels == null)
				{
					return 0;
				}
				return this._channels.Length;
			}
		}

		// Token: 0x06005995 RID: 22933 RVA: 0x0013C020 File Offset: 0x0013A220
		internal IChannel GetChannel(int index)
		{
			return this._channels[index].Channel;
		}

		// Token: 0x06005996 RID: 22934 RVA: 0x0013C02F File Offset: 0x0013A22F
		internal bool IsSender(int index)
		{
			return this._channels[index].IsSender();
		}

		// Token: 0x06005997 RID: 22935 RVA: 0x0013C03E File Offset: 0x0013A23E
		internal bool IsReceiver(int index)
		{
			return this._channels[index].IsReceiver();
		}

		// Token: 0x17000EDC RID: 3804
		// (get) Token: 0x06005998 RID: 22936 RVA: 0x0013C050 File Offset: 0x0013A250
		internal int ReceiverCount
		{
			get
			{
				if (this._channels == null)
				{
					return 0;
				}
				int num = 0;
				for (int i = 0; i < this._channels.Length; i++)
				{
					if (this.IsReceiver(i))
					{
						num++;
					}
				}
				return num;
			}
		}

		// Token: 0x06005999 RID: 22937 RVA: 0x0013C08C File Offset: 0x0013A28C
		internal int FindChannelIndex(IChannel channel)
		{
			for (int i = 0; i < this._channels.Length; i++)
			{
				if (channel == this.GetChannel(i))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x0600599A RID: 22938 RVA: 0x0013C0BC File Offset: 0x0013A2BC
		[SecurityCritical]
		internal int FindChannelIndex(string name)
		{
			for (int i = 0; i < this._channels.Length; i++)
			{
				if (string.Compare(name, this.GetChannel(i).ChannelName, StringComparison.OrdinalIgnoreCase) == 0)
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x040028CE RID: 10446
		private RegisteredChannel[] _channels;
	}
}
