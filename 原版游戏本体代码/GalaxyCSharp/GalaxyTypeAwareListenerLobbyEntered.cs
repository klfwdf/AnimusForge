using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000035 RID: 53
	public abstract class GalaxyTypeAwareListenerLobbyEntered : IGalaxyListener
	{
		// Token: 0x0600057F RID: 1407 RVA: 0x00005558 File Offset: 0x00003758
		internal GalaxyTypeAwareListenerLobbyEntered(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyEntered_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000580 RID: 1408 RVA: 0x00005574 File Offset: 0x00003774
		public GalaxyTypeAwareListenerLobbyEntered()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerLobbyEntered(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000581 RID: 1409 RVA: 0x00005592 File Offset: 0x00003792
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerLobbyEntered obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000582 RID: 1410 RVA: 0x000055B0 File Offset: 0x000037B0
		~GalaxyTypeAwareListenerLobbyEntered()
		{
			this.Dispose();
		}

		// Token: 0x06000583 RID: 1411 RVA: 0x000055E0 File Offset: 0x000037E0
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerLobbyEntered(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000584 RID: 1412 RVA: 0x00005668 File Offset: 0x00003868
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyEntered_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000049 RID: 73
		private HandleRef swigCPtr;
	}
}
