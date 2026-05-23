using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200003B RID: 59
	public abstract class GalaxyTypeAwareListenerLobbyOwnerChange : IGalaxyListener
	{
		// Token: 0x060005A3 RID: 1443 RVA: 0x00005C90 File Offset: 0x00003E90
		internal GalaxyTypeAwareListenerLobbyOwnerChange(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyOwnerChange_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060005A4 RID: 1444 RVA: 0x00005CAC File Offset: 0x00003EAC
		public GalaxyTypeAwareListenerLobbyOwnerChange()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerLobbyOwnerChange(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060005A5 RID: 1445 RVA: 0x00005CCA File Offset: 0x00003ECA
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerLobbyOwnerChange obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060005A6 RID: 1446 RVA: 0x00005CE8 File Offset: 0x00003EE8
		~GalaxyTypeAwareListenerLobbyOwnerChange()
		{
			this.Dispose();
		}

		// Token: 0x060005A7 RID: 1447 RVA: 0x00005D18 File Offset: 0x00003F18
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerLobbyOwnerChange(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060005A8 RID: 1448 RVA: 0x00005DA0 File Offset: 0x00003FA0
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyOwnerChange_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400004F RID: 79
		private HandleRef swigCPtr;
	}
}
