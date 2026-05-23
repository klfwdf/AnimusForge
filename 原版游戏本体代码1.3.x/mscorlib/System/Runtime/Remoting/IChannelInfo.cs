using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting
{
	// Token: 0x020007B6 RID: 1974
	[ComVisible(true)]
	public interface IChannelInfo
	{
		// Token: 0x17000E16 RID: 3606
		// (get) Token: 0x06005588 RID: 21896
		// (set) Token: 0x06005589 RID: 21897
		object[] ChannelData
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}
	}
}
