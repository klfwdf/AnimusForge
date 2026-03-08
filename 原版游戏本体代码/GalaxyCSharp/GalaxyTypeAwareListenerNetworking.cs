using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200003D RID: 61
	public abstract class GalaxyTypeAwareListenerNetworking : IGalaxyListener
	{
		// Token: 0x060005AF RID: 1455 RVA: 0x00005EF8 File Offset: 0x000040F8
		internal GalaxyTypeAwareListenerNetworking(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerNetworking_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060005B0 RID: 1456 RVA: 0x00005F14 File Offset: 0x00004114
		public GalaxyTypeAwareListenerNetworking()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerNetworking(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060005B1 RID: 1457 RVA: 0x00005F32 File Offset: 0x00004132
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerNetworking obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060005B2 RID: 1458 RVA: 0x00005F50 File Offset: 0x00004150
		~GalaxyTypeAwareListenerNetworking()
		{
			this.Dispose();
		}

		// Token: 0x060005B3 RID: 1459 RVA: 0x00005F80 File Offset: 0x00004180
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerNetworking(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060005B4 RID: 1460 RVA: 0x00006008 File Offset: 0x00004208
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerNetworking_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000051 RID: 81
		private HandleRef swigCPtr;
	}
}
