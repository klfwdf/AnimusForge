using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200006A RID: 106
	public abstract class GlobalChatRoomMessagesRetrieveListener : IChatRoomMessagesRetrieveListener
	{
		// Token: 0x060006A6 RID: 1702 RVA: 0x0000CBFE File Offset: 0x0000ADFE
		internal GlobalChatRoomMessagesRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalChatRoomMessagesRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006A7 RID: 1703 RVA: 0x0000CC1A File Offset: 0x0000AE1A
		public GlobalChatRoomMessagesRetrieveListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerChatRoomMessagesRetrieve.GetListenerType(), this);
			}
		}

		// Token: 0x060006A8 RID: 1704 RVA: 0x0000CC3C File Offset: 0x0000AE3C
		internal static HandleRef getCPtr(GlobalChatRoomMessagesRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006A9 RID: 1705 RVA: 0x0000CC5C File Offset: 0x0000AE5C
		~GlobalChatRoomMessagesRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x060006AA RID: 1706 RVA: 0x0000CC8C File Offset: 0x0000AE8C
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerChatRoomMessagesRetrieve.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalChatRoomMessagesRetrieveListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400007F RID: 127
		private HandleRef swigCPtr;
	}
}
