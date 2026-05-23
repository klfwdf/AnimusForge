using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000087 RID: 135
	public abstract class GlobalLobbyMemberStateListener : ILobbyMemberStateListener
	{
		// Token: 0x0600073C RID: 1852 RVA: 0x00011D88 File Offset: 0x0000FF88
		internal GlobalLobbyMemberStateListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalLobbyMemberStateListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600073D RID: 1853 RVA: 0x00011DA4 File Offset: 0x0000FFA4
		public GlobalLobbyMemberStateListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerLobbyMemberState.GetListenerType(), this);
			}
		}

		// Token: 0x0600073E RID: 1854 RVA: 0x00011DC6 File Offset: 0x0000FFC6
		internal static HandleRef getCPtr(GlobalLobbyMemberStateListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600073F RID: 1855 RVA: 0x00011DE4 File Offset: 0x0000FFE4
		~GlobalLobbyMemberStateListener()
		{
			this.Dispose();
		}

		// Token: 0x06000740 RID: 1856 RVA: 0x00011E14 File Offset: 0x00010014
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerLobbyMemberState.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalLobbyMemberStateListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000A1 RID: 161
		private HandleRef swigCPtr;
	}
}
