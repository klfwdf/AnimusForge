using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000074 RID: 116
	public abstract class GlobalFriendInvitationListRetrieveListener : IFriendInvitationListRetrieveListener
	{
		// Token: 0x060006D9 RID: 1753 RVA: 0x0000ED92 File Offset: 0x0000CF92
		internal GlobalFriendInvitationListRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalFriendInvitationListRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			GlobalFriendInvitationListRetrieveListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006DA RID: 1754 RVA: 0x0000EDBA File Offset: 0x0000CFBA
		public GlobalFriendInvitationListRetrieveListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerFriendInvitationListRetrieve.GetListenerType(), this);
			}
		}

		// Token: 0x060006DB RID: 1755 RVA: 0x0000EDDC File Offset: 0x0000CFDC
		internal static HandleRef getCPtr(GlobalFriendInvitationListRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006DC RID: 1756 RVA: 0x0000EDFC File Offset: 0x0000CFFC
		~GlobalFriendInvitationListRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x060006DD RID: 1757 RVA: 0x0000EE2C File Offset: 0x0000D02C
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerFriendInvitationListRetrieve.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalFriendInvitationListRetrieveListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (GlobalFriendInvitationListRetrieveListener.listeners.ContainsKey(handle))
					{
						GlobalFriendInvitationListRetrieveListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400008A RID: 138
		private static Dictionary<IntPtr, GlobalFriendInvitationListRetrieveListener> listeners = new Dictionary<IntPtr, GlobalFriendInvitationListRetrieveListener>();

		// Token: 0x0400008B RID: 139
		private HandleRef swigCPtr;
	}
}
