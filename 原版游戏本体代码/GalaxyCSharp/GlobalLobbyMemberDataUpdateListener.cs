using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000086 RID: 134
	public abstract class GlobalLobbyMemberDataUpdateListener : ILobbyMemberDataUpdateListener
	{
		// Token: 0x06000737 RID: 1847 RVA: 0x00011C5C File Offset: 0x0000FE5C
		internal GlobalLobbyMemberDataUpdateListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalLobbyMemberDataUpdateListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000738 RID: 1848 RVA: 0x00011C78 File Offset: 0x0000FE78
		public GlobalLobbyMemberDataUpdateListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyMemberDataUpdate.GetListenerType(), this);
			}
		}

		// Token: 0x06000739 RID: 1849 RVA: 0x00011C9A File Offset: 0x0000FE9A
		internal static HandleRef getCPtr(GlobalLobbyMemberDataUpdateListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600073A RID: 1850 RVA: 0x00011CB8 File Offset: 0x0000FEB8
		~GlobalLobbyMemberDataUpdateListener()
		{
			this.Dispose();
		}

		// Token: 0x0600073B RID: 1851 RVA: 0x00011CE8 File Offset: 0x0000FEE8
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyMemberDataUpdate.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalLobbyMemberDataUpdateListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000A0 RID: 160
		private HandleRef swigCPtr;
	}
}
