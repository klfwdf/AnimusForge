using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000034 RID: 52
	public abstract class GalaxyTypeAwareListenerLobbyDataUpdate : IGalaxyListener
	{
		// Token: 0x06000579 RID: 1401 RVA: 0x00005424 File Offset: 0x00003624
		internal GalaxyTypeAwareListenerLobbyDataUpdate(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyDataUpdate_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600057A RID: 1402 RVA: 0x00005440 File Offset: 0x00003640
		public GalaxyTypeAwareListenerLobbyDataUpdate()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerLobbyDataUpdate(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600057B RID: 1403 RVA: 0x0000545E File Offset: 0x0000365E
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerLobbyDataUpdate obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600057C RID: 1404 RVA: 0x0000547C File Offset: 0x0000367C
		~GalaxyTypeAwareListenerLobbyDataUpdate()
		{
			this.Dispose();
		}

		// Token: 0x0600057D RID: 1405 RVA: 0x000054AC File Offset: 0x000036AC
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerLobbyDataUpdate(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0600057E RID: 1406 RVA: 0x00005534 File Offset: 0x00003734
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyDataUpdate_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000048 RID: 72
		private HandleRef swigCPtr;
	}
}
