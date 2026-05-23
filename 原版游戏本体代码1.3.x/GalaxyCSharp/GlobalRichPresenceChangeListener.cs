using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000092 RID: 146
	public abstract class GlobalRichPresenceChangeListener : IRichPresenceChangeListener
	{
		// Token: 0x06000774 RID: 1908 RVA: 0x00013C2E File Offset: 0x00011E2E
		internal GlobalRichPresenceChangeListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalRichPresenceChangeListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000775 RID: 1909 RVA: 0x00013C4A File Offset: 0x00011E4A
		public GlobalRichPresenceChangeListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerRichPresenceChange.GetListenerType(), this);
			}
		}

		// Token: 0x06000776 RID: 1910 RVA: 0x00013C6C File Offset: 0x00011E6C
		internal static HandleRef getCPtr(GlobalRichPresenceChangeListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000777 RID: 1911 RVA: 0x00013C8C File Offset: 0x00011E8C
		~GlobalRichPresenceChangeListener()
		{
			this.Dispose();
		}

		// Token: 0x06000778 RID: 1912 RVA: 0x00013CBC File Offset: 0x00011EBC
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerRichPresenceChange.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalRichPresenceChangeListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000AD RID: 173
		private HandleRef swigCPtr;
	}
}
