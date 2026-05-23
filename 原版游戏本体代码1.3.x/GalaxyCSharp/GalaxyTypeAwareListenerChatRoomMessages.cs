using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200001A RID: 26
	public abstract class GalaxyTypeAwareListenerChatRoomMessages : IGalaxyListener
	{
		// Token: 0x060004DD RID: 1245 RVA: 0x000034DC File Offset: 0x000016DC
		internal GalaxyTypeAwareListenerChatRoomMessages(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerChatRoomMessages_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060004DE RID: 1246 RVA: 0x000034F8 File Offset: 0x000016F8
		public GalaxyTypeAwareListenerChatRoomMessages()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerChatRoomMessages(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060004DF RID: 1247 RVA: 0x00003516 File Offset: 0x00001716
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerChatRoomMessages obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060004E0 RID: 1248 RVA: 0x00003534 File Offset: 0x00001734
		~GalaxyTypeAwareListenerChatRoomMessages()
		{
			this.Dispose();
		}

		// Token: 0x060004E1 RID: 1249 RVA: 0x00003564 File Offset: 0x00001764
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerChatRoomMessages(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060004E2 RID: 1250 RVA: 0x000035EC File Offset: 0x000017EC
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerChatRoomMessages_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400002E RID: 46
		private HandleRef swigCPtr;
	}
}
