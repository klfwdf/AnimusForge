using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200005B RID: 91
	public abstract class GameServerGlobalLobbyLeftListener : ILobbyLeftListener
	{
		// Token: 0x0600065A RID: 1626 RVA: 0x00009A2B File Offset: 0x00007C2B
		internal GameServerGlobalLobbyLeftListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalLobbyLeftListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600065B RID: 1627 RVA: 0x00009A47 File Offset: 0x00007C47
		public GameServerGlobalLobbyLeftListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyLeft.GetListenerType(), this);
			}
		}

		// Token: 0x0600065C RID: 1628 RVA: 0x00009A69 File Offset: 0x00007C69
		internal static HandleRef getCPtr(GameServerGlobalLobbyLeftListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600065D RID: 1629 RVA: 0x00009A88 File Offset: 0x00007C88
		~GameServerGlobalLobbyLeftListener()
		{
			this.Dispose();
		}

		// Token: 0x0600065E RID: 1630 RVA: 0x00009AB8 File Offset: 0x00007CB8
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyLeft.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalLobbyLeftListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400006F RID: 111
		private HandleRef swigCPtr;
	}
}
