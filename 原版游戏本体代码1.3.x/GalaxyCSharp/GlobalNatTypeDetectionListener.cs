using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200008A RID: 138
	public abstract class GlobalNatTypeDetectionListener : INatTypeDetectionListener
	{
		// Token: 0x0600074B RID: 1867 RVA: 0x000125B6 File Offset: 0x000107B6
		internal GlobalNatTypeDetectionListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalNatTypeDetectionListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600074C RID: 1868 RVA: 0x000125D2 File Offset: 0x000107D2
		public GlobalNatTypeDetectionListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerNatTypeDetection.GetListenerType(), this);
			}
		}

		// Token: 0x0600074D RID: 1869 RVA: 0x000125F4 File Offset: 0x000107F4
		internal static HandleRef getCPtr(GlobalNatTypeDetectionListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600074E RID: 1870 RVA: 0x00012614 File Offset: 0x00010814
		~GlobalNatTypeDetectionListener()
		{
			this.Dispose();
		}

		// Token: 0x0600074F RID: 1871 RVA: 0x00012644 File Offset: 0x00010844
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerNatTypeDetection.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalNatTypeDetectionListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000A4 RID: 164
		private HandleRef swigCPtr;
	}
}
