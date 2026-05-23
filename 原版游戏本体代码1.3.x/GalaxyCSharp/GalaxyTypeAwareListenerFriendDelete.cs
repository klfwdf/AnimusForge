using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000024 RID: 36
	public abstract class GalaxyTypeAwareListenerFriendDelete : IGalaxyListener
	{
		// Token: 0x06000519 RID: 1305 RVA: 0x000040E4 File Offset: 0x000022E4
		internal GalaxyTypeAwareListenerFriendDelete(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerFriendDelete_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600051A RID: 1306 RVA: 0x00004100 File Offset: 0x00002300
		public GalaxyTypeAwareListenerFriendDelete()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerFriendDelete(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600051B RID: 1307 RVA: 0x0000411E File Offset: 0x0000231E
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerFriendDelete obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600051C RID: 1308 RVA: 0x0000413C File Offset: 0x0000233C
		~GalaxyTypeAwareListenerFriendDelete()
		{
			this.Dispose();
		}

		// Token: 0x0600051D RID: 1309 RVA: 0x0000416C File Offset: 0x0000236C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerFriendDelete(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0600051E RID: 1310 RVA: 0x000041F4 File Offset: 0x000023F4
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerFriendDelete_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000038 RID: 56
		private HandleRef swigCPtr;
	}
}
