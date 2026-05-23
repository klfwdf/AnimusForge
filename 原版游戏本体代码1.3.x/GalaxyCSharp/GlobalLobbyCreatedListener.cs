using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200007F RID: 127
	public abstract class GlobalLobbyCreatedListener : ILobbyCreatedListener
	{
		// Token: 0x06000714 RID: 1812 RVA: 0x00011214 File Offset: 0x0000F414
		internal GlobalLobbyCreatedListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalLobbyCreatedListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000715 RID: 1813 RVA: 0x00011230 File Offset: 0x0000F430
		public GlobalLobbyCreatedListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyCreated.GetListenerType(), this);
			}
		}

		// Token: 0x06000716 RID: 1814 RVA: 0x00011252 File Offset: 0x0000F452
		internal static HandleRef getCPtr(GlobalLobbyCreatedListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000717 RID: 1815 RVA: 0x00011270 File Offset: 0x0000F470
		~GlobalLobbyCreatedListener()
		{
			this.Dispose();
		}

		// Token: 0x06000718 RID: 1816 RVA: 0x000112A0 File Offset: 0x0000F4A0
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyCreated.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalLobbyCreatedListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000099 RID: 153
		private HandleRef swigCPtr;
	}
}
