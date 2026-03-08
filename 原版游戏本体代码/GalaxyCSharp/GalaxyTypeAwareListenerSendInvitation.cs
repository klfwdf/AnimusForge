using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000047 RID: 71
	public abstract class GalaxyTypeAwareListenerSendInvitation : IGalaxyListener
	{
		// Token: 0x060005EB RID: 1515 RVA: 0x00006B00 File Offset: 0x00004D00
		internal GalaxyTypeAwareListenerSendInvitation(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerSendInvitation_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060005EC RID: 1516 RVA: 0x00006B1C File Offset: 0x00004D1C
		public GalaxyTypeAwareListenerSendInvitation()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerSendInvitation(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060005ED RID: 1517 RVA: 0x00006B3A File Offset: 0x00004D3A
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerSendInvitation obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060005EE RID: 1518 RVA: 0x00006B58 File Offset: 0x00004D58
		~GalaxyTypeAwareListenerSendInvitation()
		{
			this.Dispose();
		}

		// Token: 0x060005EF RID: 1519 RVA: 0x00006B88 File Offset: 0x00004D88
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerSendInvitation(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060005F0 RID: 1520 RVA: 0x00006C10 File Offset: 0x00004E10
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerSendInvitation_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400005B RID: 91
		private HandleRef swigCPtr;
	}
}
