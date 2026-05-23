using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200008D RID: 141
	public abstract class GlobalOperationalStateChangeListener : IOperationalStateChangeListener
	{
		// Token: 0x0600075A RID: 1882 RVA: 0x00012B60 File Offset: 0x00010D60
		internal GlobalOperationalStateChangeListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalOperationalStateChangeListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600075B RID: 1883 RVA: 0x00012B7C File Offset: 0x00010D7C
		public GlobalOperationalStateChangeListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerOperationalStateChange.GetListenerType(), this);
			}
		}

		// Token: 0x0600075C RID: 1884 RVA: 0x00012B9E File Offset: 0x00010D9E
		internal static HandleRef getCPtr(GlobalOperationalStateChangeListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600075D RID: 1885 RVA: 0x00012BBC File Offset: 0x00010DBC
		~GlobalOperationalStateChangeListener()
		{
			this.Dispose();
		}

		// Token: 0x0600075E RID: 1886 RVA: 0x00012BEC File Offset: 0x00010DEC
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerOperationalStateChange.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalOperationalStateChangeListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000A7 RID: 167
		private HandleRef swigCPtr;
	}
}
