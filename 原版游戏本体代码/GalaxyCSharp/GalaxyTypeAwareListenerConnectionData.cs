using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200001F RID: 31
	public abstract class GalaxyTypeAwareListenerConnectionData : IGalaxyListener
	{
		// Token: 0x060004FB RID: 1275 RVA: 0x00003AE0 File Offset: 0x00001CE0
		internal GalaxyTypeAwareListenerConnectionData(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerConnectionData_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060004FC RID: 1276 RVA: 0x00003AFC File Offset: 0x00001CFC
		public GalaxyTypeAwareListenerConnectionData()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerConnectionData(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060004FD RID: 1277 RVA: 0x00003B1A File Offset: 0x00001D1A
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerConnectionData obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060004FE RID: 1278 RVA: 0x00003B38 File Offset: 0x00001D38
		~GalaxyTypeAwareListenerConnectionData()
		{
			this.Dispose();
		}

		// Token: 0x060004FF RID: 1279 RVA: 0x00003B68 File Offset: 0x00001D68
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerConnectionData(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000500 RID: 1280 RVA: 0x00003BF0 File Offset: 0x00001DF0
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerConnectionData_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000033 RID: 51
		private HandleRef swigCPtr;
	}
}
