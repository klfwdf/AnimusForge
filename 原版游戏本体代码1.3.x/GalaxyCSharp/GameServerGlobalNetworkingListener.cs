using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200005F RID: 95
	public abstract class GameServerGlobalNetworkingListener : INetworkingListener
	{
		// Token: 0x0600066E RID: 1646 RVA: 0x0000A8B3 File Offset: 0x00008AB3
		internal GameServerGlobalNetworkingListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalNetworkingListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600066F RID: 1647 RVA: 0x0000A8CF File Offset: 0x00008ACF
		public GameServerGlobalNetworkingListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerNetworking.GetListenerType(), this);
			}
		}

		// Token: 0x06000670 RID: 1648 RVA: 0x0000A8F1 File Offset: 0x00008AF1
		internal static HandleRef getCPtr(GameServerGlobalNetworkingListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000671 RID: 1649 RVA: 0x0000A910 File Offset: 0x00008B10
		~GameServerGlobalNetworkingListener()
		{
			this.Dispose();
		}

		// Token: 0x06000672 RID: 1650 RVA: 0x0000A940 File Offset: 0x00008B40
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerNetworking.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalNetworkingListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000073 RID: 115
		private HandleRef swigCPtr;
	}
}
