using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000098 RID: 152
	public abstract class GlobalSpecificUserDataListener : ISpecificUserDataListener
	{
		// Token: 0x06000794 RID: 1940 RVA: 0x00014DB0 File Offset: 0x00012FB0
		internal GlobalSpecificUserDataListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalSpecificUserDataListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000795 RID: 1941 RVA: 0x00014DCC File Offset: 0x00012FCC
		public GlobalSpecificUserDataListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerSpecificUserData.GetListenerType(), this);
			}
		}

		// Token: 0x06000796 RID: 1942 RVA: 0x00014DEE File Offset: 0x00012FEE
		internal static HandleRef getCPtr(GlobalSpecificUserDataListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000797 RID: 1943 RVA: 0x00014E0C File Offset: 0x0001300C
		~GlobalSpecificUserDataListener()
		{
			this.Dispose();
		}

		// Token: 0x06000798 RID: 1944 RVA: 0x00014E3C File Offset: 0x0001303C
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerSpecificUserData.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalSpecificUserDataListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000B5 RID: 181
		private HandleRef swigCPtr;
	}
}
