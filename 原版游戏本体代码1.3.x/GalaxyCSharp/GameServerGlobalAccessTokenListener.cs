using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000052 RID: 82
	public abstract class GameServerGlobalAccessTokenListener : IAccessTokenListener
	{
		// Token: 0x0600062D RID: 1581 RVA: 0x00007A33 File Offset: 0x00005C33
		internal GameServerGlobalAccessTokenListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalAccessTokenListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600062E RID: 1582 RVA: 0x00007A4F File Offset: 0x00005C4F
		public GameServerGlobalAccessTokenListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerAccessToken.GetListenerType(), this);
			}
		}

		// Token: 0x0600062F RID: 1583 RVA: 0x00007A71 File Offset: 0x00005C71
		internal static HandleRef getCPtr(GameServerGlobalAccessTokenListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000630 RID: 1584 RVA: 0x00007A90 File Offset: 0x00005C90
		~GameServerGlobalAccessTokenListener()
		{
			this.Dispose();
		}

		// Token: 0x06000631 RID: 1585 RVA: 0x00007AC0 File Offset: 0x00005CC0
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerAccessToken.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalAccessTokenListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000066 RID: 102
		private HandleRef swigCPtr;
	}
}
