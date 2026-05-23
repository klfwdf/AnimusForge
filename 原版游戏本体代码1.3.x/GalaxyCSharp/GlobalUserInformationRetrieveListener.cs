using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200009D RID: 157
	public abstract class GlobalUserInformationRetrieveListener : IUserInformationRetrieveListener
	{
		// Token: 0x060007AD RID: 1965 RVA: 0x00015AA4 File Offset: 0x00013CA4
		internal GlobalUserInformationRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalUserInformationRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			GlobalUserInformationRetrieveListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060007AE RID: 1966 RVA: 0x00015ACC File Offset: 0x00013CCC
		public GlobalUserInformationRetrieveListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerUserInformationRetrieve.GetListenerType(), this);
			}
		}

		// Token: 0x060007AF RID: 1967 RVA: 0x00015AEE File Offset: 0x00013CEE
		internal static HandleRef getCPtr(GlobalUserInformationRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060007B0 RID: 1968 RVA: 0x00015B0C File Offset: 0x00013D0C
		~GlobalUserInformationRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x060007B1 RID: 1969 RVA: 0x00015B3C File Offset: 0x00013D3C
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerUserInformationRetrieve.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalUserInformationRetrieveListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (GlobalUserInformationRetrieveListener.listeners.ContainsKey(handle))
					{
						GlobalUserInformationRetrieveListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000BA RID: 186
		private static Dictionary<IntPtr, GlobalUserInformationRetrieveListener> listeners = new Dictionary<IntPtr, GlobalUserInformationRetrieveListener>();

		// Token: 0x040000BB RID: 187
		private HandleRef swigCPtr;
	}
}
