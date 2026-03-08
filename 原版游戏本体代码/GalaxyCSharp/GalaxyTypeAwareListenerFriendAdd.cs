using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000023 RID: 35
	public abstract class GalaxyTypeAwareListenerFriendAdd : IGalaxyListener
	{
		// Token: 0x06000513 RID: 1299 RVA: 0x00003FB0 File Offset: 0x000021B0
		internal GalaxyTypeAwareListenerFriendAdd(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerFriendAdd_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000514 RID: 1300 RVA: 0x00003FCC File Offset: 0x000021CC
		public GalaxyTypeAwareListenerFriendAdd()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerFriendAdd(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000515 RID: 1301 RVA: 0x00003FEA File Offset: 0x000021EA
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerFriendAdd obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000516 RID: 1302 RVA: 0x00004008 File Offset: 0x00002208
		~GalaxyTypeAwareListenerFriendAdd()
		{
			this.Dispose();
		}

		// Token: 0x06000517 RID: 1303 RVA: 0x00004038 File Offset: 0x00002238
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerFriendAdd(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000518 RID: 1304 RVA: 0x000040C0 File Offset: 0x000022C0
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerFriendAdd_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000037 RID: 55
		private HandleRef swigCPtr;
	}
}
