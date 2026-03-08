using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000032 RID: 50
	public abstract class GalaxyTypeAwareListenerLobbyData : IGalaxyListener
	{
		// Token: 0x0600056D RID: 1389 RVA: 0x000051BC File Offset: 0x000033BC
		internal GalaxyTypeAwareListenerLobbyData(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyData_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600056E RID: 1390 RVA: 0x000051D8 File Offset: 0x000033D8
		public GalaxyTypeAwareListenerLobbyData()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerLobbyData(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600056F RID: 1391 RVA: 0x000051F6 File Offset: 0x000033F6
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerLobbyData obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000570 RID: 1392 RVA: 0x00005214 File Offset: 0x00003414
		~GalaxyTypeAwareListenerLobbyData()
		{
			this.Dispose();
		}

		// Token: 0x06000571 RID: 1393 RVA: 0x00005244 File Offset: 0x00003444
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerLobbyData(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000572 RID: 1394 RVA: 0x000052CC File Offset: 0x000034CC
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyData_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000046 RID: 70
		private HandleRef swigCPtr;
	}
}
