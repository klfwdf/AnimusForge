using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000093 RID: 147
	public abstract class GlobalRichPresenceListener : IRichPresenceListener
	{
		// Token: 0x06000779 RID: 1913 RVA: 0x00013F71 File Offset: 0x00012171
		internal GlobalRichPresenceListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalRichPresenceListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600077A RID: 1914 RVA: 0x00013F8D File Offset: 0x0001218D
		public GlobalRichPresenceListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerRichPresence.GetListenerType(), this);
			}
		}

		// Token: 0x0600077B RID: 1915 RVA: 0x00013FAF File Offset: 0x000121AF
		internal static HandleRef getCPtr(GlobalRichPresenceListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600077C RID: 1916 RVA: 0x00013FD0 File Offset: 0x000121D0
		~GlobalRichPresenceListener()
		{
			this.Dispose();
		}

		// Token: 0x0600077D RID: 1917 RVA: 0x00014000 File Offset: 0x00012200
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerRichPresence.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalRichPresenceListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000AE RID: 174
		private HandleRef swigCPtr;
	}
}
