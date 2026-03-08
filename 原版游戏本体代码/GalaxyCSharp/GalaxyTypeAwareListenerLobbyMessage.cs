using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200003A RID: 58
	public abstract class GalaxyTypeAwareListenerLobbyMessage : IGalaxyListener
	{
		// Token: 0x0600059D RID: 1437 RVA: 0x00005B5C File Offset: 0x00003D5C
		internal GalaxyTypeAwareListenerLobbyMessage(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyMessage_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600059E RID: 1438 RVA: 0x00005B78 File Offset: 0x00003D78
		public GalaxyTypeAwareListenerLobbyMessage()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerLobbyMessage(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600059F RID: 1439 RVA: 0x00005B96 File Offset: 0x00003D96
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerLobbyMessage obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060005A0 RID: 1440 RVA: 0x00005BB4 File Offset: 0x00003DB4
		~GalaxyTypeAwareListenerLobbyMessage()
		{
			this.Dispose();
		}

		// Token: 0x060005A1 RID: 1441 RVA: 0x00005BE4 File Offset: 0x00003DE4
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerLobbyMessage(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060005A2 RID: 1442 RVA: 0x00005C6C File Offset: 0x00003E6C
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyMessage_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400004E RID: 78
		private HandleRef swigCPtr;
	}
}
