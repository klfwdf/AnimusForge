using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200002E RID: 46
	public abstract class GalaxyTypeAwareListenerLeaderboardRetrieve : IGalaxyListener
	{
		// Token: 0x06000555 RID: 1365 RVA: 0x00004CEC File Offset: 0x00002EEC
		internal GalaxyTypeAwareListenerLeaderboardRetrieve(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLeaderboardRetrieve_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000556 RID: 1366 RVA: 0x00004D08 File Offset: 0x00002F08
		public GalaxyTypeAwareListenerLeaderboardRetrieve()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerLeaderboardRetrieve(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000557 RID: 1367 RVA: 0x00004D26 File Offset: 0x00002F26
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerLeaderboardRetrieve obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000558 RID: 1368 RVA: 0x00004D44 File Offset: 0x00002F44
		~GalaxyTypeAwareListenerLeaderboardRetrieve()
		{
			this.Dispose();
		}

		// Token: 0x06000559 RID: 1369 RVA: 0x00004D74 File Offset: 0x00002F74
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerLeaderboardRetrieve(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0600055A RID: 1370 RVA: 0x00004DFC File Offset: 0x00002FFC
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLeaderboardRetrieve_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000042 RID: 66
		private HandleRef swigCPtr;
	}
}
