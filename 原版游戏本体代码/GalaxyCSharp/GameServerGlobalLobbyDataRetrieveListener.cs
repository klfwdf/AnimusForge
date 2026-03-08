using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000058 RID: 88
	public abstract class GameServerGlobalLobbyDataRetrieveListener : ILobbyDataRetrieveListener
	{
		// Token: 0x0600064B RID: 1611 RVA: 0x00008FAC File Offset: 0x000071AC
		internal GameServerGlobalLobbyDataRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalLobbyDataRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600064C RID: 1612 RVA: 0x00008FC8 File Offset: 0x000071C8
		public GameServerGlobalLobbyDataRetrieveListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyDataRetrieve.GetListenerType(), this);
			}
		}

		// Token: 0x0600064D RID: 1613 RVA: 0x00008FEA File Offset: 0x000071EA
		internal static HandleRef getCPtr(GameServerGlobalLobbyDataRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600064E RID: 1614 RVA: 0x00009008 File Offset: 0x00007208
		~GameServerGlobalLobbyDataRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x0600064F RID: 1615 RVA: 0x00009038 File Offset: 0x00007238
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyDataRetrieve.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalLobbyDataRetrieveListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400006C RID: 108
		private HandleRef swigCPtr;
	}
}
