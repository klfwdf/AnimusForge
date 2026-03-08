using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200002B RID: 43
	public abstract class GalaxyTypeAwareListenerGameJoinRequested : IGalaxyListener
	{
		// Token: 0x06000543 RID: 1347 RVA: 0x00004950 File Offset: 0x00002B50
		internal GalaxyTypeAwareListenerGameJoinRequested(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerGameJoinRequested_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000544 RID: 1348 RVA: 0x0000496C File Offset: 0x00002B6C
		public GalaxyTypeAwareListenerGameJoinRequested()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerGameJoinRequested(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000545 RID: 1349 RVA: 0x0000498A File Offset: 0x00002B8A
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerGameJoinRequested obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000546 RID: 1350 RVA: 0x000049A8 File Offset: 0x00002BA8
		~GalaxyTypeAwareListenerGameJoinRequested()
		{
			this.Dispose();
		}

		// Token: 0x06000547 RID: 1351 RVA: 0x000049D8 File Offset: 0x00002BD8
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerGameJoinRequested(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000548 RID: 1352 RVA: 0x00004A60 File Offset: 0x00002C60
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerGameJoinRequested_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400003F RID: 63
		private HandleRef swigCPtr;
	}
}
