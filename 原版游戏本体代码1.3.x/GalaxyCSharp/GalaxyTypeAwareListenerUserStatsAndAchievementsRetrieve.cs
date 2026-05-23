using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000050 RID: 80
	public abstract class GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve : IGalaxyListener
	{
		// Token: 0x06000621 RID: 1569 RVA: 0x000075D4 File Offset: 0x000057D4
		internal GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000622 RID: 1570 RVA: 0x000075F0 File Offset: 0x000057F0
		public GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000623 RID: 1571 RVA: 0x0000760E File Offset: 0x0000580E
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000624 RID: 1572 RVA: 0x0000762C File Offset: 0x0000582C
		~GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve()
		{
			this.Dispose();
		}

		// Token: 0x06000625 RID: 1573 RVA: 0x0000765C File Offset: 0x0000585C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000626 RID: 1574 RVA: 0x000076E4 File Offset: 0x000058E4
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000064 RID: 100
		private HandleRef swigCPtr;
	}
}
