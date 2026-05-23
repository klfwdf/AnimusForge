using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200008F RID: 143
	public abstract class GlobalOverlayInitializationStateChangeListener : IOverlayInitializationStateChangeListener
	{
		// Token: 0x06000764 RID: 1892 RVA: 0x000131B5 File Offset: 0x000113B5
		internal GlobalOverlayInitializationStateChangeListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalOverlayInitializationStateChangeListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			GlobalOverlayInitializationStateChangeListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000765 RID: 1893 RVA: 0x000131DD File Offset: 0x000113DD
		public GlobalOverlayInitializationStateChangeListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerOverlayInitializationStateChange.GetListenerType(), this);
			}
		}

		// Token: 0x06000766 RID: 1894 RVA: 0x000131FF File Offset: 0x000113FF
		internal static HandleRef getCPtr(GlobalOverlayInitializationStateChangeListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000767 RID: 1895 RVA: 0x00013220 File Offset: 0x00011420
		~GlobalOverlayInitializationStateChangeListener()
		{
			this.Dispose();
		}

		// Token: 0x06000768 RID: 1896 RVA: 0x00013250 File Offset: 0x00011450
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerOverlayInitializationStateChange.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalOverlayInitializationStateChangeListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (GlobalOverlayInitializationStateChangeListener.listeners.ContainsKey(handle))
					{
						GlobalOverlayInitializationStateChangeListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000A9 RID: 169
		private static Dictionary<IntPtr, GlobalOverlayInitializationStateChangeListener> listeners = new Dictionary<IntPtr, GlobalOverlayInitializationStateChangeListener>();

		// Token: 0x040000AA RID: 170
		private HandleRef swigCPtr;
	}
}
