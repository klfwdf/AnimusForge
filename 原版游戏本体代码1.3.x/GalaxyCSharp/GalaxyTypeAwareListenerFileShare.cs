using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000022 RID: 34
	public abstract class GalaxyTypeAwareListenerFileShare : IGalaxyListener
	{
		// Token: 0x0600050D RID: 1293 RVA: 0x00003E7C File Offset: 0x0000207C
		internal GalaxyTypeAwareListenerFileShare(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerFileShare_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600050E RID: 1294 RVA: 0x00003E98 File Offset: 0x00002098
		public GalaxyTypeAwareListenerFileShare()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerFileShare(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600050F RID: 1295 RVA: 0x00003EB6 File Offset: 0x000020B6
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerFileShare obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000510 RID: 1296 RVA: 0x00003ED4 File Offset: 0x000020D4
		~GalaxyTypeAwareListenerFileShare()
		{
			this.Dispose();
		}

		// Token: 0x06000511 RID: 1297 RVA: 0x00003F04 File Offset: 0x00002104
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerFileShare(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000512 RID: 1298 RVA: 0x00003F8C File Offset: 0x0000218C
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerFileShare_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000036 RID: 54
		private HandleRef swigCPtr;
	}
}
