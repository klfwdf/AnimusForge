using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000020 RID: 32
	public abstract class GalaxyTypeAwareListenerConnectionOpen : IGalaxyListener
	{
		// Token: 0x06000501 RID: 1281 RVA: 0x00003C14 File Offset: 0x00001E14
		internal GalaxyTypeAwareListenerConnectionOpen(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerConnectionOpen_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000502 RID: 1282 RVA: 0x00003C30 File Offset: 0x00001E30
		public GalaxyTypeAwareListenerConnectionOpen()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerConnectionOpen(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000503 RID: 1283 RVA: 0x00003C4E File Offset: 0x00001E4E
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerConnectionOpen obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000504 RID: 1284 RVA: 0x00003C6C File Offset: 0x00001E6C
		~GalaxyTypeAwareListenerConnectionOpen()
		{
			this.Dispose();
		}

		// Token: 0x06000505 RID: 1285 RVA: 0x00003C9C File Offset: 0x00001E9C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerConnectionOpen(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000506 RID: 1286 RVA: 0x00003D24 File Offset: 0x00001F24
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerConnectionOpen_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000034 RID: 52
		private HandleRef swigCPtr;
	}
}
