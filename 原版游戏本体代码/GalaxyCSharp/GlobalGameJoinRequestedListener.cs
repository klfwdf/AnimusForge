using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000079 RID: 121
	public abstract class GlobalGameJoinRequestedListener : IGameJoinRequestedListener
	{
		// Token: 0x060006F6 RID: 1782 RVA: 0x0001009B File Offset: 0x0000E29B
		internal GlobalGameJoinRequestedListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalGameJoinRequestedListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006F7 RID: 1783 RVA: 0x000100B7 File Offset: 0x0000E2B7
		public GlobalGameJoinRequestedListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerGameJoinRequested.GetListenerType(), this);
			}
		}

		// Token: 0x060006F8 RID: 1784 RVA: 0x000100D9 File Offset: 0x0000E2D9
		internal static HandleRef getCPtr(GlobalGameJoinRequestedListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006F9 RID: 1785 RVA: 0x000100F8 File Offset: 0x0000E2F8
		~GlobalGameJoinRequestedListener()
		{
			this.Dispose();
		}

		// Token: 0x060006FA RID: 1786 RVA: 0x00010128 File Offset: 0x0000E328
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerGameJoinRequested.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalGameJoinRequestedListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000093 RID: 147
		private HandleRef swigCPtr;
	}
}
