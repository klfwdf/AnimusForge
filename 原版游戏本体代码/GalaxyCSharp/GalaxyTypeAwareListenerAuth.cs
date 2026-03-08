using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000019 RID: 25
	public abstract class GalaxyTypeAwareListenerAuth : IGalaxyListener
	{
		// Token: 0x060004D7 RID: 1239 RVA: 0x000033A8 File Offset: 0x000015A8
		internal GalaxyTypeAwareListenerAuth(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerAuth_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060004D8 RID: 1240 RVA: 0x000033C4 File Offset: 0x000015C4
		public GalaxyTypeAwareListenerAuth()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerAuth(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060004D9 RID: 1241 RVA: 0x000033E2 File Offset: 0x000015E2
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerAuth obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060004DA RID: 1242 RVA: 0x00003400 File Offset: 0x00001600
		~GalaxyTypeAwareListenerAuth()
		{
			this.Dispose();
		}

		// Token: 0x060004DB RID: 1243 RVA: 0x00003430 File Offset: 0x00001630
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerAuth(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060004DC RID: 1244 RVA: 0x000034B8 File Offset: 0x000016B8
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerAuth_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400002D RID: 45
		private HandleRef swigCPtr;
	}
}
