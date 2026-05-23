using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000078 RID: 120
	public abstract class GlobalGameInvitationReceivedListener : IGameInvitationReceivedListener
	{
		// Token: 0x060006F0 RID: 1776 RVA: 0x0000FD0B File Offset: 0x0000DF0B
		internal GlobalGameInvitationReceivedListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalGameInvitationReceivedListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			GlobalGameInvitationReceivedListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006F1 RID: 1777 RVA: 0x0000FD33 File Offset: 0x0000DF33
		public GlobalGameInvitationReceivedListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerGameInvitationReceived.GetListenerType(), this);
			}
		}

		// Token: 0x060006F2 RID: 1778 RVA: 0x0000FD55 File Offset: 0x0000DF55
		internal static HandleRef getCPtr(GlobalGameInvitationReceivedListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006F3 RID: 1779 RVA: 0x0000FD74 File Offset: 0x0000DF74
		~GlobalGameInvitationReceivedListener()
		{
			this.Dispose();
		}

		// Token: 0x060006F4 RID: 1780 RVA: 0x0000FDA4 File Offset: 0x0000DFA4
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerGameInvitationReceived.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalGameInvitationReceivedListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (GlobalGameInvitationReceivedListener.listeners.ContainsKey(handle))
					{
						GlobalGameInvitationReceivedListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000091 RID: 145
		private static Dictionary<IntPtr, GlobalGameInvitationReceivedListener> listeners = new Dictionary<IntPtr, GlobalGameInvitationReceivedListener>();

		// Token: 0x04000092 RID: 146
		private HandleRef swigCPtr;
	}
}
