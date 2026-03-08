using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200009E RID: 158
	public abstract class GlobalUserStatsAndAchievementsRetrieveListener : IUserStatsAndAchievementsRetrieveListener
	{
		// Token: 0x060007B3 RID: 1971 RVA: 0x00015EC4 File Offset: 0x000140C4
		internal GlobalUserStatsAndAchievementsRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalUserStatsAndAchievementsRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060007B4 RID: 1972 RVA: 0x00015EE0 File Offset: 0x000140E0
		public GlobalUserStatsAndAchievementsRetrieveListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve.GetListenerType(), this);
			}
		}

		// Token: 0x060007B5 RID: 1973 RVA: 0x00015F02 File Offset: 0x00014102
		internal static HandleRef getCPtr(GlobalUserStatsAndAchievementsRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060007B6 RID: 1974 RVA: 0x00015F20 File Offset: 0x00014120
		~GlobalUserStatsAndAchievementsRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x060007B7 RID: 1975 RVA: 0x00015F50 File Offset: 0x00014150
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalUserStatsAndAchievementsRetrieveListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000BC RID: 188
		private HandleRef swigCPtr;
	}
}
