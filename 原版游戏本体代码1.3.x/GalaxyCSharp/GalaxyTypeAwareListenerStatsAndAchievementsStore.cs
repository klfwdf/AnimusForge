using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200004B RID: 75
	public abstract class GalaxyTypeAwareListenerStatsAndAchievementsStore : IGalaxyListener
	{
		// Token: 0x06000603 RID: 1539 RVA: 0x00006FD0 File Offset: 0x000051D0
		internal GalaxyTypeAwareListenerStatsAndAchievementsStore(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerStatsAndAchievementsStore_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000604 RID: 1540 RVA: 0x00006FEC File Offset: 0x000051EC
		public GalaxyTypeAwareListenerStatsAndAchievementsStore()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerStatsAndAchievementsStore(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000605 RID: 1541 RVA: 0x0000700A File Offset: 0x0000520A
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerStatsAndAchievementsStore obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000606 RID: 1542 RVA: 0x00007028 File Offset: 0x00005228
		~GalaxyTypeAwareListenerStatsAndAchievementsStore()
		{
			this.Dispose();
		}

		// Token: 0x06000607 RID: 1543 RVA: 0x00007058 File Offset: 0x00005258
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerStatsAndAchievementsStore(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000608 RID: 1544 RVA: 0x000070E0 File Offset: 0x000052E0
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerStatsAndAchievementsStore_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400005F RID: 95
		private HandleRef swigCPtr;
	}
}
