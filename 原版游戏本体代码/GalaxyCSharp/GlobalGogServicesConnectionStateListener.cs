using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200007A RID: 122
	public abstract class GlobalGogServicesConnectionStateListener : IGogServicesConnectionStateListener
	{
		// Token: 0x060006FB RID: 1787 RVA: 0x000101C8 File Offset: 0x0000E3C8
		internal GlobalGogServicesConnectionStateListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalGogServicesConnectionStateListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006FC RID: 1788 RVA: 0x000101E4 File Offset: 0x0000E3E4
		public GlobalGogServicesConnectionStateListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerGogServicesConnectionState.GetListenerType(), this);
			}
		}

		// Token: 0x060006FD RID: 1789 RVA: 0x00010206 File Offset: 0x0000E406
		internal static HandleRef getCPtr(GlobalGogServicesConnectionStateListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006FE RID: 1790 RVA: 0x00010224 File Offset: 0x0000E424
		~GlobalGogServicesConnectionStateListener()
		{
			this.Dispose();
		}

		// Token: 0x060006FF RID: 1791 RVA: 0x00010254 File Offset: 0x0000E454
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerGogServicesConnectionState.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalGogServicesConnectionStateListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000094 RID: 148
		private HandleRef swigCPtr;
	}
}
