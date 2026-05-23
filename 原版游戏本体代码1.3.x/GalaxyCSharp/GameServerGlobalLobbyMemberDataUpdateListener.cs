using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200005C RID: 92
	public abstract class GameServerGlobalLobbyMemberDataUpdateListener : ILobbyMemberDataUpdateListener
	{
		// Token: 0x0600065F RID: 1631 RVA: 0x00009E5E File Offset: 0x0000805E
		internal GameServerGlobalLobbyMemberDataUpdateListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalLobbyMemberDataUpdateListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000660 RID: 1632 RVA: 0x00009E7A File Offset: 0x0000807A
		public GameServerGlobalLobbyMemberDataUpdateListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyMemberDataUpdate.GetListenerType(), this);
			}
		}

		// Token: 0x06000661 RID: 1633 RVA: 0x00009E9C File Offset: 0x0000809C
		internal static HandleRef getCPtr(GameServerGlobalLobbyMemberDataUpdateListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000662 RID: 1634 RVA: 0x00009EBC File Offset: 0x000080BC
		~GameServerGlobalLobbyMemberDataUpdateListener()
		{
			this.Dispose();
		}

		// Token: 0x06000663 RID: 1635 RVA: 0x00009EEC File Offset: 0x000080EC
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyMemberDataUpdate.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalLobbyMemberDataUpdateListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000070 RID: 112
		private HandleRef swigCPtr;
	}
}
