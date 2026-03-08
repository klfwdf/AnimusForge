using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000041 RID: 65
	public abstract class GalaxyTypeAwareListenerOverlayInitializationStateChange : IGalaxyListener
	{
		// Token: 0x060005C7 RID: 1479 RVA: 0x000063C8 File Offset: 0x000045C8
		internal GalaxyTypeAwareListenerOverlayInitializationStateChange(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerOverlayInitializationStateChange_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060005C8 RID: 1480 RVA: 0x000063E4 File Offset: 0x000045E4
		public GalaxyTypeAwareListenerOverlayInitializationStateChange()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerOverlayInitializationStateChange(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060005C9 RID: 1481 RVA: 0x00006402 File Offset: 0x00004602
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerOverlayInitializationStateChange obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060005CA RID: 1482 RVA: 0x00006420 File Offset: 0x00004620
		~GalaxyTypeAwareListenerOverlayInitializationStateChange()
		{
			this.Dispose();
		}

		// Token: 0x060005CB RID: 1483 RVA: 0x00006450 File Offset: 0x00004650
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerOverlayInitializationStateChange(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060005CC RID: 1484 RVA: 0x000064D8 File Offset: 0x000046D8
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerOverlayInitializationStateChange_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000055 RID: 85
		private HandleRef swigCPtr;
	}
}
