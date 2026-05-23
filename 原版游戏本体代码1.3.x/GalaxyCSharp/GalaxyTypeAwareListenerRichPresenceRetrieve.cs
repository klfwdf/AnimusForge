using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000046 RID: 70
	public abstract class GalaxyTypeAwareListenerRichPresenceRetrieve : IGalaxyListener
	{
		// Token: 0x060005E5 RID: 1509 RVA: 0x000069CC File Offset: 0x00004BCC
		internal GalaxyTypeAwareListenerRichPresenceRetrieve(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerRichPresenceRetrieve_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060005E6 RID: 1510 RVA: 0x000069E8 File Offset: 0x00004BE8
		public GalaxyTypeAwareListenerRichPresenceRetrieve()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerRichPresenceRetrieve(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060005E7 RID: 1511 RVA: 0x00006A06 File Offset: 0x00004C06
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerRichPresenceRetrieve obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060005E8 RID: 1512 RVA: 0x00006A24 File Offset: 0x00004C24
		~GalaxyTypeAwareListenerRichPresenceRetrieve()
		{
			this.Dispose();
		}

		// Token: 0x060005E9 RID: 1513 RVA: 0x00006A54 File Offset: 0x00004C54
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerRichPresenceRetrieve(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060005EA RID: 1514 RVA: 0x00006ADC File Offset: 0x00004CDC
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerRichPresenceRetrieve_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400005A RID: 90
		private HandleRef swigCPtr;
	}
}
