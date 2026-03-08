using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000082 RID: 130
	public interface ICommunicator
	{
		// Token: 0x170002E5 RID: 741
		// (get) Token: 0x06000877 RID: 2167
		VirtualPlayer VirtualPlayer { get; }

		// Token: 0x06000878 RID: 2168
		void OnSynchronizeComponentTo(VirtualPlayer peer, PeerComponent component);

		// Token: 0x06000879 RID: 2169
		void OnAddComponent(PeerComponent component);

		// Token: 0x0600087A RID: 2170
		void OnRemoveComponent(PeerComponent component);

		// Token: 0x170002E6 RID: 742
		// (get) Token: 0x0600087B RID: 2171
		bool IsNetworkActive { get; }

		// Token: 0x170002E7 RID: 743
		// (get) Token: 0x0600087C RID: 2172
		bool IsConnectionActive { get; }

		// Token: 0x170002E8 RID: 744
		// (get) Token: 0x0600087D RID: 2173
		bool IsServerPeer { get; }

		// Token: 0x170002E9 RID: 745
		// (get) Token: 0x0600087E RID: 2174
		// (set) Token: 0x0600087F RID: 2175
		bool IsSynchronized { get; set; }
	}
}
