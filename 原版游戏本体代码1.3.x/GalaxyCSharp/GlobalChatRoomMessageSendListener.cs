using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000068 RID: 104
	public abstract class GlobalChatRoomMessageSendListener : IChatRoomMessageSendListener
	{
		// Token: 0x0600069C RID: 1692 RVA: 0x0000C4D4 File Offset: 0x0000A6D4
		internal GlobalChatRoomMessageSendListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalChatRoomMessageSendListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600069D RID: 1693 RVA: 0x0000C4F0 File Offset: 0x0000A6F0
		public GlobalChatRoomMessageSendListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerChatRoomMessageSend.GetListenerType(), this);
			}
		}

		// Token: 0x0600069E RID: 1694 RVA: 0x0000C512 File Offset: 0x0000A712
		internal static HandleRef getCPtr(GlobalChatRoomMessageSendListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600069F RID: 1695 RVA: 0x0000C530 File Offset: 0x0000A730
		~GlobalChatRoomMessageSendListener()
		{
			this.Dispose();
		}

		// Token: 0x060006A0 RID: 1696 RVA: 0x0000C560 File Offset: 0x0000A760
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerChatRoomMessageSend.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalChatRoomMessageSendListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400007D RID: 125
		private HandleRef swigCPtr;
	}
}
