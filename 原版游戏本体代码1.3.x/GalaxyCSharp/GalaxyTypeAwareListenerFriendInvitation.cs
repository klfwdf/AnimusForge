using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000025 RID: 37
	public abstract class GalaxyTypeAwareListenerFriendInvitation : IGalaxyListener
	{
		// Token: 0x0600051F RID: 1311 RVA: 0x00004218 File Offset: 0x00002418
		internal GalaxyTypeAwareListenerFriendInvitation(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerFriendInvitation_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000520 RID: 1312 RVA: 0x00004234 File Offset: 0x00002434
		public GalaxyTypeAwareListenerFriendInvitation()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerFriendInvitation(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000521 RID: 1313 RVA: 0x00004252 File Offset: 0x00002452
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerFriendInvitation obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000522 RID: 1314 RVA: 0x00004270 File Offset: 0x00002470
		~GalaxyTypeAwareListenerFriendInvitation()
		{
			this.Dispose();
		}

		// Token: 0x06000523 RID: 1315 RVA: 0x000042A0 File Offset: 0x000024A0
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerFriendInvitation(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000524 RID: 1316 RVA: 0x00004328 File Offset: 0x00002528
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerFriendInvitation_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000039 RID: 57
		private HandleRef swigCPtr;
	}
}
