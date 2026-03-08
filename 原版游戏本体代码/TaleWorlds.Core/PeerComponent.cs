using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000C3 RID: 195
	public abstract class PeerComponent : IEntityComponent
	{
		// Token: 0x170003B5 RID: 949
		// (get) Token: 0x06000ABF RID: 2751 RVA: 0x00022AF4 File Offset: 0x00020CF4
		// (set) Token: 0x06000AC0 RID: 2752 RVA: 0x00022AFC File Offset: 0x00020CFC
		public VirtualPlayer Peer
		{
			get
			{
				return this._peer;
			}
			set
			{
				this._peer = value;
			}
		}

		// Token: 0x06000AC1 RID: 2753 RVA: 0x00022B05 File Offset: 0x00020D05
		public virtual void Initialize()
		{
		}

		// Token: 0x170003B6 RID: 950
		// (get) Token: 0x06000AC2 RID: 2754 RVA: 0x00022B07 File Offset: 0x00020D07
		public string Name
		{
			get
			{
				return this.Peer.UserName;
			}
		}

		// Token: 0x170003B7 RID: 951
		// (get) Token: 0x06000AC3 RID: 2755 RVA: 0x00022B14 File Offset: 0x00020D14
		public bool IsMine
		{
			get
			{
				return this.Peer.IsMine;
			}
		}

		// Token: 0x06000AC4 RID: 2756 RVA: 0x00022B21 File Offset: 0x00020D21
		public T GetComponent<T>() where T : PeerComponent
		{
			return this.Peer.GetComponent<T>();
		}

		// Token: 0x06000AC5 RID: 2757 RVA: 0x00022B2E File Offset: 0x00020D2E
		public virtual void OnInitialize()
		{
		}

		// Token: 0x06000AC6 RID: 2758 RVA: 0x00022B30 File Offset: 0x00020D30
		public virtual void OnFinalize()
		{
		}

		// Token: 0x170003B8 RID: 952
		// (get) Token: 0x06000AC7 RID: 2759 RVA: 0x00022B32 File Offset: 0x00020D32
		// (set) Token: 0x06000AC8 RID: 2760 RVA: 0x00022B3A File Offset: 0x00020D3A
		public uint TypeId { get; set; }

		// Token: 0x04000604 RID: 1540
		private VirtualPlayer _peer;
	}
}
