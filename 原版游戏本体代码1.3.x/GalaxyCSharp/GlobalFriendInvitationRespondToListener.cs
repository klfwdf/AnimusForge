using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000075 RID: 117
	public abstract class GlobalFriendInvitationRespondToListener : IFriendInvitationRespondToListener
	{
		// Token: 0x060006DF RID: 1759 RVA: 0x0000F1C1 File Offset: 0x0000D3C1
		internal GlobalFriendInvitationRespondToListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalFriendInvitationRespondToListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			GlobalFriendInvitationRespondToListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006E0 RID: 1760 RVA: 0x0000F1E9 File Offset: 0x0000D3E9
		public GlobalFriendInvitationRespondToListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerFriendInvitationRespondTo.GetListenerType(), this);
			}
		}

		// Token: 0x060006E1 RID: 1761 RVA: 0x0000F20B File Offset: 0x0000D40B
		internal static HandleRef getCPtr(GlobalFriendInvitationRespondToListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006E2 RID: 1762 RVA: 0x0000F22C File Offset: 0x0000D42C
		~GlobalFriendInvitationRespondToListener()
		{
			this.Dispose();
		}

		// Token: 0x060006E3 RID: 1763 RVA: 0x0000F25C File Offset: 0x0000D45C
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerFriendInvitationRespondTo.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalFriendInvitationRespondToListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (GlobalFriendInvitationRespondToListener.listeners.ContainsKey(handle))
					{
						GlobalFriendInvitationRespondToListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400008C RID: 140
		private static Dictionary<IntPtr, GlobalFriendInvitationRespondToListener> listeners = new Dictionary<IntPtr, GlobalFriendInvitationRespondToListener>();

		// Token: 0x0400008D RID: 141
		private HandleRef swigCPtr;
	}
}
