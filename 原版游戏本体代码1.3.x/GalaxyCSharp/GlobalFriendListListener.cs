using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000077 RID: 119
	public abstract class GlobalFriendListListener : IFriendListListener
	{
		// Token: 0x060006EB RID: 1771 RVA: 0x0000F9BA File Offset: 0x0000DBBA
		internal GlobalFriendListListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalFriendListListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006EC RID: 1772 RVA: 0x0000F9D6 File Offset: 0x0000DBD6
		public GlobalFriendListListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerFriendList.GetListenerType(), this);
			}
		}

		// Token: 0x060006ED RID: 1773 RVA: 0x0000F9F8 File Offset: 0x0000DBF8
		internal static HandleRef getCPtr(GlobalFriendListListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006EE RID: 1774 RVA: 0x0000FA18 File Offset: 0x0000DC18
		~GlobalFriendListListener()
		{
			this.Dispose();
		}

		// Token: 0x060006EF RID: 1775 RVA: 0x0000FA48 File Offset: 0x0000DC48
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerFriendList.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalFriendListListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000090 RID: 144
		private HandleRef swigCPtr;
	}
}
