using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x0200084B RID: 2123
	[SecurityCritical]
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.Infrastructure)]
	[Serializable]
	public class ChannelDataStore : IChannelDataStore
	{
		// Token: 0x06005A27 RID: 23079 RVA: 0x0013D60C File Offset: 0x0013B80C
		private ChannelDataStore(string[] channelUrls, DictionaryEntry[] extraData)
		{
			this._channelURIs = channelUrls;
			this._extraData = extraData;
		}

		// Token: 0x06005A28 RID: 23080 RVA: 0x0013D622 File Offset: 0x0013B822
		public ChannelDataStore(string[] channelURIs)
		{
			this._channelURIs = channelURIs;
			this._extraData = null;
		}

		// Token: 0x06005A29 RID: 23081 RVA: 0x0013D638 File Offset: 0x0013B838
		[SecurityCritical]
		internal ChannelDataStore InternalShallowCopy()
		{
			return new ChannelDataStore(this._channelURIs, this._extraData);
		}

		// Token: 0x17000EFD RID: 3837
		// (get) Token: 0x06005A2A RID: 23082 RVA: 0x0013D64B File Offset: 0x0013B84B
		// (set) Token: 0x06005A2B RID: 23083 RVA: 0x0013D653 File Offset: 0x0013B853
		public string[] ChannelUris
		{
			[SecurityCritical]
			get
			{
				return this._channelURIs;
			}
			set
			{
				this._channelURIs = value;
			}
		}

		// Token: 0x17000EFE RID: 3838
		public object this[object key]
		{
			[SecurityCritical]
			get
			{
				foreach (DictionaryEntry dictionaryEntry in this._extraData)
				{
					if (dictionaryEntry.Key.Equals(key))
					{
						return dictionaryEntry.Value;
					}
				}
				return null;
			}
			[SecurityCritical]
			set
			{
				if (this._extraData == null)
				{
					this._extraData = new DictionaryEntry[1];
					this._extraData[0] = new DictionaryEntry(key, value);
					return;
				}
				int num = this._extraData.Length;
				DictionaryEntry[] array = new DictionaryEntry[num + 1];
				int i;
				for (i = 0; i < num; i++)
				{
					array[i] = this._extraData[i];
				}
				array[i] = new DictionaryEntry(key, value);
				this._extraData = array;
			}
		}

		// Token: 0x040028FC RID: 10492
		private string[] _channelURIs;

		// Token: 0x040028FD RID: 10493
		private DictionaryEntry[] _extraData;
	}
}
