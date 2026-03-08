using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000042 RID: 66
	public abstract class GalaxyTypeAwareListenerOverlayVisibilityChange : IGalaxyListener
	{
		// Token: 0x060005CD RID: 1485 RVA: 0x000064FC File Offset: 0x000046FC
		internal GalaxyTypeAwareListenerOverlayVisibilityChange(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerOverlayVisibilityChange_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060005CE RID: 1486 RVA: 0x00006518 File Offset: 0x00004718
		public GalaxyTypeAwareListenerOverlayVisibilityChange()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerOverlayVisibilityChange(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060005CF RID: 1487 RVA: 0x00006536 File Offset: 0x00004736
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerOverlayVisibilityChange obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060005D0 RID: 1488 RVA: 0x00006554 File Offset: 0x00004754
		~GalaxyTypeAwareListenerOverlayVisibilityChange()
		{
			this.Dispose();
		}

		// Token: 0x060005D1 RID: 1489 RVA: 0x00006584 File Offset: 0x00004784
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerOverlayVisibilityChange(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060005D2 RID: 1490 RVA: 0x0000660C File Offset: 0x0000480C
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerOverlayVisibilityChange_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000056 RID: 86
		private HandleRef swigCPtr;
	}
}
