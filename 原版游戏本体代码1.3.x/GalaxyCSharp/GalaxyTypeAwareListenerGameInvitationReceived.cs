using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200002A RID: 42
	public abstract class GalaxyTypeAwareListenerGameInvitationReceived : IGalaxyListener
	{
		// Token: 0x0600053D RID: 1341 RVA: 0x0000481C File Offset: 0x00002A1C
		internal GalaxyTypeAwareListenerGameInvitationReceived(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerGameInvitationReceived_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600053E RID: 1342 RVA: 0x00004838 File Offset: 0x00002A38
		public GalaxyTypeAwareListenerGameInvitationReceived()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerGameInvitationReceived(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600053F RID: 1343 RVA: 0x00004856 File Offset: 0x00002A56
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerGameInvitationReceived obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000540 RID: 1344 RVA: 0x00004874 File Offset: 0x00002A74
		~GalaxyTypeAwareListenerGameInvitationReceived()
		{
			this.Dispose();
		}

		// Token: 0x06000541 RID: 1345 RVA: 0x000048A4 File Offset: 0x00002AA4
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerGameInvitationReceived(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000542 RID: 1346 RVA: 0x0000492C File Offset: 0x00002B2C
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerGameInvitationReceived_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400003E RID: 62
		private HandleRef swigCPtr;
	}
}
