using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000017 RID: 23
	public abstract class GalaxyTypeAwareListenerAccessToken : IGalaxyListener
	{
		// Token: 0x060004CB RID: 1227 RVA: 0x00003140 File Offset: 0x00001340
		internal GalaxyTypeAwareListenerAccessToken(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerAccessToken_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060004CC RID: 1228 RVA: 0x0000315C File Offset: 0x0000135C
		public GalaxyTypeAwareListenerAccessToken()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerAccessToken(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060004CD RID: 1229 RVA: 0x0000317A File Offset: 0x0000137A
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerAccessToken obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060004CE RID: 1230 RVA: 0x00003198 File Offset: 0x00001398
		~GalaxyTypeAwareListenerAccessToken()
		{
			this.Dispose();
		}

		// Token: 0x060004CF RID: 1231 RVA: 0x000031C8 File Offset: 0x000013C8
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerAccessToken(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060004D0 RID: 1232 RVA: 0x00003250 File Offset: 0x00001450
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerAccessToken_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400002B RID: 43
		private HandleRef swigCPtr;
	}
}
