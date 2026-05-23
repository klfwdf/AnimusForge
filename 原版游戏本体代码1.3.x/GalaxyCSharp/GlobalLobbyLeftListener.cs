using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000084 RID: 132
	public abstract class GlobalLobbyLeftListener : ILobbyLeftListener
	{
		// Token: 0x0600072D RID: 1837 RVA: 0x000117F0 File Offset: 0x0000F9F0
		internal GlobalLobbyLeftListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalLobbyLeftListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600072E RID: 1838 RVA: 0x0001180C File Offset: 0x0000FA0C
		public GlobalLobbyLeftListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyLeft.GetListenerType(), this);
			}
		}

		// Token: 0x0600072F RID: 1839 RVA: 0x0001182E File Offset: 0x0000FA2E
		internal static HandleRef getCPtr(GlobalLobbyLeftListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000730 RID: 1840 RVA: 0x0001184C File Offset: 0x0000FA4C
		~GlobalLobbyLeftListener()
		{
			this.Dispose();
		}

		// Token: 0x06000731 RID: 1841 RVA: 0x0001187C File Offset: 0x0000FA7C
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyLeft.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalLobbyLeftListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400009E RID: 158
		private HandleRef swigCPtr;
	}
}
