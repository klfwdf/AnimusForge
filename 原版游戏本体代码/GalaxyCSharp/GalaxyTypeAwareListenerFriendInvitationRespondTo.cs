using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000027 RID: 39
	public abstract class GalaxyTypeAwareListenerFriendInvitationRespondTo : IGalaxyListener
	{
		// Token: 0x0600052B RID: 1323 RVA: 0x00004480 File Offset: 0x00002680
		internal GalaxyTypeAwareListenerFriendInvitationRespondTo(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerFriendInvitationRespondTo_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600052C RID: 1324 RVA: 0x0000449C File Offset: 0x0000269C
		public GalaxyTypeAwareListenerFriendInvitationRespondTo()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerFriendInvitationRespondTo(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600052D RID: 1325 RVA: 0x000044BA File Offset: 0x000026BA
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerFriendInvitationRespondTo obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600052E RID: 1326 RVA: 0x000044D8 File Offset: 0x000026D8
		~GalaxyTypeAwareListenerFriendInvitationRespondTo()
		{
			this.Dispose();
		}

		// Token: 0x0600052F RID: 1327 RVA: 0x00004508 File Offset: 0x00002708
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerFriendInvitationRespondTo(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000530 RID: 1328 RVA: 0x00004590 File Offset: 0x00002790
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerFriendInvitationRespondTo_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400003B RID: 59
		private HandleRef swigCPtr;
	}
}
