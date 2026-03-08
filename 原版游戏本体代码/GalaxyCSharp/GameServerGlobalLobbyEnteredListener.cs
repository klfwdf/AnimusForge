using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200005A RID: 90
	public abstract class GameServerGlobalLobbyEnteredListener : ILobbyEnteredListener
	{
		// Token: 0x06000655 RID: 1621 RVA: 0x000096DB File Offset: 0x000078DB
		internal GameServerGlobalLobbyEnteredListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalLobbyEnteredListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000656 RID: 1622 RVA: 0x000096F7 File Offset: 0x000078F7
		public GameServerGlobalLobbyEnteredListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyEntered.GetListenerType(), this);
			}
		}

		// Token: 0x06000657 RID: 1623 RVA: 0x00009719 File Offset: 0x00007919
		internal static HandleRef getCPtr(GameServerGlobalLobbyEnteredListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000658 RID: 1624 RVA: 0x00009738 File Offset: 0x00007938
		~GameServerGlobalLobbyEnteredListener()
		{
			this.Dispose();
		}

		// Token: 0x06000659 RID: 1625 RVA: 0x00009768 File Offset: 0x00007968
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyEntered.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalLobbyEnteredListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400006E RID: 110
		private HandleRef swigCPtr;
	}
}
