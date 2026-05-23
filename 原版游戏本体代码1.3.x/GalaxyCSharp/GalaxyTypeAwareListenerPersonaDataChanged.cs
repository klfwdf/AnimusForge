using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000043 RID: 67
	public abstract class GalaxyTypeAwareListenerPersonaDataChanged : IGalaxyListener
	{
		// Token: 0x060005D3 RID: 1491 RVA: 0x00006630 File Offset: 0x00004830
		internal GalaxyTypeAwareListenerPersonaDataChanged(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerPersonaDataChanged_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060005D4 RID: 1492 RVA: 0x0000664C File Offset: 0x0000484C
		public GalaxyTypeAwareListenerPersonaDataChanged()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerPersonaDataChanged(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060005D5 RID: 1493 RVA: 0x0000666A File Offset: 0x0000486A
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerPersonaDataChanged obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060005D6 RID: 1494 RVA: 0x00006688 File Offset: 0x00004888
		~GalaxyTypeAwareListenerPersonaDataChanged()
		{
			this.Dispose();
		}

		// Token: 0x060005D7 RID: 1495 RVA: 0x000066B8 File Offset: 0x000048B8
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerPersonaDataChanged(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060005D8 RID: 1496 RVA: 0x00006740 File Offset: 0x00004940
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerPersonaDataChanged_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000057 RID: 87
		private HandleRef swigCPtr;
	}
}
