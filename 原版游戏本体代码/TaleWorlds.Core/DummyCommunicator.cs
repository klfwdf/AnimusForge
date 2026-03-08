using System;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.Core
{
	// Token: 0x02000056 RID: 86
	public class DummyCommunicator : ICommunicator
	{
		// Token: 0x17000292 RID: 658
		// (get) Token: 0x060006E8 RID: 1768 RVA: 0x00018245 File Offset: 0x00016445
		public VirtualPlayer VirtualPlayer { get; }

		// Token: 0x060006E9 RID: 1769 RVA: 0x0001824D File Offset: 0x0001644D
		public void OnSynchronizeComponentTo(VirtualPlayer peer, PeerComponent component)
		{
		}

		// Token: 0x060006EA RID: 1770 RVA: 0x0001824F File Offset: 0x0001644F
		public void OnAddComponent(PeerComponent component)
		{
		}

		// Token: 0x060006EB RID: 1771 RVA: 0x00018251 File Offset: 0x00016451
		public void OnRemoveComponent(PeerComponent component)
		{
		}

		// Token: 0x17000293 RID: 659
		// (get) Token: 0x060006EC RID: 1772 RVA: 0x00018253 File Offset: 0x00016453
		public bool IsNetworkActive
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000294 RID: 660
		// (get) Token: 0x060006ED RID: 1773 RVA: 0x00018256 File Offset: 0x00016456
		public bool IsConnectionActive
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000295 RID: 661
		// (get) Token: 0x060006EE RID: 1774 RVA: 0x00018259 File Offset: 0x00016459
		public bool IsServerPeer
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000296 RID: 662
		// (get) Token: 0x060006EF RID: 1775 RVA: 0x0001825C File Offset: 0x0001645C
		// (set) Token: 0x060006F0 RID: 1776 RVA: 0x0001825F File Offset: 0x0001645F
		public bool IsSynchronized
		{
			get
			{
				return true;
			}
			set
			{
			}
		}

		// Token: 0x060006F1 RID: 1777 RVA: 0x00018261 File Offset: 0x00016461
		private DummyCommunicator(int index, string name)
		{
			this.VirtualPlayer = new VirtualPlayer(index, name, PlayerId.Empty, this);
		}

		// Token: 0x060006F2 RID: 1778 RVA: 0x0001827C File Offset: 0x0001647C
		public static DummyCommunicator CreateAsServer(int index, string name)
		{
			return new DummyCommunicator(index, name);
		}

		// Token: 0x060006F3 RID: 1779 RVA: 0x00018285 File Offset: 0x00016485
		public static DummyCommunicator CreateAsClient(string name, int index)
		{
			return new DummyCommunicator(index, name);
		}
	}
}
