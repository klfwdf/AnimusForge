using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200006B RID: 107
	public abstract class GlobalChatRoomWithUserRetrieveListener : IChatRoomWithUserRetrieveListener
	{
		// Token: 0x060006AB RID: 1707 RVA: 0x0000CFED File Offset: 0x0000B1ED
		internal GlobalChatRoomWithUserRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalChatRoomWithUserRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006AC RID: 1708 RVA: 0x0000D009 File Offset: 0x0000B209
		public GlobalChatRoomWithUserRetrieveListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerChatRoomWithUserRetrieve.GetListenerType(), this);
			}
		}

		// Token: 0x060006AD RID: 1709 RVA: 0x0000D02B File Offset: 0x0000B22B
		internal static HandleRef getCPtr(GlobalChatRoomWithUserRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006AE RID: 1710 RVA: 0x0000D04C File Offset: 0x0000B24C
		~GlobalChatRoomWithUserRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x060006AF RID: 1711 RVA: 0x0000D07C File Offset: 0x0000B27C
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerChatRoomWithUserRetrieve.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalChatRoomWithUserRetrieveListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000080 RID: 128
		private HandleRef swigCPtr;
	}
}
