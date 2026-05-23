using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200009B RID: 155
	public abstract class GlobalUserDataListener : IUserDataListener
	{
		// Token: 0x060007A3 RID: 1955 RVA: 0x00015597 File Offset: 0x00013797
		internal GlobalUserDataListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalUserDataListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060007A4 RID: 1956 RVA: 0x000155B3 File Offset: 0x000137B3
		public GlobalUserDataListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerUserData.GetListenerType(), this);
			}
		}

		// Token: 0x060007A5 RID: 1957 RVA: 0x000155D5 File Offset: 0x000137D5
		internal static HandleRef getCPtr(GlobalUserDataListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060007A6 RID: 1958 RVA: 0x000155F4 File Offset: 0x000137F4
		~GlobalUserDataListener()
		{
			this.Dispose();
		}

		// Token: 0x060007A7 RID: 1959 RVA: 0x00015624 File Offset: 0x00013824
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerUserData.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalUserDataListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000B8 RID: 184
		private HandleRef swigCPtr;
	}
}
