using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200008C RID: 140
	public abstract class GlobalNotificationListener : INotificationListener
	{
		// Token: 0x06000755 RID: 1877 RVA: 0x00012A31 File Offset: 0x00010C31
		internal GlobalNotificationListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalNotificationListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000756 RID: 1878 RVA: 0x00012A4D File Offset: 0x00010C4D
		public GlobalNotificationListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerNotification.GetListenerType(), this);
			}
		}

		// Token: 0x06000757 RID: 1879 RVA: 0x00012A6F File Offset: 0x00010C6F
		internal static HandleRef getCPtr(GlobalNotificationListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000758 RID: 1880 RVA: 0x00012A90 File Offset: 0x00010C90
		~GlobalNotificationListener()
		{
			this.Dispose();
		}

		// Token: 0x06000759 RID: 1881 RVA: 0x00012AC0 File Offset: 0x00010CC0
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerNotification.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalNotificationListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000A6 RID: 166
		private HandleRef swigCPtr;
	}
}
