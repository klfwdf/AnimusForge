using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000070 RID: 112
	public abstract class GlobalFileShareListener : IFileShareListener
	{
		// Token: 0x060006C4 RID: 1732 RVA: 0x0000DF39 File Offset: 0x0000C139
		internal GlobalFileShareListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalFileShareListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006C5 RID: 1733 RVA: 0x0000DF55 File Offset: 0x0000C155
		public GlobalFileShareListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerFileShare.GetListenerType(), this);
			}
		}

		// Token: 0x060006C6 RID: 1734 RVA: 0x0000DF77 File Offset: 0x0000C177
		internal static HandleRef getCPtr(GlobalFileShareListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006C7 RID: 1735 RVA: 0x0000DF98 File Offset: 0x0000C198
		~GlobalFileShareListener()
		{
			this.Dispose();
		}

		// Token: 0x060006C8 RID: 1736 RVA: 0x0000DFC8 File Offset: 0x0000C1C8
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerFileShare.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalFileShareListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000085 RID: 133
		private HandleRef swigCPtr;
	}
}
