using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000029 RID: 41
	public abstract class GalaxyTypeAwareListenerFriendList : IGalaxyListener
	{
		// Token: 0x06000537 RID: 1335 RVA: 0x000046E8 File Offset: 0x000028E8
		internal GalaxyTypeAwareListenerFriendList(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerFriendList_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000538 RID: 1336 RVA: 0x00004704 File Offset: 0x00002904
		public GalaxyTypeAwareListenerFriendList()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerFriendList(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000539 RID: 1337 RVA: 0x00004722 File Offset: 0x00002922
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerFriendList obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600053A RID: 1338 RVA: 0x00004740 File Offset: 0x00002940
		~GalaxyTypeAwareListenerFriendList()
		{
			this.Dispose();
		}

		// Token: 0x0600053B RID: 1339 RVA: 0x00004770 File Offset: 0x00002970
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerFriendList(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0600053C RID: 1340 RVA: 0x000047F8 File Offset: 0x000029F8
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerFriendList_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400003D RID: 61
		private HandleRef swigCPtr;
	}
}
