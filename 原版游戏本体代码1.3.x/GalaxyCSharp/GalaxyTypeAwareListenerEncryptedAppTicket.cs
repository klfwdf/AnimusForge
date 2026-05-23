using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000021 RID: 33
	public abstract class GalaxyTypeAwareListenerEncryptedAppTicket : IGalaxyListener
	{
		// Token: 0x06000507 RID: 1287 RVA: 0x00003D48 File Offset: 0x00001F48
		internal GalaxyTypeAwareListenerEncryptedAppTicket(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerEncryptedAppTicket_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000508 RID: 1288 RVA: 0x00003D64 File Offset: 0x00001F64
		public GalaxyTypeAwareListenerEncryptedAppTicket()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerEncryptedAppTicket(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000509 RID: 1289 RVA: 0x00003D82 File Offset: 0x00001F82
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerEncryptedAppTicket obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600050A RID: 1290 RVA: 0x00003DA0 File Offset: 0x00001FA0
		~GalaxyTypeAwareListenerEncryptedAppTicket()
		{
			this.Dispose();
		}

		// Token: 0x0600050B RID: 1291 RVA: 0x00003DD0 File Offset: 0x00001FD0
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerEncryptedAppTicket(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0600050C RID: 1292 RVA: 0x00003E58 File Offset: 0x00002058
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerEncryptedAppTicket_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000035 RID: 53
		private HandleRef swigCPtr;
	}
}
