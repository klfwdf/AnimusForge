using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000049 RID: 73
	public abstract class GalaxyTypeAwareListenerSharedFileDownload : IGalaxyListener
	{
		// Token: 0x060005F7 RID: 1527 RVA: 0x00006D68 File Offset: 0x00004F68
		internal GalaxyTypeAwareListenerSharedFileDownload(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerSharedFileDownload_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060005F8 RID: 1528 RVA: 0x00006D84 File Offset: 0x00004F84
		public GalaxyTypeAwareListenerSharedFileDownload()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerSharedFileDownload(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060005F9 RID: 1529 RVA: 0x00006DA2 File Offset: 0x00004FA2
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerSharedFileDownload obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060005FA RID: 1530 RVA: 0x00006DC0 File Offset: 0x00004FC0
		~GalaxyTypeAwareListenerSharedFileDownload()
		{
			this.Dispose();
		}

		// Token: 0x060005FB RID: 1531 RVA: 0x00006DF0 File Offset: 0x00004FF0
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerSharedFileDownload(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060005FC RID: 1532 RVA: 0x00006E78 File Offset: 0x00005078
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerSharedFileDownload_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400005D RID: 93
		private HandleRef swigCPtr;
	}
}
