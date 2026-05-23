using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200007E RID: 126
	public abstract class GlobalLeaderboardsRetrieveListener : ILeaderboardsRetrieveListener
	{
		// Token: 0x0600070F RID: 1807 RVA: 0x000110E6 File Offset: 0x0000F2E6
		internal GlobalLeaderboardsRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalLeaderboardsRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000710 RID: 1808 RVA: 0x00011102 File Offset: 0x0000F302
		public GlobalLeaderboardsRetrieveListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerLeaderboardsRetrieve.GetListenerType(), this);
			}
		}

		// Token: 0x06000711 RID: 1809 RVA: 0x00011124 File Offset: 0x0000F324
		internal static HandleRef getCPtr(GlobalLeaderboardsRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000712 RID: 1810 RVA: 0x00011144 File Offset: 0x0000F344
		~GlobalLeaderboardsRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x06000713 RID: 1811 RVA: 0x00011174 File Offset: 0x0000F374
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerLeaderboardsRetrieve.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalLeaderboardsRetrieveListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000098 RID: 152
		private HandleRef swigCPtr;
	}
}
