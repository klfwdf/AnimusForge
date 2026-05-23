using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000085 RID: 133
	public abstract class GlobalLobbyListListener : ILobbyListListener
	{
		// Token: 0x06000732 RID: 1842 RVA: 0x00011B2F File Offset: 0x0000FD2F
		internal GlobalLobbyListListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalLobbyListListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000733 RID: 1843 RVA: 0x00011B4B File Offset: 0x0000FD4B
		public GlobalLobbyListListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyList.GetListenerType(), this);
			}
		}

		// Token: 0x06000734 RID: 1844 RVA: 0x00011B6D File Offset: 0x0000FD6D
		internal static HandleRef getCPtr(GlobalLobbyListListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000735 RID: 1845 RVA: 0x00011B8C File Offset: 0x0000FD8C
		~GlobalLobbyListListener()
		{
			this.Dispose();
		}

		// Token: 0x06000736 RID: 1846 RVA: 0x00011BBC File Offset: 0x0000FDBC
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyList.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalLobbyListListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400009F RID: 159
		private HandleRef swigCPtr;
	}
}
