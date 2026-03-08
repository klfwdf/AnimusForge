using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200003E RID: 62
	public abstract class GalaxyTypeAwareListenerNotification : IGalaxyListener
	{
		// Token: 0x060005B5 RID: 1461 RVA: 0x0000602C File Offset: 0x0000422C
		internal GalaxyTypeAwareListenerNotification(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerNotification_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060005B6 RID: 1462 RVA: 0x00006048 File Offset: 0x00004248
		public GalaxyTypeAwareListenerNotification()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerNotification(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060005B7 RID: 1463 RVA: 0x00006066 File Offset: 0x00004266
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerNotification obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060005B8 RID: 1464 RVA: 0x00006084 File Offset: 0x00004284
		~GalaxyTypeAwareListenerNotification()
		{
			this.Dispose();
		}

		// Token: 0x060005B9 RID: 1465 RVA: 0x000060B4 File Offset: 0x000042B4
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerNotification(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060005BA RID: 1466 RVA: 0x0000613C File Offset: 0x0000433C
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerNotification_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000052 RID: 82
		private HandleRef swigCPtr;
	}
}
