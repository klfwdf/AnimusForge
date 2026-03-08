using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000059 RID: 89
	public abstract class GameServerGlobalLobbyDataUpdateListener : ILobbyDataUpdateListener
	{
		// Token: 0x06000650 RID: 1616 RVA: 0x0000938C File Offset: 0x0000758C
		internal GameServerGlobalLobbyDataUpdateListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalLobbyDataUpdateListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000651 RID: 1617 RVA: 0x000093A8 File Offset: 0x000075A8
		public GameServerGlobalLobbyDataUpdateListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyDataUpdate.GetListenerType(), this);
			}
		}

		// Token: 0x06000652 RID: 1618 RVA: 0x000093CA File Offset: 0x000075CA
		internal static HandleRef getCPtr(GameServerGlobalLobbyDataUpdateListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000653 RID: 1619 RVA: 0x000093E8 File Offset: 0x000075E8
		~GameServerGlobalLobbyDataUpdateListener()
		{
			this.Dispose();
		}

		// Token: 0x06000654 RID: 1620 RVA: 0x00009418 File Offset: 0x00007618
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyDataUpdate.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalLobbyDataUpdateListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400006D RID: 109
		private HandleRef swigCPtr;
	}
}
