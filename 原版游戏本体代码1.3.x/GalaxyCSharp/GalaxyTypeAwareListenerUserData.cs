using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200004D RID: 77
	public abstract class GalaxyTypeAwareListenerUserData : IGalaxyListener
	{
		// Token: 0x0600060F RID: 1551 RVA: 0x00007238 File Offset: 0x00005438
		internal GalaxyTypeAwareListenerUserData(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerUserData_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000610 RID: 1552 RVA: 0x00007254 File Offset: 0x00005454
		public GalaxyTypeAwareListenerUserData()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerUserData(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000611 RID: 1553 RVA: 0x00007272 File Offset: 0x00005472
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerUserData obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000612 RID: 1554 RVA: 0x00007290 File Offset: 0x00005490
		~GalaxyTypeAwareListenerUserData()
		{
			this.Dispose();
		}

		// Token: 0x06000613 RID: 1555 RVA: 0x000072C0 File Offset: 0x000054C0
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerUserData(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000614 RID: 1556 RVA: 0x00007348 File Offset: 0x00005548
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerUserData_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000061 RID: 97
		private HandleRef swigCPtr;
	}
}
