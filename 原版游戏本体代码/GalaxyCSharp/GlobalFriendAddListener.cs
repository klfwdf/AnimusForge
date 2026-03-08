using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000071 RID: 113
	public abstract class GlobalFriendAddListener : IFriendAddListener
	{
		// Token: 0x060006C9 RID: 1737 RVA: 0x0000E28B File Offset: 0x0000C48B
		internal GlobalFriendAddListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalFriendAddListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006CA RID: 1738 RVA: 0x0000E2A7 File Offset: 0x0000C4A7
		public GlobalFriendAddListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerFriendAdd.GetListenerType(), this);
			}
		}

		// Token: 0x060006CB RID: 1739 RVA: 0x0000E2C9 File Offset: 0x0000C4C9
		internal static HandleRef getCPtr(GlobalFriendAddListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006CC RID: 1740 RVA: 0x0000E2E8 File Offset: 0x0000C4E8
		~GlobalFriendAddListener()
		{
			this.Dispose();
		}

		// Token: 0x060006CD RID: 1741 RVA: 0x0000E318 File Offset: 0x0000C518
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerFriendAdd.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalFriendAddListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000086 RID: 134
		private HandleRef swigCPtr;
	}
}
