using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000096 RID: 150
	public abstract class GlobalSentFriendInvitationListRetrieveListener : ISentFriendInvitationListRetrieveListener
	{
		// Token: 0x06000789 RID: 1929 RVA: 0x00014872 File Offset: 0x00012A72
		internal GlobalSentFriendInvitationListRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalSentFriendInvitationListRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			GlobalSentFriendInvitationListRetrieveListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600078A RID: 1930 RVA: 0x0001489A File Offset: 0x00012A9A
		public GlobalSentFriendInvitationListRetrieveListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerSentFriendInvitationListRetrieve.GetListenerType(), this);
			}
		}

		// Token: 0x0600078B RID: 1931 RVA: 0x000148BC File Offset: 0x00012ABC
		internal static HandleRef getCPtr(GlobalSentFriendInvitationListRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600078C RID: 1932 RVA: 0x000148DC File Offset: 0x00012ADC
		~GlobalSentFriendInvitationListRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x0600078D RID: 1933 RVA: 0x0001490C File Offset: 0x00012B0C
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerSentFriendInvitationListRetrieve.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalSentFriendInvitationListRetrieveListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (GlobalSentFriendInvitationListRetrieveListener.listeners.ContainsKey(handle))
					{
						GlobalSentFriendInvitationListRetrieveListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000B2 RID: 178
		private static Dictionary<IntPtr, GlobalSentFriendInvitationListRetrieveListener> listeners = new Dictionary<IntPtr, GlobalSentFriendInvitationListRetrieveListener>();

		// Token: 0x040000B3 RID: 179
		private HandleRef swigCPtr;
	}
}
