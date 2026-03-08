using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200006C RID: 108
	public abstract class GlobalConnectionCloseListener : IConnectionCloseListener
	{
		// Token: 0x060006B0 RID: 1712 RVA: 0x0000D32F File Offset: 0x0000B52F
		internal GlobalConnectionCloseListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalConnectionCloseListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006B1 RID: 1713 RVA: 0x0000D34B File Offset: 0x0000B54B
		public GlobalConnectionCloseListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerConnectionClose.GetListenerType(), this);
			}
		}

		// Token: 0x060006B2 RID: 1714 RVA: 0x0000D36D File Offset: 0x0000B56D
		internal static HandleRef getCPtr(GlobalConnectionCloseListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006B3 RID: 1715 RVA: 0x0000D38C File Offset: 0x0000B58C
		~GlobalConnectionCloseListener()
		{
			this.Dispose();
		}

		// Token: 0x060006B4 RID: 1716 RVA: 0x0000D3BC File Offset: 0x0000B5BC
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerConnectionClose.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalConnectionCloseListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000081 RID: 129
		private HandleRef swigCPtr;
	}
}
