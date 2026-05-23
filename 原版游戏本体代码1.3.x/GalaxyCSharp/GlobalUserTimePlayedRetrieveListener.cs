using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200009F RID: 159
	public abstract class GlobalUserTimePlayedRetrieveListener : IUserTimePlayedRetrieveListener
	{
		// Token: 0x060007B8 RID: 1976 RVA: 0x000162A4 File Offset: 0x000144A4
		internal GlobalUserTimePlayedRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalUserTimePlayedRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060007B9 RID: 1977 RVA: 0x000162C0 File Offset: 0x000144C0
		public GlobalUserTimePlayedRetrieveListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerUserTimePlayedRetrieve.GetListenerType(), this);
			}
		}

		// Token: 0x060007BA RID: 1978 RVA: 0x000162E2 File Offset: 0x000144E2
		internal static HandleRef getCPtr(GlobalUserTimePlayedRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060007BB RID: 1979 RVA: 0x00016300 File Offset: 0x00014500
		~GlobalUserTimePlayedRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x060007BC RID: 1980 RVA: 0x00016330 File Offset: 0x00014530
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerUserTimePlayedRetrieve.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalUserTimePlayedRetrieveListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000BD RID: 189
		private HandleRef swigCPtr;
	}
}
