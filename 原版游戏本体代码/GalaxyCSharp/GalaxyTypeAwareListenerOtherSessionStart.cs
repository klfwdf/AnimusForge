using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000040 RID: 64
	public abstract class GalaxyTypeAwareListenerOtherSessionStart : IGalaxyListener
	{
		// Token: 0x060005C1 RID: 1473 RVA: 0x00006294 File Offset: 0x00004494
		internal GalaxyTypeAwareListenerOtherSessionStart(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerOtherSessionStart_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060005C2 RID: 1474 RVA: 0x000062B0 File Offset: 0x000044B0
		public GalaxyTypeAwareListenerOtherSessionStart()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerOtherSessionStart(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060005C3 RID: 1475 RVA: 0x000062CE File Offset: 0x000044CE
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerOtherSessionStart obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060005C4 RID: 1476 RVA: 0x000062EC File Offset: 0x000044EC
		~GalaxyTypeAwareListenerOtherSessionStart()
		{
			this.Dispose();
		}

		// Token: 0x060005C5 RID: 1477 RVA: 0x0000631C File Offset: 0x0000451C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerOtherSessionStart(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060005C6 RID: 1478 RVA: 0x000063A4 File Offset: 0x000045A4
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerOtherSessionStart_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000054 RID: 84
		private HandleRef swigCPtr;
	}
}
