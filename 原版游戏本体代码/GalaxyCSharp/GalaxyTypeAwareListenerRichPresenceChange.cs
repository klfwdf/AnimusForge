using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000045 RID: 69
	public abstract class GalaxyTypeAwareListenerRichPresenceChange : IGalaxyListener
	{
		// Token: 0x060005DF RID: 1503 RVA: 0x00006898 File Offset: 0x00004A98
		internal GalaxyTypeAwareListenerRichPresenceChange(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerRichPresenceChange_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060005E0 RID: 1504 RVA: 0x000068B4 File Offset: 0x00004AB4
		public GalaxyTypeAwareListenerRichPresenceChange()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerRichPresenceChange(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060005E1 RID: 1505 RVA: 0x000068D2 File Offset: 0x00004AD2
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerRichPresenceChange obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060005E2 RID: 1506 RVA: 0x000068F0 File Offset: 0x00004AF0
		~GalaxyTypeAwareListenerRichPresenceChange()
		{
			this.Dispose();
		}

		// Token: 0x060005E3 RID: 1507 RVA: 0x00006920 File Offset: 0x00004B20
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerRichPresenceChange(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060005E4 RID: 1508 RVA: 0x000069A8 File Offset: 0x00004BA8
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerRichPresenceChange_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000059 RID: 89
		private HandleRef swigCPtr;
	}
}
