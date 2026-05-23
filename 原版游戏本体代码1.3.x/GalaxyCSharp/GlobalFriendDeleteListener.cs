using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000072 RID: 114
	public abstract class GlobalFriendDeleteListener : IFriendDeleteListener
	{
		// Token: 0x060006CE RID: 1742 RVA: 0x0000E66C File Offset: 0x0000C86C
		internal GlobalFriendDeleteListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalFriendDeleteListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006CF RID: 1743 RVA: 0x0000E688 File Offset: 0x0000C888
		public GlobalFriendDeleteListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerFriendDelete.GetListenerType(), this);
			}
		}

		// Token: 0x060006D0 RID: 1744 RVA: 0x0000E6AA File Offset: 0x0000C8AA
		internal static HandleRef getCPtr(GlobalFriendDeleteListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006D1 RID: 1745 RVA: 0x0000E6C8 File Offset: 0x0000C8C8
		~GlobalFriendDeleteListener()
		{
			this.Dispose();
		}

		// Token: 0x060006D2 RID: 1746 RVA: 0x0000E6F8 File Offset: 0x0000C8F8
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerFriendDelete.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalFriendDeleteListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000087 RID: 135
		private HandleRef swigCPtr;
	}
}
