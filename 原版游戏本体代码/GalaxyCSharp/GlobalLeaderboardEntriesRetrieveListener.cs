using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200007B RID: 123
	public abstract class GlobalLeaderboardEntriesRetrieveListener : ILeaderboardEntriesRetrieveListener
	{
		// Token: 0x06000700 RID: 1792 RVA: 0x00010595 File Offset: 0x0000E795
		internal GlobalLeaderboardEntriesRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalLeaderboardEntriesRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000701 RID: 1793 RVA: 0x000105B1 File Offset: 0x0000E7B1
		public GlobalLeaderboardEntriesRetrieveListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerLeaderboardEntriesRetrieve.GetListenerType(), this);
			}
		}

		// Token: 0x06000702 RID: 1794 RVA: 0x000105D3 File Offset: 0x0000E7D3
		internal static HandleRef getCPtr(GlobalLeaderboardEntriesRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000703 RID: 1795 RVA: 0x000105F4 File Offset: 0x0000E7F4
		~GlobalLeaderboardEntriesRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x06000704 RID: 1796 RVA: 0x00010624 File Offset: 0x0000E824
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerLeaderboardEntriesRetrieve.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalLeaderboardEntriesRetrieveListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000095 RID: 149
		private HandleRef swigCPtr;
	}
}
