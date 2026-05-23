using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200009A RID: 154
	public abstract class GlobalTelemetryEventSendListener : ITelemetryEventSendListener
	{
		// Token: 0x0600079E RID: 1950 RVA: 0x00015274 File Offset: 0x00013474
		internal GlobalTelemetryEventSendListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalTelemetryEventSendListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600079F RID: 1951 RVA: 0x00015290 File Offset: 0x00013490
		public GlobalTelemetryEventSendListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerTelemetryEventSend.GetListenerType(), this);
			}
		}

		// Token: 0x060007A0 RID: 1952 RVA: 0x000152B2 File Offset: 0x000134B2
		internal static HandleRef getCPtr(GlobalTelemetryEventSendListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060007A1 RID: 1953 RVA: 0x000152D0 File Offset: 0x000134D0
		~GlobalTelemetryEventSendListener()
		{
			this.Dispose();
		}

		// Token: 0x060007A2 RID: 1954 RVA: 0x00015300 File Offset: 0x00013500
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerTelemetryEventSend.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalTelemetryEventSendListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000B7 RID: 183
		private HandleRef swigCPtr;
	}
}
