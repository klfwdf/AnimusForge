using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000037 RID: 55
	public abstract class GalaxyTypeAwareListenerLobbyList : IGalaxyListener
	{
		// Token: 0x0600058B RID: 1419 RVA: 0x000057C0 File Offset: 0x000039C0
		internal GalaxyTypeAwareListenerLobbyList(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyList_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600058C RID: 1420 RVA: 0x000057DC File Offset: 0x000039DC
		public GalaxyTypeAwareListenerLobbyList()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerLobbyList(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600058D RID: 1421 RVA: 0x000057FA File Offset: 0x000039FA
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerLobbyList obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600058E RID: 1422 RVA: 0x00005818 File Offset: 0x00003A18
		~GalaxyTypeAwareListenerLobbyList()
		{
			this.Dispose();
		}

		// Token: 0x0600058F RID: 1423 RVA: 0x00005848 File Offset: 0x00003A48
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerLobbyList(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000590 RID: 1424 RVA: 0x000058D0 File Offset: 0x00003AD0
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyList_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400004B RID: 75
		private HandleRef swigCPtr;
	}
}
