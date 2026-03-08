using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000066 RID: 102
	public abstract class GlobalAchievementChangeListener : IAchievementChangeListener
	{
		// Token: 0x06000692 RID: 1682 RVA: 0x0000BFAD File Offset: 0x0000A1AD
		internal GlobalAchievementChangeListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalAchievementChangeListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000693 RID: 1683 RVA: 0x0000BFC9 File Offset: 0x0000A1C9
		public GlobalAchievementChangeListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerAchievementChange.GetListenerType(), this);
			}
		}

		// Token: 0x06000694 RID: 1684 RVA: 0x0000BFEB File Offset: 0x0000A1EB
		internal static HandleRef getCPtr(GlobalAchievementChangeListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000695 RID: 1685 RVA: 0x0000C00C File Offset: 0x0000A20C
		~GlobalAchievementChangeListener()
		{
			this.Dispose();
		}

		// Token: 0x06000696 RID: 1686 RVA: 0x0000C03C File Offset: 0x0000A23C
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerAchievementChange.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalAchievementChangeListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400007B RID: 123
		private HandleRef swigCPtr;
	}
}
