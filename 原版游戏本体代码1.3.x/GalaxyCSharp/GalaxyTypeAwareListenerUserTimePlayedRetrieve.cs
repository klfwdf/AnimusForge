using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000051 RID: 81
	public abstract class GalaxyTypeAwareListenerUserTimePlayedRetrieve : IGalaxyListener
	{
		// Token: 0x06000627 RID: 1575 RVA: 0x00007708 File Offset: 0x00005908
		internal GalaxyTypeAwareListenerUserTimePlayedRetrieve(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerUserTimePlayedRetrieve_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000628 RID: 1576 RVA: 0x00007724 File Offset: 0x00005924
		public GalaxyTypeAwareListenerUserTimePlayedRetrieve()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerUserTimePlayedRetrieve(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000629 RID: 1577 RVA: 0x00007742 File Offset: 0x00005942
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerUserTimePlayedRetrieve obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600062A RID: 1578 RVA: 0x00007760 File Offset: 0x00005960
		~GalaxyTypeAwareListenerUserTimePlayedRetrieve()
		{
			this.Dispose();
		}

		// Token: 0x0600062B RID: 1579 RVA: 0x00007790 File Offset: 0x00005990
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerUserTimePlayedRetrieve(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0600062C RID: 1580 RVA: 0x00007818 File Offset: 0x00005A18
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerUserTimePlayedRetrieve_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000065 RID: 101
		private HandleRef swigCPtr;
	}
}
