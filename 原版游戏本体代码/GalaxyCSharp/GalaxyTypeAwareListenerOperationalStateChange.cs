using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200003F RID: 63
	public abstract class GalaxyTypeAwareListenerOperationalStateChange : IGalaxyListener
	{
		// Token: 0x060005BB RID: 1467 RVA: 0x00006160 File Offset: 0x00004360
		internal GalaxyTypeAwareListenerOperationalStateChange(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerOperationalStateChange_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060005BC RID: 1468 RVA: 0x0000617C File Offset: 0x0000437C
		public GalaxyTypeAwareListenerOperationalStateChange()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerOperationalStateChange(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060005BD RID: 1469 RVA: 0x0000619A File Offset: 0x0000439A
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerOperationalStateChange obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060005BE RID: 1470 RVA: 0x000061B8 File Offset: 0x000043B8
		~GalaxyTypeAwareListenerOperationalStateChange()
		{
			this.Dispose();
		}

		// Token: 0x060005BF RID: 1471 RVA: 0x000061E8 File Offset: 0x000043E8
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerOperationalStateChange(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060005C0 RID: 1472 RVA: 0x00006270 File Offset: 0x00004470
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerOperationalStateChange_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000053 RID: 83
		private HandleRef swigCPtr;
	}
}
