using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000038 RID: 56
	public abstract class GalaxyTypeAwareListenerLobbyMemberDataUpdate : IGalaxyListener
	{
		// Token: 0x06000591 RID: 1425 RVA: 0x000058F4 File Offset: 0x00003AF4
		internal GalaxyTypeAwareListenerLobbyMemberDataUpdate(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyMemberDataUpdate_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000592 RID: 1426 RVA: 0x00005910 File Offset: 0x00003B10
		public GalaxyTypeAwareListenerLobbyMemberDataUpdate()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerLobbyMemberDataUpdate(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000593 RID: 1427 RVA: 0x0000592E File Offset: 0x00003B2E
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerLobbyMemberDataUpdate obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000594 RID: 1428 RVA: 0x0000594C File Offset: 0x00003B4C
		~GalaxyTypeAwareListenerLobbyMemberDataUpdate()
		{
			this.Dispose();
		}

		// Token: 0x06000595 RID: 1429 RVA: 0x0000597C File Offset: 0x00003B7C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerLobbyMemberDataUpdate(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000596 RID: 1430 RVA: 0x00005A04 File Offset: 0x00003C04
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyMemberDataUpdate_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400004C RID: 76
		private HandleRef swigCPtr;
	}
}
