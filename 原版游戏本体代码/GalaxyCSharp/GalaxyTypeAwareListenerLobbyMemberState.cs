using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000039 RID: 57
	public abstract class GalaxyTypeAwareListenerLobbyMemberState : IGalaxyListener
	{
		// Token: 0x06000597 RID: 1431 RVA: 0x00005A28 File Offset: 0x00003C28
		internal GalaxyTypeAwareListenerLobbyMemberState(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyMemberState_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000598 RID: 1432 RVA: 0x00005A44 File Offset: 0x00003C44
		public GalaxyTypeAwareListenerLobbyMemberState()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerLobbyMemberState(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000599 RID: 1433 RVA: 0x00005A62 File Offset: 0x00003C62
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerLobbyMemberState obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600059A RID: 1434 RVA: 0x00005A80 File Offset: 0x00003C80
		~GalaxyTypeAwareListenerLobbyMemberState()
		{
			this.Dispose();
		}

		// Token: 0x0600059B RID: 1435 RVA: 0x00005AB0 File Offset: 0x00003CB0
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerLobbyMemberState(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0600059C RID: 1436 RVA: 0x00005B38 File Offset: 0x00003D38
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyMemberState_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400004D RID: 77
		private HandleRef swigCPtr;
	}
}
