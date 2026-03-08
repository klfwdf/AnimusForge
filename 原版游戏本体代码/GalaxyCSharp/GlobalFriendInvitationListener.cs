using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000073 RID: 115
	public abstract class GlobalFriendInvitationListener : IFriendInvitationListener
	{
		// Token: 0x060006D3 RID: 1747 RVA: 0x0000E9BB File Offset: 0x0000CBBB
		internal GlobalFriendInvitationListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalFriendInvitationListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			GlobalFriendInvitationListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006D4 RID: 1748 RVA: 0x0000E9E3 File Offset: 0x0000CBE3
		public GlobalFriendInvitationListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerFriendInvitation.GetListenerType(), this);
			}
		}

		// Token: 0x060006D5 RID: 1749 RVA: 0x0000EA05 File Offset: 0x0000CC05
		internal static HandleRef getCPtr(GlobalFriendInvitationListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006D6 RID: 1750 RVA: 0x0000EA24 File Offset: 0x0000CC24
		~GlobalFriendInvitationListener()
		{
			this.Dispose();
		}

		// Token: 0x060006D7 RID: 1751 RVA: 0x0000EA54 File Offset: 0x0000CC54
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerFriendInvitation.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalFriendInvitationListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (GlobalFriendInvitationListener.listeners.ContainsKey(handle))
					{
						GlobalFriendInvitationListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000088 RID: 136
		private static Dictionary<IntPtr, GlobalFriendInvitationListener> listeners = new Dictionary<IntPtr, GlobalFriendInvitationListener>();

		// Token: 0x04000089 RID: 137
		private HandleRef swigCPtr;
	}
}
