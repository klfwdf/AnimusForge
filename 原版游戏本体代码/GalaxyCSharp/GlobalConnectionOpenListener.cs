using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200006E RID: 110
	public abstract class GlobalConnectionOpenListener : IConnectionOpenListener
	{
		// Token: 0x060006BA RID: 1722 RVA: 0x0000DA3D File Offset: 0x0000BC3D
		internal GlobalConnectionOpenListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalConnectionOpenListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006BB RID: 1723 RVA: 0x0000DA59 File Offset: 0x0000BC59
		public GlobalConnectionOpenListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerConnectionOpen.GetListenerType(), this);
			}
		}

		// Token: 0x060006BC RID: 1724 RVA: 0x0000DA7B File Offset: 0x0000BC7B
		internal static HandleRef getCPtr(GlobalConnectionOpenListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006BD RID: 1725 RVA: 0x0000DA9C File Offset: 0x0000BC9C
		~GlobalConnectionOpenListener()
		{
			this.Dispose();
		}

		// Token: 0x060006BE RID: 1726 RVA: 0x0000DACC File Offset: 0x0000BCCC
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerConnectionOpen.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalConnectionOpenListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000083 RID: 131
		private HandleRef swigCPtr;
	}
}
