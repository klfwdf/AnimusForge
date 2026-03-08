using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000030 RID: 48
	public abstract class GalaxyTypeAwareListenerLeaderboardsRetrieve : IGalaxyListener
	{
		// Token: 0x06000561 RID: 1377 RVA: 0x00004F54 File Offset: 0x00003154
		internal GalaxyTypeAwareListenerLeaderboardsRetrieve(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLeaderboardsRetrieve_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000562 RID: 1378 RVA: 0x00004F70 File Offset: 0x00003170
		public GalaxyTypeAwareListenerLeaderboardsRetrieve()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerLeaderboardsRetrieve(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000563 RID: 1379 RVA: 0x00004F8E File Offset: 0x0000318E
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerLeaderboardsRetrieve obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000564 RID: 1380 RVA: 0x00004FAC File Offset: 0x000031AC
		~GalaxyTypeAwareListenerLeaderboardsRetrieve()
		{
			this.Dispose();
		}

		// Token: 0x06000565 RID: 1381 RVA: 0x00004FDC File Offset: 0x000031DC
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerLeaderboardsRetrieve(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000566 RID: 1382 RVA: 0x00005064 File Offset: 0x00003264
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLeaderboardsRetrieve_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000044 RID: 68
		private HandleRef swigCPtr;
	}
}
