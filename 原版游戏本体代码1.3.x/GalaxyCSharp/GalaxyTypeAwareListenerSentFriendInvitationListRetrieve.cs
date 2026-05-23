using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000048 RID: 72
	public abstract class GalaxyTypeAwareListenerSentFriendInvitationListRetrieve : IGalaxyListener
	{
		// Token: 0x060005F1 RID: 1521 RVA: 0x00006C34 File Offset: 0x00004E34
		internal GalaxyTypeAwareListenerSentFriendInvitationListRetrieve(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerSentFriendInvitationListRetrieve_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060005F2 RID: 1522 RVA: 0x00006C50 File Offset: 0x00004E50
		public GalaxyTypeAwareListenerSentFriendInvitationListRetrieve()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerSentFriendInvitationListRetrieve(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060005F3 RID: 1523 RVA: 0x00006C6E File Offset: 0x00004E6E
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerSentFriendInvitationListRetrieve obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060005F4 RID: 1524 RVA: 0x00006C8C File Offset: 0x00004E8C
		~GalaxyTypeAwareListenerSentFriendInvitationListRetrieve()
		{
			this.Dispose();
		}

		// Token: 0x060005F5 RID: 1525 RVA: 0x00006CBC File Offset: 0x00004EBC
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerSentFriendInvitationListRetrieve(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060005F6 RID: 1526 RVA: 0x00006D44 File Offset: 0x00004F44
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerSentFriendInvitationListRetrieve_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400005C RID: 92
		private HandleRef swigCPtr;
	}
}
