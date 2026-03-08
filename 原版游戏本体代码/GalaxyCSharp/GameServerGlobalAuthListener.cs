using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000053 RID: 83
	public abstract class GameServerGlobalAuthListener : IAuthListener
	{
		// Token: 0x06000632 RID: 1586 RVA: 0x00007E23 File Offset: 0x00006023
		internal GameServerGlobalAuthListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalAuthListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000633 RID: 1587 RVA: 0x00007E3F File Offset: 0x0000603F
		public GameServerGlobalAuthListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerAuth.GetListenerType(), this);
			}
		}

		// Token: 0x06000634 RID: 1588 RVA: 0x00007E61 File Offset: 0x00006061
		internal static HandleRef getCPtr(GameServerGlobalAuthListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000635 RID: 1589 RVA: 0x00007E80 File Offset: 0x00006080
		~GameServerGlobalAuthListener()
		{
			this.Dispose();
		}

		// Token: 0x06000636 RID: 1590 RVA: 0x00007EB0 File Offset: 0x000060B0
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerAuth.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalAuthListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000067 RID: 103
		private HandleRef swigCPtr;
	}
}
