using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000081 RID: 129
	public abstract class GlobalLobbyDataRetrieveListener : ILobbyDataRetrieveListener
	{
		// Token: 0x0600071E RID: 1822 RVA: 0x0001146C File Offset: 0x0000F66C
		internal GlobalLobbyDataRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalLobbyDataRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600071F RID: 1823 RVA: 0x00011488 File Offset: 0x0000F688
		public GlobalLobbyDataRetrieveListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyDataRetrieve.GetListenerType(), this);
			}
		}

		// Token: 0x06000720 RID: 1824 RVA: 0x000114AA File Offset: 0x0000F6AA
		internal static HandleRef getCPtr(GlobalLobbyDataRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000721 RID: 1825 RVA: 0x000114C8 File Offset: 0x0000F6C8
		~GlobalLobbyDataRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x06000722 RID: 1826 RVA: 0x000114F8 File Offset: 0x0000F6F8
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyDataRetrieve.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalLobbyDataRetrieveListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400009B RID: 155
		private HandleRef swigCPtr;
	}
}
