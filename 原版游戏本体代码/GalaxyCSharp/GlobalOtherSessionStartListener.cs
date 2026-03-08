using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200008E RID: 142
	public abstract class GlobalOtherSessionStartListener : IOtherSessionStartListener
	{
		// Token: 0x0600075F RID: 1887 RVA: 0x00012E83 File Offset: 0x00011083
		internal GlobalOtherSessionStartListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalOtherSessionStartListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000760 RID: 1888 RVA: 0x00012E9F File Offset: 0x0001109F
		public GlobalOtherSessionStartListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerOtherSessionStart.GetListenerType(), this);
			}
		}

		// Token: 0x06000761 RID: 1889 RVA: 0x00012EC1 File Offset: 0x000110C1
		internal static HandleRef getCPtr(GlobalOtherSessionStartListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000762 RID: 1890 RVA: 0x00012EE0 File Offset: 0x000110E0
		~GlobalOtherSessionStartListener()
		{
			this.Dispose();
		}

		// Token: 0x06000763 RID: 1891 RVA: 0x00012F10 File Offset: 0x00011110
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerOtherSessionStart.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalOtherSessionStartListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000A8 RID: 168
		private HandleRef swigCPtr;
	}
}
