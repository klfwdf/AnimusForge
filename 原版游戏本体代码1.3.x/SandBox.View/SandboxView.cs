using System;
using TaleWorlds.ScreenSystem;

namespace SandBox.View
{
	// Token: 0x02000007 RID: 7
	public abstract class SandboxView
	{
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000013 RID: 19 RVA: 0x0000296C File Offset: 0x00000B6C
		// (set) Token: 0x06000014 RID: 20 RVA: 0x00002974 File Offset: 0x00000B74
		public bool IsFinalized { get; protected set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000015 RID: 21 RVA: 0x0000297D File Offset: 0x00000B7D
		// (set) Token: 0x06000016 RID: 22 RVA: 0x00002985 File Offset: 0x00000B85
		public ScreenLayer Layer { get; protected set; }

		// Token: 0x06000017 RID: 23 RVA: 0x0000298E File Offset: 0x00000B8E
		protected internal virtual void OnActivate()
		{
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002990 File Offset: 0x00000B90
		protected internal virtual void OnDeactivate()
		{
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002992 File Offset: 0x00000B92
		protected internal virtual void OnInitialize()
		{
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002994 File Offset: 0x00000B94
		protected internal virtual void OnFinalize()
		{
			this.IsFinalized = true;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x0000299D File Offset: 0x00000B9D
		protected internal virtual void OnFrameTick(float dt)
		{
		}
	}
}
