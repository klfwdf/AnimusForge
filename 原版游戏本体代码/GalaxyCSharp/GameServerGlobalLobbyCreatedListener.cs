using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000056 RID: 86
	public abstract class GameServerGlobalLobbyCreatedListener : ILobbyCreatedListener
	{
		// Token: 0x06000641 RID: 1601 RVA: 0x0000885F File Offset: 0x00006A5F
		internal GameServerGlobalLobbyCreatedListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalLobbyCreatedListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000642 RID: 1602 RVA: 0x0000887B File Offset: 0x00006A7B
		public GameServerGlobalLobbyCreatedListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyCreated.GetListenerType(), this);
			}
		}

		// Token: 0x06000643 RID: 1603 RVA: 0x0000889D File Offset: 0x00006A9D
		internal static HandleRef getCPtr(GameServerGlobalLobbyCreatedListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000644 RID: 1604 RVA: 0x000088BC File Offset: 0x00006ABC
		~GameServerGlobalLobbyCreatedListener()
		{
			this.Dispose();
		}

		// Token: 0x06000645 RID: 1605 RVA: 0x000088EC File Offset: 0x00006AEC
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyCreated.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalLobbyCreatedListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400006A RID: 106
		private HandleRef swigCPtr;
	}
}
