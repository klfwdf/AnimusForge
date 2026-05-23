using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000083 RID: 131
	public abstract class GlobalLobbyEnteredListener : ILobbyEnteredListener
	{
		// Token: 0x06000728 RID: 1832 RVA: 0x000116C4 File Offset: 0x0000F8C4
		internal GlobalLobbyEnteredListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalLobbyEnteredListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000729 RID: 1833 RVA: 0x000116E0 File Offset: 0x0000F8E0
		public GlobalLobbyEnteredListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyEntered.GetListenerType(), this);
			}
		}

		// Token: 0x0600072A RID: 1834 RVA: 0x00011702 File Offset: 0x0000F902
		internal static HandleRef getCPtr(GlobalLobbyEnteredListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600072B RID: 1835 RVA: 0x00011720 File Offset: 0x0000F920
		~GlobalLobbyEnteredListener()
		{
			this.Dispose();
		}

		// Token: 0x0600072C RID: 1836 RVA: 0x00011750 File Offset: 0x0000F950
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyEntered.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalLobbyEnteredListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400009D RID: 157
		private HandleRef swigCPtr;
	}
}
