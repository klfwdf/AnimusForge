using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200004A RID: 74
	public abstract class GalaxyTypeAwareListenerSpecificUserData : IGalaxyListener
	{
		// Token: 0x060005FD RID: 1533 RVA: 0x00006E9C File Offset: 0x0000509C
		internal GalaxyTypeAwareListenerSpecificUserData(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerSpecificUserData_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060005FE RID: 1534 RVA: 0x00006EB8 File Offset: 0x000050B8
		public GalaxyTypeAwareListenerSpecificUserData()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerSpecificUserData(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060005FF RID: 1535 RVA: 0x00006ED6 File Offset: 0x000050D6
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerSpecificUserData obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000600 RID: 1536 RVA: 0x00006EF4 File Offset: 0x000050F4
		~GalaxyTypeAwareListenerSpecificUserData()
		{
			this.Dispose();
		}

		// Token: 0x06000601 RID: 1537 RVA: 0x00006F24 File Offset: 0x00005124
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerSpecificUserData(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000602 RID: 1538 RVA: 0x00006FAC File Offset: 0x000051AC
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerSpecificUserData_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400005E RID: 94
		private HandleRef swigCPtr;
	}
}
