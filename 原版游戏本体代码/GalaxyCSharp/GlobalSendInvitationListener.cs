using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000095 RID: 149
	public abstract class GlobalSendInvitationListener : ISendInvitationListener
	{
		// Token: 0x06000783 RID: 1923 RVA: 0x0001449A File Offset: 0x0001269A
		internal GlobalSendInvitationListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalSendInvitationListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			GlobalSendInvitationListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000784 RID: 1924 RVA: 0x000144C2 File Offset: 0x000126C2
		public GlobalSendInvitationListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerSendInvitation.GetListenerType(), this);
			}
		}

		// Token: 0x06000785 RID: 1925 RVA: 0x000144E4 File Offset: 0x000126E4
		internal static HandleRef getCPtr(GlobalSendInvitationListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000786 RID: 1926 RVA: 0x00014504 File Offset: 0x00012704
		~GlobalSendInvitationListener()
		{
			this.Dispose();
		}

		// Token: 0x06000787 RID: 1927 RVA: 0x00014534 File Offset: 0x00012734
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerSendInvitation.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalSendInvitationListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (GlobalSendInvitationListener.listeners.ContainsKey(handle))
					{
						GlobalSendInvitationListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000B0 RID: 176
		private static Dictionary<IntPtr, GlobalSendInvitationListener> listeners = new Dictionary<IntPtr, GlobalSendInvitationListener>();

		// Token: 0x040000B1 RID: 177
		private HandleRef swigCPtr;
	}
}
