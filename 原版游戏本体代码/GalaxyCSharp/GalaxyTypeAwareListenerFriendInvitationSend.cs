using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000028 RID: 40
	public abstract class GalaxyTypeAwareListenerFriendInvitationSend : IGalaxyListener
	{
		// Token: 0x06000531 RID: 1329 RVA: 0x000045B4 File Offset: 0x000027B4
		internal GalaxyTypeAwareListenerFriendInvitationSend(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerFriendInvitationSend_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000532 RID: 1330 RVA: 0x000045D0 File Offset: 0x000027D0
		public GalaxyTypeAwareListenerFriendInvitationSend()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerFriendInvitationSend(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000533 RID: 1331 RVA: 0x000045EE File Offset: 0x000027EE
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerFriendInvitationSend obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000534 RID: 1332 RVA: 0x0000460C File Offset: 0x0000280C
		~GalaxyTypeAwareListenerFriendInvitationSend()
		{
			this.Dispose();
		}

		// Token: 0x06000535 RID: 1333 RVA: 0x0000463C File Offset: 0x0000283C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerFriendInvitationSend(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000536 RID: 1334 RVA: 0x000046C4 File Offset: 0x000028C4
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerFriendInvitationSend_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400003C RID: 60
		private HandleRef swigCPtr;
	}
}
