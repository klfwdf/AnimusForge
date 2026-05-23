using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000060 RID: 96
	public abstract class GameServerGlobalOperationalStateChangeListener : IOperationalStateChangeListener
	{
		// Token: 0x06000673 RID: 1651 RVA: 0x0000ABE5 File Offset: 0x00008DE5
		internal GameServerGlobalOperationalStateChangeListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalOperationalStateChangeListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000674 RID: 1652 RVA: 0x0000AC01 File Offset: 0x00008E01
		public GameServerGlobalOperationalStateChangeListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerOperationalStateChange.GetListenerType(), this);
			}
		}

		// Token: 0x06000675 RID: 1653 RVA: 0x0000AC23 File Offset: 0x00008E23
		internal static HandleRef getCPtr(GameServerGlobalOperationalStateChangeListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000676 RID: 1654 RVA: 0x0000AC44 File Offset: 0x00008E44
		~GameServerGlobalOperationalStateChangeListener()
		{
			this.Dispose();
		}

		// Token: 0x06000677 RID: 1655 RVA: 0x0000AC74 File Offset: 0x00008E74
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerOperationalStateChange.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalOperationalStateChangeListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000074 RID: 116
		private HandleRef swigCPtr;
	}
}
