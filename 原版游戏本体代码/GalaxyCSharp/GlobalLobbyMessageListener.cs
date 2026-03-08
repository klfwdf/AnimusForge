using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000088 RID: 136
	public abstract class GlobalLobbyMessageListener : ILobbyMessageListener
	{
		// Token: 0x06000741 RID: 1857 RVA: 0x00011EB4 File Offset: 0x000100B4
		internal GlobalLobbyMessageListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalLobbyMessageListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000742 RID: 1858 RVA: 0x00011ED0 File Offset: 0x000100D0
		public GlobalLobbyMessageListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyMessage.GetListenerType(), this);
			}
		}

		// Token: 0x06000743 RID: 1859 RVA: 0x00011EF2 File Offset: 0x000100F2
		internal static HandleRef getCPtr(GlobalLobbyMessageListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000744 RID: 1860 RVA: 0x00011F10 File Offset: 0x00010110
		~GlobalLobbyMessageListener()
		{
			this.Dispose();
		}

		// Token: 0x06000745 RID: 1861 RVA: 0x00011F40 File Offset: 0x00010140
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyMessage.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalLobbyMessageListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000A2 RID: 162
		private HandleRef swigCPtr;
	}
}
