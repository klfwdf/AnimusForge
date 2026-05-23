using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000069 RID: 105
	public abstract class GlobalChatRoomMessagesListener : IChatRoomMessagesListener
	{
		// Token: 0x060006A1 RID: 1697 RVA: 0x0000C821 File Offset: 0x0000AA21
		internal GlobalChatRoomMessagesListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalChatRoomMessagesListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006A2 RID: 1698 RVA: 0x0000C83D File Offset: 0x0000AA3D
		public GlobalChatRoomMessagesListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerChatRoomMessages.GetListenerType(), this);
			}
		}

		// Token: 0x060006A3 RID: 1699 RVA: 0x0000C85F File Offset: 0x0000AA5F
		internal static HandleRef getCPtr(GlobalChatRoomMessagesListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006A4 RID: 1700 RVA: 0x0000C880 File Offset: 0x0000AA80
		~GlobalChatRoomMessagesListener()
		{
			this.Dispose();
		}

		// Token: 0x060006A5 RID: 1701 RVA: 0x0000C8B0 File Offset: 0x0000AAB0
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerChatRoomMessages.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalChatRoomMessagesListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400007E RID: 126
		private HandleRef swigCPtr;
	}
}
