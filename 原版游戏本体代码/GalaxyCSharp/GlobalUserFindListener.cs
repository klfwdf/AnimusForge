using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200009C RID: 156
	public abstract class GlobalUserFindListener : IUserFindListener
	{
		// Token: 0x060007A8 RID: 1960 RVA: 0x00015975 File Offset: 0x00013B75
		internal GlobalUserFindListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalUserFindListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060007A9 RID: 1961 RVA: 0x00015991 File Offset: 0x00013B91
		public GlobalUserFindListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerUserFind.GetListenerType(), this);
			}
		}

		// Token: 0x060007AA RID: 1962 RVA: 0x000159B3 File Offset: 0x00013BB3
		internal static HandleRef getCPtr(GlobalUserFindListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060007AB RID: 1963 RVA: 0x000159D4 File Offset: 0x00013BD4
		~GlobalUserFindListener()
		{
			this.Dispose();
		}

		// Token: 0x060007AC RID: 1964 RVA: 0x00015A04 File Offset: 0x00013C04
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerUserFind.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalUserFindListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000B9 RID: 185
		private HandleRef swigCPtr;
	}
}
