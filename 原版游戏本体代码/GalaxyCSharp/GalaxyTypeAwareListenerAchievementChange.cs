using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000018 RID: 24
	public abstract class GalaxyTypeAwareListenerAchievementChange : IGalaxyListener
	{
		// Token: 0x060004D1 RID: 1233 RVA: 0x00003274 File Offset: 0x00001474
		internal GalaxyTypeAwareListenerAchievementChange(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GalaxyTypeAwareListenerAchievementChange_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060004D2 RID: 1234 RVA: 0x00003290 File Offset: 0x00001490
		public GalaxyTypeAwareListenerAchievementChange()
			: this(GalaxyInstancePINVOKE.new_GalaxyTypeAwareListenerAchievementChange(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x060004D3 RID: 1235 RVA: 0x000032AE File Offset: 0x000014AE
		internal static HandleRef getCPtr(GalaxyTypeAwareListenerAchievementChange obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060004D4 RID: 1236 RVA: 0x000032CC File Offset: 0x000014CC
		~GalaxyTypeAwareListenerAchievementChange()
		{
			this.Dispose();
		}

		// Token: 0x060004D5 RID: 1237 RVA: 0x000032FC File Offset: 0x000014FC
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyTypeAwareListenerAchievementChange(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060004D6 RID: 1238 RVA: 0x00003384 File Offset: 0x00001584
		public static ListenerType GetListenerType()
		{
			ListenerType result = (ListenerType)GalaxyInstancePINVOKE.GalaxyTypeAwareListenerAchievementChange_GetListenerType();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400002C RID: 44
		private HandleRef swigCPtr;
	}
}
