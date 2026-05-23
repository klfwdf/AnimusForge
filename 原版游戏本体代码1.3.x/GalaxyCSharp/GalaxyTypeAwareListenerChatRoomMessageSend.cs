using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200001B RID: 27
	public abstract class GalaxyTypeAwareListenerChatRoomMessageSend : IGalaxyListener
	{
		// Token: 0x060004E3 RID: 1251 RVA: 0x00003610 File Offset: 0x00001810
		internal GalaxyTypeAwareListenerChatRoomMessageSend(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerChatRoomMessageSend_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060004E4 RID: 1252 RVA: 0x0000362C File Offset: 0x0000182C
		public GalaxyTypeAwareListenerChatRoomMessageSend()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerChatRoomMessageSend(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060004E5 RID: 1253 RVA: 0x0000364A File Offset: 0x0000184A
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerChatRoomMessageSend obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060004E6 RID: 1254 RVA: 0x00003668 File Offset: 0x00001868
		~GalaxyTypeAwareListenerChatRoomMessageSend()
		{
			this.Dispose();
		}

		// Token: 0x060004E7 RID: 1255 RVA: 0x00003698 File Offset: 0x00001898
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerChatRoomMessageSend(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060004E8 RID: 1256 RVA: 0x00003720 File Offset: 0x00001920
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerChatRoomMessageSend_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400002F RID: 47
		private HandleRef swigCPtr;
	}
}
