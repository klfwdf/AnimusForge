using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000089 RID: 137
	public abstract class GlobalLobbyOwnerChangeListener : ILobbyOwnerChangeListener
	{
		// Token: 0x06000746 RID: 1862 RVA: 0x00012220 File Offset: 0x00010420
		internal GlobalLobbyOwnerChangeListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalLobbyOwnerChangeListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000747 RID: 1863 RVA: 0x0001223C File Offset: 0x0001043C
		public GlobalLobbyOwnerChangeListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyOwnerChange.GetListenerType(), this);
			}
		}

		// Token: 0x06000748 RID: 1864 RVA: 0x0001225E File Offset: 0x0001045E
		internal static HandleRef getCPtr(GlobalLobbyOwnerChangeListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000749 RID: 1865 RVA: 0x0001227C File Offset: 0x0001047C
		~GlobalLobbyOwnerChangeListener()
		{
			this.Dispose();
		}

		// Token: 0x0600074A RID: 1866 RVA: 0x000122AC File Offset: 0x000104AC
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyOwnerChange.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalLobbyOwnerChangeListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000A3 RID: 163
		private HandleRef swigCPtr;
	}
}
