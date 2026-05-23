using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000067 RID: 103
	public abstract class GlobalAuthListener : IAuthListener
	{
		// Token: 0x06000697 RID: 1687 RVA: 0x0000C0DC File Offset: 0x0000A2DC
		internal GlobalAuthListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalAuthListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000698 RID: 1688 RVA: 0x0000C0F8 File Offset: 0x0000A2F8
		public GlobalAuthListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerAuth.GetListenerType(), this);
			}
		}

		// Token: 0x06000699 RID: 1689 RVA: 0x0000C11A File Offset: 0x0000A31A
		internal static HandleRef getCPtr(GlobalAuthListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600069A RID: 1690 RVA: 0x0000C138 File Offset: 0x0000A338
		~GlobalAuthListener()
		{
			this.Dispose();
		}

		// Token: 0x0600069B RID: 1691 RVA: 0x0000C168 File Offset: 0x0000A368
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerAuth.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalAuthListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400007C RID: 124
		private HandleRef swigCPtr;
	}
}
