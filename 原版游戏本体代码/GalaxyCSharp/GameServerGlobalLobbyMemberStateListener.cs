using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200005D RID: 93
	public abstract class GameServerGlobalLobbyMemberStateListener : ILobbyMemberStateListener
	{
		// Token: 0x06000664 RID: 1636 RVA: 0x0000A1DA File Offset: 0x000083DA
		internal GameServerGlobalLobbyMemberStateListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalLobbyMemberStateListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000665 RID: 1637 RVA: 0x0000A1F6 File Offset: 0x000083F6
		public GameServerGlobalLobbyMemberStateListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyMemberState.GetListenerType(), this);
			}
		}

		// Token: 0x06000666 RID: 1638 RVA: 0x0000A218 File Offset: 0x00008418
		internal static HandleRef getCPtr(GameServerGlobalLobbyMemberStateListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000667 RID: 1639 RVA: 0x0000A238 File Offset: 0x00008438
		~GameServerGlobalLobbyMemberStateListener()
		{
			this.Dispose();
		}

		// Token: 0x06000668 RID: 1640 RVA: 0x0000A268 File Offset: 0x00008468
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyMemberState.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalLobbyMemberStateListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000071 RID: 113
		private HandleRef swigCPtr;
	}
}
