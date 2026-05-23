using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200001E RID: 30
	public abstract class GalaxyTypeAwareListenerConnectionClose : IGalaxyListener
	{
		// Token: 0x060004F5 RID: 1269 RVA: 0x000039AC File Offset: 0x00001BAC
		internal GalaxyTypeAwareListenerConnectionClose(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerConnectionClose_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060004F6 RID: 1270 RVA: 0x000039C8 File Offset: 0x00001BC8
		public GalaxyTypeAwareListenerConnectionClose()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerConnectionClose(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060004F7 RID: 1271 RVA: 0x000039E6 File Offset: 0x00001BE6
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerConnectionClose obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060004F8 RID: 1272 RVA: 0x00003A04 File Offset: 0x00001C04
		~GalaxyTypeAwareListenerConnectionClose()
		{
			this.Dispose();
		}

		// Token: 0x060004F9 RID: 1273 RVA: 0x00003A34 File Offset: 0x00001C34
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerConnectionClose(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060004FA RID: 1274 RVA: 0x00003ABC File Offset: 0x00001CBC
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerConnectionClose_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000032 RID: 50
		private HandleRef swigCPtr;
	}
}
