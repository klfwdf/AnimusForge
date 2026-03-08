using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000076 RID: 118
	public abstract class GlobalFriendInvitationSendListener : IFriendInvitationSendListener
	{
		// Token: 0x060006E5 RID: 1765 RVA: 0x0000F5E4 File Offset: 0x0000D7E4
		internal GlobalFriendInvitationSendListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalFriendInvitationSendListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			GlobalFriendInvitationSendListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006E6 RID: 1766 RVA: 0x0000F60C File Offset: 0x0000D80C
		public GlobalFriendInvitationSendListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerFriendInvitationSend.GetListenerType(), this);
			}
		}

		// Token: 0x060006E7 RID: 1767 RVA: 0x0000F62E File Offset: 0x0000D82E
		internal static HandleRef getCPtr(GlobalFriendInvitationSendListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006E8 RID: 1768 RVA: 0x0000F64C File Offset: 0x0000D84C
		~GlobalFriendInvitationSendListener()
		{
			this.Dispose();
		}

		// Token: 0x060006E9 RID: 1769 RVA: 0x0000F67C File Offset: 0x0000D87C
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerFriendInvitationSend.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalFriendInvitationSendListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (GlobalFriendInvitationSendListener.listeners.ContainsKey(handle))
					{
						GlobalFriendInvitationSendListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400008E RID: 142
		private static Dictionary<IntPtr, GlobalFriendInvitationSendListener> listeners = new Dictionary<IntPtr, GlobalFriendInvitationSendListener>();

		// Token: 0x0400008F RID: 143
		private HandleRef swigCPtr;
	}
}
