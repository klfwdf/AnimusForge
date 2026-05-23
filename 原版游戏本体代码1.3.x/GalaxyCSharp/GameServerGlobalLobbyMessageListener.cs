using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200005E RID: 94
	public abstract class GameServerGlobalLobbyMessageListener : ILobbyMessageListener
	{
		// Token: 0x06000669 RID: 1641 RVA: 0x0000A572 File Offset: 0x00008772
		internal GameServerGlobalLobbyMessageListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalLobbyMessageListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600066A RID: 1642 RVA: 0x0000A58E File Offset: 0x0000878E
		public GameServerGlobalLobbyMessageListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyMessage.GetListenerType(), this);
			}
		}

		// Token: 0x0600066B RID: 1643 RVA: 0x0000A5B0 File Offset: 0x000087B0
		internal static HandleRef getCPtr(GameServerGlobalLobbyMessageListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600066C RID: 1644 RVA: 0x0000A5D0 File Offset: 0x000087D0
		~GameServerGlobalLobbyMessageListener()
		{
			this.Dispose();
		}

		// Token: 0x0600066D RID: 1645 RVA: 0x0000A600 File Offset: 0x00008800
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyMessage.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalLobbyMessageListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000072 RID: 114
		private HandleRef swigCPtr;
	}
}
