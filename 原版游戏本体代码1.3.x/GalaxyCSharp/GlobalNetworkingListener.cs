using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200008B RID: 139
	public abstract class GlobalNetworkingListener : INetworkingListener
	{
		// Token: 0x06000750 RID: 1872 RVA: 0x000126E4 File Offset: 0x000108E4
		internal GlobalNetworkingListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalNetworkingListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000751 RID: 1873 RVA: 0x00012700 File Offset: 0x00010900
		public GlobalNetworkingListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerNetworking.GetListenerType(), this);
			}
		}

		// Token: 0x06000752 RID: 1874 RVA: 0x00012722 File Offset: 0x00010922
		internal static HandleRef getCPtr(GlobalNetworkingListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000753 RID: 1875 RVA: 0x00012740 File Offset: 0x00010940
		~GlobalNetworkingListener()
		{
			this.Dispose();
		}

		// Token: 0x06000754 RID: 1876 RVA: 0x00012770 File Offset: 0x00010970
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerNetworking.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalNetworkingListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000A5 RID: 165
		private HandleRef swigCPtr;
	}
}
