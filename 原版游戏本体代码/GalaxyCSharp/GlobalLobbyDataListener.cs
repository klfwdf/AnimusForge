using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000080 RID: 128
	public abstract class GlobalLobbyDataListener : ILobbyDataListener
	{
		// Token: 0x06000719 RID: 1817 RVA: 0x00011340 File Offset: 0x0000F540
		internal GlobalLobbyDataListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalLobbyDataListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600071A RID: 1818 RVA: 0x0001135C File Offset: 0x0000F55C
		public GlobalLobbyDataListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyData.GetListenerType(), this);
			}
		}

		// Token: 0x0600071B RID: 1819 RVA: 0x0001137E File Offset: 0x0000F57E
		internal static HandleRef getCPtr(GlobalLobbyDataListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600071C RID: 1820 RVA: 0x0001139C File Offset: 0x0000F59C
		~GlobalLobbyDataListener()
		{
			this.Dispose();
		}

		// Token: 0x0600071D RID: 1821 RVA: 0x000113CC File Offset: 0x0000F5CC
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyData.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalLobbyDataListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400009A RID: 154
		private HandleRef swigCPtr;
	}
}
