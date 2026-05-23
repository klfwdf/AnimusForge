using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200003C RID: 60
	public abstract class GalaxyTypeAwareListenerNatTypeDetection : IGalaxyListener
	{
		// Token: 0x060005A9 RID: 1449 RVA: 0x00005DC4 File Offset: 0x00003FC4
		internal GalaxyTypeAwareListenerNatTypeDetection(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerNatTypeDetection_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060005AA RID: 1450 RVA: 0x00005DE0 File Offset: 0x00003FE0
		public GalaxyTypeAwareListenerNatTypeDetection()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerNatTypeDetection(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060005AB RID: 1451 RVA: 0x00005DFE File Offset: 0x00003FFE
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerNatTypeDetection obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060005AC RID: 1452 RVA: 0x00005E1C File Offset: 0x0000401C
		~GalaxyTypeAwareListenerNatTypeDetection()
		{
			this.Dispose();
		}

		// Token: 0x060005AD RID: 1453 RVA: 0x00005E4C File Offset: 0x0000404C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerNatTypeDetection(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060005AE RID: 1454 RVA: 0x00005ED4 File Offset: 0x000040D4
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerNatTypeDetection_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000050 RID: 80
		private HandleRef swigCPtr;
	}
}
