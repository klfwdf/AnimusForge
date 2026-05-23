using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200004C RID: 76
	public abstract class GalaxyTypeAwareListenerTelemetryEventSend : IGalaxyListener
	{
		// Token: 0x06000609 RID: 1545 RVA: 0x00007104 File Offset: 0x00005304
		internal GalaxyTypeAwareListenerTelemetryEventSend(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerTelemetryEventSend_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600060A RID: 1546 RVA: 0x00007120 File Offset: 0x00005320
		public GalaxyTypeAwareListenerTelemetryEventSend()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerTelemetryEventSend(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600060B RID: 1547 RVA: 0x0000713E File Offset: 0x0000533E
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerTelemetryEventSend obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600060C RID: 1548 RVA: 0x0000715C File Offset: 0x0000535C
		~GalaxyTypeAwareListenerTelemetryEventSend()
		{
			this.Dispose();
		}

		// Token: 0x0600060D RID: 1549 RVA: 0x0000718C File Offset: 0x0000538C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerTelemetryEventSend(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0600060E RID: 1550 RVA: 0x00007214 File Offset: 0x00005414
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerTelemetryEventSend_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000060 RID: 96
		private HandleRef swigCPtr;
	}
}
