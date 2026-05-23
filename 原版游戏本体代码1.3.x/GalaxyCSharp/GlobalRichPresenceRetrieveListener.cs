using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000094 RID: 148
	public abstract class GlobalRichPresenceRetrieveListener : IRichPresenceRetrieveListener
	{
		// Token: 0x0600077E RID: 1918 RVA: 0x000140A0 File Offset: 0x000122A0
		internal GlobalRichPresenceRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalRichPresenceRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600077F RID: 1919 RVA: 0x000140BC File Offset: 0x000122BC
		public GlobalRichPresenceRetrieveListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerRichPresenceRetrieve.GetListenerType(), this);
			}
		}

		// Token: 0x06000780 RID: 1920 RVA: 0x000140DE File Offset: 0x000122DE
		internal static HandleRef getCPtr(GlobalRichPresenceRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000781 RID: 1921 RVA: 0x000140FC File Offset: 0x000122FC
		~GlobalRichPresenceRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x06000782 RID: 1922 RVA: 0x0001412C File Offset: 0x0001232C
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerRichPresenceRetrieve.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalRichPresenceRetrieveListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000AF RID: 175
		private HandleRef swigCPtr;
	}
}
