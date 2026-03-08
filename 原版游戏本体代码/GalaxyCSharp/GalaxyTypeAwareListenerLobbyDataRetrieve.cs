using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000033 RID: 51
	public abstract class GalaxyTypeAwareListenerLobbyDataRetrieve : IGalaxyListener
	{
		// Token: 0x06000573 RID: 1395 RVA: 0x000052F0 File Offset: 0x000034F0
		internal GalaxyTypeAwareListenerLobbyDataRetrieve(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyDataRetrieve_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000574 RID: 1396 RVA: 0x0000530C File Offset: 0x0000350C
		public GalaxyTypeAwareListenerLobbyDataRetrieve()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerLobbyDataRetrieve(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000575 RID: 1397 RVA: 0x0000532A File Offset: 0x0000352A
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerLobbyDataRetrieve obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000576 RID: 1398 RVA: 0x00005348 File Offset: 0x00003548
		~GalaxyTypeAwareListenerLobbyDataRetrieve()
		{
			this.Dispose();
		}

		// Token: 0x06000577 RID: 1399 RVA: 0x00005378 File Offset: 0x00003578
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerLobbyDataRetrieve(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000578 RID: 1400 RVA: 0x00005400 File Offset: 0x00003600
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyDataRetrieve_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000047 RID: 71
		private HandleRef swigCPtr;
	}
}
