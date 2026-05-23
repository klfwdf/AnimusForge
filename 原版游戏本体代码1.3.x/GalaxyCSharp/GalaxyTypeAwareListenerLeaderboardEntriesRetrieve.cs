using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200002D RID: 45
	public abstract class GalaxyTypeAwareListenerLeaderboardEntriesRetrieve : IGalaxyListener
	{
		// Token: 0x0600054F RID: 1359 RVA: 0x00004BB8 File Offset: 0x00002DB8
		internal GalaxyTypeAwareListenerLeaderboardEntriesRetrieve(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLeaderboardEntriesRetrieve_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000550 RID: 1360 RVA: 0x00004BD4 File Offset: 0x00002DD4
		public GalaxyTypeAwareListenerLeaderboardEntriesRetrieve()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerLeaderboardEntriesRetrieve(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000551 RID: 1361 RVA: 0x00004BF2 File Offset: 0x00002DF2
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerLeaderboardEntriesRetrieve obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000552 RID: 1362 RVA: 0x00004C10 File Offset: 0x00002E10
		~GalaxyTypeAwareListenerLeaderboardEntriesRetrieve()
		{
			this.Dispose();
		}

		// Token: 0x06000553 RID: 1363 RVA: 0x00004C40 File Offset: 0x00002E40
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerLeaderboardEntriesRetrieve(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000554 RID: 1364 RVA: 0x00004CC8 File Offset: 0x00002EC8
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLeaderboardEntriesRetrieve_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000041 RID: 65
		private HandleRef swigCPtr;
	}
}
