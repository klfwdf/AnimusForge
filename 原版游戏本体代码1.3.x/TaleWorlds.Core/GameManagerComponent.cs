using System;

namespace TaleWorlds.Core
{
	// Token: 0x0200006E RID: 110
	public abstract class GameManagerComponent : IEntityComponent
	{
		// Token: 0x170002BC RID: 700
		// (get) Token: 0x060007D5 RID: 2005 RVA: 0x0001A26C File Offset: 0x0001846C
		// (set) Token: 0x060007D6 RID: 2006 RVA: 0x0001A274 File Offset: 0x00018474
		public GameManagerBase GameManager { get; internal set; }

		// Token: 0x060007D7 RID: 2007 RVA: 0x0001A27D File Offset: 0x0001847D
		void IEntityComponent.OnInitialize()
		{
			this.OnInitialize();
		}

		// Token: 0x060007D8 RID: 2008 RVA: 0x0001A285 File Offset: 0x00018485
		protected virtual void OnInitialize()
		{
		}

		// Token: 0x060007D9 RID: 2009 RVA: 0x0001A287 File Offset: 0x00018487
		void IEntityComponent.OnFinalize()
		{
			this.OnFinalize();
		}

		// Token: 0x060007DA RID: 2010 RVA: 0x0001A28F File Offset: 0x0001848F
		protected virtual void OnFinalize()
		{
		}

		// Token: 0x060007DB RID: 2011 RVA: 0x0001A291 File Offset: 0x00018491
		protected internal virtual void OnTick()
		{
		}

		// Token: 0x060007DC RID: 2012 RVA: 0x0001A293 File Offset: 0x00018493
		protected internal virtual void OnPlayerDisconnect(VirtualPlayer peer)
		{
		}

		// Token: 0x060007DD RID: 2013 RVA: 0x0001A295 File Offset: 0x00018495
		protected internal virtual void OnEarlyPlayerConnect(VirtualPlayer peer)
		{
		}

		// Token: 0x060007DE RID: 2014 RVA: 0x0001A297 File Offset: 0x00018497
		protected internal virtual void OnPlayerConnect(VirtualPlayer peer)
		{
		}

		// Token: 0x060007DF RID: 2015 RVA: 0x0001A299 File Offset: 0x00018499
		protected internal virtual void OnGameNetworkBegin()
		{
		}

		// Token: 0x060007E0 RID: 2016 RVA: 0x0001A29B File Offset: 0x0001849B
		protected internal virtual void OnGameNetworkEnd()
		{
		}
	}
}
