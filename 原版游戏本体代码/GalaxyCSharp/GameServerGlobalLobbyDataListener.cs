using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000057 RID: 87
	public abstract class GameServerGlobalLobbyDataListener : ILobbyDataListener
	{
		// Token: 0x06000646 RID: 1606 RVA: 0x00008BCC File Offset: 0x00006DCC
		internal GameServerGlobalLobbyDataListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalLobbyDataListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000647 RID: 1607 RVA: 0x00008BE8 File Offset: 0x00006DE8
		public GameServerGlobalLobbyDataListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyData.GetListenerType(), this);
			}
		}

		// Token: 0x06000648 RID: 1608 RVA: 0x00008C0A File Offset: 0x00006E0A
		internal static HandleRef getCPtr(GameServerGlobalLobbyDataListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000649 RID: 1609 RVA: 0x00008C28 File Offset: 0x00006E28
		~GameServerGlobalLobbyDataListener()
		{
			this.Dispose();
		}

		// Token: 0x0600064A RID: 1610 RVA: 0x00008C58 File Offset: 0x00006E58
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyData.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalLobbyDataListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400006B RID: 107
		private HandleRef swigCPtr;
	}
}
