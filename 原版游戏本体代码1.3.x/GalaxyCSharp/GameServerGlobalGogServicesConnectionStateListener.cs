using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000055 RID: 85
	public abstract class GameServerGlobalGogServicesConnectionStateListener : IGogServicesConnectionStateListener
	{
		// Token: 0x0600063C RID: 1596 RVA: 0x0000850D File Offset: 0x0000670D
		internal GameServerGlobalGogServicesConnectionStateListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalGogServicesConnectionStateListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600063D RID: 1597 RVA: 0x00008529 File Offset: 0x00006729
		public GameServerGlobalGogServicesConnectionStateListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerGogServicesConnectionState.GetListenerType(), this);
			}
		}

		// Token: 0x0600063E RID: 1598 RVA: 0x0000854B File Offset: 0x0000674B
		internal static HandleRef getCPtr(GameServerGlobalGogServicesConnectionStateListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600063F RID: 1599 RVA: 0x0000856C File Offset: 0x0000676C
		~GameServerGlobalGogServicesConnectionStateListener()
		{
			this.Dispose();
		}

		// Token: 0x06000640 RID: 1600 RVA: 0x0000859C File Offset: 0x0000679C
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerGogServicesConnectionState.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalGogServicesConnectionStateListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000069 RID: 105
		private HandleRef swigCPtr;
	}
}
