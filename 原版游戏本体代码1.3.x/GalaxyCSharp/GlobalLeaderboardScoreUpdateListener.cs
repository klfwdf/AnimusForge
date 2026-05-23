using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200007D RID: 125
	public abstract class GlobalLeaderboardScoreUpdateListener : ILeaderboardScoreUpdateListener
	{
		// Token: 0x0600070A RID: 1802 RVA: 0x00010D50 File Offset: 0x0000EF50
		internal GlobalLeaderboardScoreUpdateListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalLeaderboardScoreUpdateListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600070B RID: 1803 RVA: 0x00010D6C File Offset: 0x0000EF6C
		public GlobalLeaderboardScoreUpdateListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerLeaderboardScoreUpdate.GetListenerType(), this);
			}
		}

		// Token: 0x0600070C RID: 1804 RVA: 0x00010D8E File Offset: 0x0000EF8E
		internal static HandleRef getCPtr(GlobalLeaderboardScoreUpdateListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600070D RID: 1805 RVA: 0x00010DAC File Offset: 0x0000EFAC
		~GlobalLeaderboardScoreUpdateListener()
		{
			this.Dispose();
		}

		// Token: 0x0600070E RID: 1806 RVA: 0x00010DDC File Offset: 0x0000EFDC
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerLeaderboardScoreUpdate.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalLeaderboardScoreUpdateListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000097 RID: 151
		private HandleRef swigCPtr;
	}
}
