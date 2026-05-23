using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000B0 RID: 176
	public static class MBNetwork
	{
		// Token: 0x1700031F RID: 799
		// (get) Token: 0x06000945 RID: 2373 RVA: 0x0001E460 File Offset: 0x0001C660
		// (set) Token: 0x06000946 RID: 2374 RVA: 0x0001E467 File Offset: 0x0001C667
		public static INetworkCommunication NetworkViewCommunication { get; private set; }

		// Token: 0x06000947 RID: 2375 RVA: 0x0001E46F File Offset: 0x0001C66F
		public static void Initialize(INetworkCommunication networkCommunication)
		{
			MBNetwork.NetworkViewCommunication = networkCommunication;
		}

		// Token: 0x17000320 RID: 800
		// (get) Token: 0x06000948 RID: 2376 RVA: 0x0001E477 File Offset: 0x0001C677
		public static VirtualPlayer MyPeer
		{
			get
			{
				if (MBNetwork.NetworkViewCommunication != null)
				{
					return MBNetwork.NetworkViewCommunication.MyPeer;
				}
				return null;
			}
		}
	}
}
