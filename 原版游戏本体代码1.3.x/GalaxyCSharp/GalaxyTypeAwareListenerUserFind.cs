using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200004E RID: 78
	public abstract class GalaxyTypeAwareListenerUserFind : IGalaxyListener
	{
		// Token: 0x06000615 RID: 1557 RVA: 0x0000736C File Offset: 0x0000556C
		internal GalaxyTypeAwareListenerUserFind(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerUserFind_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000616 RID: 1558 RVA: 0x00007388 File Offset: 0x00005588
		public GalaxyTypeAwareListenerUserFind()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerUserFind(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000617 RID: 1559 RVA: 0x000073A6 File Offset: 0x000055A6
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerUserFind obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000618 RID: 1560 RVA: 0x000073C4 File Offset: 0x000055C4
		~GalaxyTypeAwareListenerUserFind()
		{
			this.Dispose();
		}

		// Token: 0x06000619 RID: 1561 RVA: 0x000073F4 File Offset: 0x000055F4
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerUserFind(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0600061A RID: 1562 RVA: 0x0000747C File Offset: 0x0000567C
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerUserFind_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000062 RID: 98
		private HandleRef swigCPtr;
	}
}
