using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000036 RID: 54
	public abstract class GalaxyTypeAwareListenerLobbyLeft : IGalaxyListener
	{
		// Token: 0x06000585 RID: 1413 RVA: 0x0000568C File Offset: 0x0000388C
		internal GalaxyTypeAwareListenerLobbyLeft(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyLeft_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000586 RID: 1414 RVA: 0x000056A8 File Offset: 0x000038A8
		public GalaxyTypeAwareListenerLobbyLeft()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerLobbyLeft(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000587 RID: 1415 RVA: 0x000056C6 File Offset: 0x000038C6
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerLobbyLeft obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000588 RID: 1416 RVA: 0x000056E4 File Offset: 0x000038E4
		~GalaxyTypeAwareListenerLobbyLeft()
		{
			this.Dispose();
		}

		// Token: 0x06000589 RID: 1417 RVA: 0x00005714 File Offset: 0x00003914
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerLobbyLeft(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0600058A RID: 1418 RVA: 0x0000579C File Offset: 0x0000399C
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerLobbyLeft_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400004A RID: 74
		private HandleRef swigCPtr;
	}
}
