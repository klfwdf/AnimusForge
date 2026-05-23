using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000031 RID: 49
	public abstract class GalaxyTypeAwareListenerLobbyCreated : IGalaxyListener
	{
		// Token: 0x06000567 RID: 1383 RVA: 0x00005088 File Offset: 0x00003288
		internal GalaxyTypeAwareListenerLobbyCreated(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyCreated_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000568 RID: 1384 RVA: 0x000050A4 File Offset: 0x000032A4
		public GalaxyTypeAwareListenerLobbyCreated()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerLobbyCreated(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000569 RID: 1385 RVA: 0x000050C2 File Offset: 0x000032C2
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerLobbyCreated obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600056A RID: 1386 RVA: 0x000050E0 File Offset: 0x000032E0
		~GalaxyTypeAwareListenerLobbyCreated()
		{
			this.Dispose();
		}

		// Token: 0x0600056B RID: 1387 RVA: 0x00005110 File Offset: 0x00003310
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerLobbyCreated(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0600056C RID: 1388 RVA: 0x00005198 File Offset: 0x00003398
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyCreated_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000045 RID: 69
		private HandleRef swigCPtr;
	}
}
