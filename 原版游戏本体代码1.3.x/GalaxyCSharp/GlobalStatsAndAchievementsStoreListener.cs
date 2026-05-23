using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000099 RID: 153
	public abstract class GlobalStatsAndAchievementsStoreListener : IStatsAndAchievementsStoreListener
	{
		// Token: 0x06000799 RID: 1945 RVA: 0x00015146 File Offset: 0x00013346
		internal GlobalStatsAndAchievementsStoreListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalStatsAndAchievementsStoreListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600079A RID: 1946 RVA: 0x00015162 File Offset: 0x00013362
		public GlobalStatsAndAchievementsStoreListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerStatsAndAchievementsStore.GetListenerType(), this);
			}
		}

		// Token: 0x0600079B RID: 1947 RVA: 0x00015184 File Offset: 0x00013384
		internal static HandleRef getCPtr(GlobalStatsAndAchievementsStoreListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600079C RID: 1948 RVA: 0x000151A4 File Offset: 0x000133A4
		~GlobalStatsAndAchievementsStoreListener()
		{
			this.Dispose();
		}

		// Token: 0x0600079D RID: 1949 RVA: 0x000151D4 File Offset: 0x000133D4
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerStatsAndAchievementsStore.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalStatsAndAchievementsStoreListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000B6 RID: 182
		private HandleRef swigCPtr;
	}
}
