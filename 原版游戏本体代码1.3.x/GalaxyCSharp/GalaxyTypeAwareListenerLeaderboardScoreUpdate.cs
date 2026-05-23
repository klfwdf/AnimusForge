using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200002F RID: 47
	public abstract class GalaxyTypeAwareListenerLeaderboardScoreUpdate : IGalaxyListener
	{
		// Token: 0x0600055B RID: 1371 RVA: 0x00004E20 File Offset: 0x00003020
		internal GalaxyTypeAwareListenerLeaderboardScoreUpdate(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLeaderboardScoreUpdate_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600055C RID: 1372 RVA: 0x00004E3C File Offset: 0x0000303C
		public GalaxyTypeAwareListenerLeaderboardScoreUpdate()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerLeaderboardScoreUpdate(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600055D RID: 1373 RVA: 0x00004E5A File Offset: 0x0000305A
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerLeaderboardScoreUpdate obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600055E RID: 1374 RVA: 0x00004E78 File Offset: 0x00003078
		~GalaxyTypeAwareListenerLeaderboardScoreUpdate()
		{
			this.Dispose();
		}

		// Token: 0x0600055F RID: 1375 RVA: 0x00004EA8 File Offset: 0x000030A8
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerLeaderboardScoreUpdate(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000560 RID: 1376 RVA: 0x00004F30 File Offset: 0x00003130
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLeaderboardScoreUpdate_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000043 RID: 67
		private HandleRef swigCPtr;
	}
}
