using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000062 RID: 98
	public abstract class GameServerGlobalSpecificUserDataListener : ISpecificUserDataListener
	{
		// Token: 0x0600067D RID: 1661 RVA: 0x0000B351 File Offset: 0x00009551
		internal GameServerGlobalSpecificUserDataListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalSpecificUserDataListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600067E RID: 1662 RVA: 0x0000B36D File Offset: 0x0000956D
		public GameServerGlobalSpecificUserDataListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerSpecificUserData.GetListenerType(), this);
			}
		}

		// Token: 0x0600067F RID: 1663 RVA: 0x0000B38F File Offset: 0x0000958F
		internal static HandleRef getCPtr(GameServerGlobalSpecificUserDataListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000680 RID: 1664 RVA: 0x0000B3B0 File Offset: 0x000095B0
		~GameServerGlobalSpecificUserDataListener()
		{
			this.Dispose();
		}

		// Token: 0x06000681 RID: 1665 RVA: 0x0000B3E0 File Offset: 0x000095E0
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerSpecificUserData.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalSpecificUserDataListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000076 RID: 118
		private HandleRef swigCPtr;
	}
}
