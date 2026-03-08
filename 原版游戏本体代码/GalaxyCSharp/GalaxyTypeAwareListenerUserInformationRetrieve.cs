using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200004F RID: 79
	public abstract class GalaxyTypeAwareListenerUserInformationRetrieve : IGalaxyListener
	{
		// Token: 0x0600061B RID: 1563 RVA: 0x000074A0 File Offset: 0x000056A0
		internal GalaxyTypeAwareListenerUserInformationRetrieve(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerUserInformationRetrieve_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600061C RID: 1564 RVA: 0x000074BC File Offset: 0x000056BC
		public GalaxyTypeAwareListenerUserInformationRetrieve()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerUserInformationRetrieve(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600061D RID: 1565 RVA: 0x000074DA File Offset: 0x000056DA
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerUserInformationRetrieve obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600061E RID: 1566 RVA: 0x000074F8 File Offset: 0x000056F8
		~GalaxyTypeAwareListenerUserInformationRetrieve()
		{
			this.Dispose();
		}

		// Token: 0x0600061F RID: 1567 RVA: 0x00007528 File Offset: 0x00005728
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerUserInformationRetrieve(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000620 RID: 1568 RVA: 0x000075B0 File Offset: 0x000057B0
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerUserInformationRetrieve_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000063 RID: 99
		private HandleRef swigCPtr;
	}
}
