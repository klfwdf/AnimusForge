using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200001D RID: 29
	public abstract class GalaxyTypeAwareListenerChatRoomWithUserRetrieve : IGalaxyListener
	{
		// Token: 0x060004EF RID: 1263 RVA: 0x00003878 File Offset: 0x00001A78
		internal GalaxyTypeAwareListenerChatRoomWithUserRetrieve(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerChatRoomWithUserRetrieve_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060004F0 RID: 1264 RVA: 0x00003894 File Offset: 0x00001A94
		public GalaxyTypeAwareListenerChatRoomWithUserRetrieve()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerChatRoomWithUserRetrieve(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060004F1 RID: 1265 RVA: 0x000038B2 File Offset: 0x00001AB2
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerChatRoomWithUserRetrieve obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060004F2 RID: 1266 RVA: 0x000038D0 File Offset: 0x00001AD0
		~GalaxyTypeAwareListenerChatRoomWithUserRetrieve()
		{
			this.Dispose();
		}

		// Token: 0x060004F3 RID: 1267 RVA: 0x00003900 File Offset: 0x00001B00
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerChatRoomWithUserRetrieve(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060004F4 RID: 1268 RVA: 0x00003988 File Offset: 0x00001B88
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerChatRoomWithUserRetrieve_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000031 RID: 49
		private HandleRef swigCPtr;
	}
}
