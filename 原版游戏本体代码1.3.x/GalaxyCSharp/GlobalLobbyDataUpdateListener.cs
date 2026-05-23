using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000082 RID: 130
	public abstract class GlobalLobbyDataUpdateListener : ILobbyDataUpdateListener
	{
		// Token: 0x06000723 RID: 1827 RVA: 0x00011598 File Offset: 0x0000F798
		internal GlobalLobbyDataUpdateListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalLobbyDataUpdateListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000724 RID: 1828 RVA: 0x000115B4 File Offset: 0x0000F7B4
		public GlobalLobbyDataUpdateListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyDataUpdate.GetListenerType(), this);
			}
		}

		// Token: 0x06000725 RID: 1829 RVA: 0x000115D6 File Offset: 0x0000F7D6
		internal static HandleRef getCPtr(GlobalLobbyDataUpdateListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000726 RID: 1830 RVA: 0x000115F4 File Offset: 0x0000F7F4
		~GlobalLobbyDataUpdateListener()
		{
			this.Dispose();
		}

		// Token: 0x06000727 RID: 1831 RVA: 0x00011624 File Offset: 0x0000F824
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyDataUpdate.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalLobbyDataUpdateListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400009C RID: 156
		private HandleRef swigCPtr;
	}
}
