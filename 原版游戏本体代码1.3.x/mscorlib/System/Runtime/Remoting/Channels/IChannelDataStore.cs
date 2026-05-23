using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x0200084A RID: 2122
	[ComVisible(true)]
	public interface IChannelDataStore
	{
		// Token: 0x17000EFB RID: 3835
		// (get) Token: 0x06005A24 RID: 23076
		string[] ChannelUris
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000EFC RID: 3836
		object this[object key]
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}
	}
}
