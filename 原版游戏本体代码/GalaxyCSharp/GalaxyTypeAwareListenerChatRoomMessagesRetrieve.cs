using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200001C RID: 28
	public abstract class GalaxyTypeAwareListenerChatRoomMessagesRetrieve : IGalaxyListener
	{
		// Token: 0x060004E9 RID: 1257 RVA: 0x00003744 File Offset: 0x00001944
		internal GalaxyTypeAwareListenerChatRoomMessagesRetrieve(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerChatRoomMessagesRetrieve_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060004EA RID: 1258 RVA: 0x00003760 File Offset: 0x00001960
		public GalaxyTypeAwareListenerChatRoomMessagesRetrieve()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerChatRoomMessagesRetrieve(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060004EB RID: 1259 RVA: 0x0000377E File Offset: 0x0000197E
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerChatRoomMessagesRetrieve obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060004EC RID: 1260 RVA: 0x0000379C File Offset: 0x0000199C
		~GalaxyTypeAwareListenerChatRoomMessagesRetrieve()
		{
			this.Dispose();
		}

		// Token: 0x060004ED RID: 1261 RVA: 0x000037CC File Offset: 0x000019CC
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerChatRoomMessagesRetrieve(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060004EE RID: 1262 RVA: 0x00003854 File Offset: 0x00001A54
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerChatRoomMessagesRetrieve_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000030 RID: 48
		private HandleRef swigCPtr;
	}
}
