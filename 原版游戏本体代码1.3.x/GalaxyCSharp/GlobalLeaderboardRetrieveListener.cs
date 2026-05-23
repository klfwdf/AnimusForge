using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200007C RID: 124
	public abstract class GlobalLeaderboardRetrieveListener : ILeaderboardRetrieveListener
	{
		// Token: 0x06000705 RID: 1797 RVA: 0x00010958 File Offset: 0x0000EB58
		internal GlobalLeaderboardRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalLeaderboardRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000706 RID: 1798 RVA: 0x00010974 File Offset: 0x0000EB74
		public GlobalLeaderboardRetrieveListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerLeaderboardRetrieve.GetListenerType(), this);
			}
		}

		// Token: 0x06000707 RID: 1799 RVA: 0x00010996 File Offset: 0x0000EB96
		internal static HandleRef getCPtr(GlobalLeaderboardRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000708 RID: 1800 RVA: 0x000109B4 File Offset: 0x0000EBB4
		~GlobalLeaderboardRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x06000709 RID: 1801 RVA: 0x000109E4 File Offset: 0x0000EBE4
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerLeaderboardRetrieve.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalLeaderboardRetrieveListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000096 RID: 150
		private HandleRef swigCPtr;
	}
}
