using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000044 RID: 68
	public abstract class GalaxyTypeAwareListenerRichPresence : IGalaxyListener
	{
		// Token: 0x060005D9 RID: 1497 RVA: 0x00006764 File Offset: 0x00004964
		internal GalaxyTypeAwareListenerRichPresence(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerRichPresence_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060005DA RID: 1498 RVA: 0x00006780 File Offset: 0x00004980
		public GalaxyTypeAwareListenerRichPresence()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerRichPresence(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060005DB RID: 1499 RVA: 0x0000679E File Offset: 0x0000499E
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerRichPresence obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060005DC RID: 1500 RVA: 0x000067BC File Offset: 0x000049BC
		~GalaxyTypeAwareListenerRichPresence()
		{
			this.Dispose();
		}

		// Token: 0x060005DD RID: 1501 RVA: 0x000067EC File Offset: 0x000049EC
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerRichPresence(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060005DE RID: 1502 RVA: 0x00006874 File Offset: 0x00004A74
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerRichPresence_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000058 RID: 88
		private HandleRef swigCPtr;
	}
}
