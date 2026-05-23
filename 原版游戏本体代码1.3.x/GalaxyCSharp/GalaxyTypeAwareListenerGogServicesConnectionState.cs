using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200002C RID: 44
	public abstract class GalaxyTypeAwareListenerGogServicesConnectionState : IGalaxyListener
	{
		// Token: 0x06000549 RID: 1353 RVA: 0x00004A84 File Offset: 0x00002C84
		internal GalaxyTypeAwareListenerGogServicesConnectionState(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerGogServicesConnectionState_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600054A RID: 1354 RVA: 0x00004AA0 File Offset: 0x00002CA0
		public GalaxyTypeAwareListenerGogServicesConnectionState()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerGogServicesConnectionState(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600054B RID: 1355 RVA: 0x00004ABE File Offset: 0x00002CBE
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerGogServicesConnectionState obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600054C RID: 1356 RVA: 0x00004ADC File Offset: 0x00002CDC
		~GalaxyTypeAwareListenerGogServicesConnectionState()
		{
			this.Dispose();
		}

		// Token: 0x0600054D RID: 1357 RVA: 0x00004B0C File Offset: 0x00002D0C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerGogServicesConnectionState(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0600054E RID: 1358 RVA: 0x00004B94 File Offset: 0x00002D94
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerGogServicesConnectionState_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000040 RID: 64
		private HandleRef swigCPtr;
	}
}
