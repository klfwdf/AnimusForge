using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000090 RID: 144
	public abstract class GlobalOverlayVisibilityChangeListener : IOverlayVisibilityChangeListener
	{
		// Token: 0x0600076A RID: 1898 RVA: 0x00013545 File Offset: 0x00011745
		internal GlobalOverlayVisibilityChangeListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalOverlayVisibilityChangeListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600076B RID: 1899 RVA: 0x00013561 File Offset: 0x00011761
		public GlobalOverlayVisibilityChangeListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerOverlayVisibilityChange.GetListenerType(), this);
			}
		}

		// Token: 0x0600076C RID: 1900 RVA: 0x00013583 File Offset: 0x00011783
		internal static HandleRef getCPtr(GlobalOverlayVisibilityChangeListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600076D RID: 1901 RVA: 0x000135A4 File Offset: 0x000117A4
		~GlobalOverlayVisibilityChangeListener()
		{
			this.Dispose();
		}

		// Token: 0x0600076E RID: 1902 RVA: 0x000135D4 File Offset: 0x000117D4
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerOverlayVisibilityChange.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalOverlayVisibilityChangeListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000AB RID: 171
		private HandleRef swigCPtr;
	}
}
