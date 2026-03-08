using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000026 RID: 38
	public abstract class GalaxyTypeAwareListenerFriendInvitationListRetrieve : IGalaxyListener
	{
		// Token: 0x06000525 RID: 1317 RVA: 0x0000434C File Offset: 0x0000254C
		internal GalaxyTypeAwareListenerFriendInvitationListRetrieve(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerFriendInvitationListRetrieve_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000526 RID: 1318 RVA: 0x00004368 File Offset: 0x00002568
		public GalaxyTypeAwareListenerFriendInvitationListRetrieve()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerFriendInvitationListRetrieve(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000527 RID: 1319 RVA: 0x00004386 File Offset: 0x00002586
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerFriendInvitationListRetrieve obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000528 RID: 1320 RVA: 0x000043A4 File Offset: 0x000025A4
		~GalaxyTypeAwareListenerFriendInvitationListRetrieve()
		{
			this.Dispose();
		}

		// Token: 0x06000529 RID: 1321 RVA: 0x000043D4 File Offset: 0x000025D4
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerFriendInvitationListRetrieve(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0600052A RID: 1322 RVA: 0x0000445C File Offset: 0x0000265C
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerFriendInvitationListRetrieve_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400003A RID: 58
		private HandleRef swigCPtr;
	}
}
