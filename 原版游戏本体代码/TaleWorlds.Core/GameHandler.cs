using System;

namespace TaleWorlds.Core
{
	// Token: 0x0200006A RID: 106
	public abstract class GameHandler : IEntityComponent
	{
		// Token: 0x06000799 RID: 1945 RVA: 0x00019DBD File Offset: 0x00017FBD
		void IEntityComponent.OnInitialize()
		{
			this.OnInitialize();
		}

		// Token: 0x0600079A RID: 1946 RVA: 0x00019DC5 File Offset: 0x00017FC5
		void IEntityComponent.OnFinalize()
		{
			this.OnFinalize();
		}

		// Token: 0x0600079B RID: 1947 RVA: 0x00019DCD File Offset: 0x00017FCD
		protected virtual void OnInitialize()
		{
		}

		// Token: 0x0600079C RID: 1948 RVA: 0x00019DCF File Offset: 0x00017FCF
		protected virtual void OnFinalize()
		{
		}

		// Token: 0x0600079D RID: 1949 RVA: 0x00019DD1 File Offset: 0x00017FD1
		protected internal virtual void OnTick(float dt)
		{
		}

		// Token: 0x0600079E RID: 1950 RVA: 0x00019DD3 File Offset: 0x00017FD3
		protected internal virtual void OnGameStart()
		{
		}

		// Token: 0x0600079F RID: 1951 RVA: 0x00019DD5 File Offset: 0x00017FD5
		protected internal virtual void OnGameEnd()
		{
		}

		// Token: 0x060007A0 RID: 1952 RVA: 0x00019DD7 File Offset: 0x00017FD7
		protected internal virtual void OnGameNetworkBegin()
		{
		}

		// Token: 0x060007A1 RID: 1953 RVA: 0x00019DD9 File Offset: 0x00017FD9
		protected internal virtual void OnGameNetworkEnd()
		{
		}

		// Token: 0x060007A2 RID: 1954 RVA: 0x00019DDB File Offset: 0x00017FDB
		protected internal virtual void OnEarlyPlayerConnect(VirtualPlayer peer)
		{
		}

		// Token: 0x060007A3 RID: 1955 RVA: 0x00019DDD File Offset: 0x00017FDD
		protected internal virtual void OnPlayerConnect(VirtualPlayer peer)
		{
		}

		// Token: 0x060007A4 RID: 1956 RVA: 0x00019DDF File Offset: 0x00017FDF
		protected internal virtual void OnPlayerDisconnect(VirtualPlayer peer)
		{
		}

		// Token: 0x060007A5 RID: 1957
		public abstract void OnBeforeSave();

		// Token: 0x060007A6 RID: 1958
		public abstract void OnAfterSave();
	}
}
