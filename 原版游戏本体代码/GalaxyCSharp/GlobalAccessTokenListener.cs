using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000065 RID: 101
	public abstract class GlobalAccessTokenListener : IAccessTokenListener
	{
		// Token: 0x0600068D RID: 1677 RVA: 0x0000BC7C File Offset: 0x00009E7C
		internal GlobalAccessTokenListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalAccessTokenListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600068E RID: 1678 RVA: 0x0000BC98 File Offset: 0x00009E98
		public GlobalAccessTokenListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerAccessToken.GetListenerType(), this);
			}
		}

		// Token: 0x0600068F RID: 1679 RVA: 0x0000BCBA File Offset: 0x00009EBA
		internal static HandleRef getCPtr(GlobalAccessTokenListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000690 RID: 1680 RVA: 0x0000BCD8 File Offset: 0x00009ED8
		~GlobalAccessTokenListener()
		{
			this.Dispose();
		}

		// Token: 0x06000691 RID: 1681 RVA: 0x0000BD08 File Offset: 0x00009F08
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerAccessToken.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalAccessTokenListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400007A RID: 122
		private HandleRef swigCPtr;
	}
}
