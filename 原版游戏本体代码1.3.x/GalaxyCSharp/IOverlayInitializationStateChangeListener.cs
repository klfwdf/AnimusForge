using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200012F RID: 303
	public abstract class IOverlayInitializationStateChangeListener : GalaxyTypeAwareListenerOverlayInitializationStateChange
	{
		// Token: 0x06000B6C RID: 2924 RVA: 0x00012FB0 File Offset: 0x000111B0
		internal IOverlayInitializationStateChangeListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IOverlayInitializationStateChangeListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IOverlayInitializationStateChangeListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000B6D RID: 2925 RVA: 0x00012FD8 File Offset: 0x000111D8
		public IOverlayInitializationStateChangeListener()
			: this(GalaxyInstancePINVOKE.new_IOverlayInitializationStateChangeListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000B6E RID: 2926 RVA: 0x00012FFC File Offset: 0x000111FC
		internal static HandleRef getCPtr(IOverlayInitializationStateChangeListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000B6F RID: 2927 RVA: 0x0001301C File Offset: 0x0001121C
		~IOverlayInitializationStateChangeListener()
		{
			this.Dispose();
		}

		// Token: 0x06000B70 RID: 2928 RVA: 0x0001304C File Offset: 0x0001124C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IOverlayInitializationStateChangeListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IOverlayInitializationStateChangeListener.listeners.ContainsKey(handle))
					{
						IOverlayInitializationStateChangeListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000B71 RID: 2929
		public abstract void OnOverlayStateChanged(OverlayState overlayState);

		// Token: 0x06000B72 RID: 2930 RVA: 0x000130FC File Offset: 0x000112FC
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnOverlayStateChanged", IOverlayInitializationStateChangeListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IOverlayInitializationStateChangeListener.SwigDelegateIOverlayInitializationStateChangeListener_0(IOverlayInitializationStateChangeListener.SwigDirectorOnOverlayStateChanged);
			}
			GalaxyInstancePINVOKE.IOverlayInitializationStateChangeListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000B73 RID: 2931 RVA: 0x00013138 File Offset: 0x00011338
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IOverlayInitializationStateChangeListener));
		}

		// Token: 0x06000B74 RID: 2932 RVA: 0x0001316E File Offset: 0x0001136E
		[MonoPInvokeCallback(typeof(IOverlayInitializationStateChangeListener.SwigDelegateIOverlayInitializationStateChangeListener_0))]
		private static void SwigDirectorOnOverlayStateChanged(IntPtr cPtr, int overlayState)
		{
			if (IOverlayInitializationStateChangeListener.listeners.ContainsKey(cPtr))
			{
				IOverlayInitializationStateChangeListener.listeners[cPtr].OnOverlayStateChanged((OverlayState)overlayState);
			}
		}

		// Token: 0x0400020E RID: 526
		private static Dictionary<IntPtr, IOverlayInitializationStateChangeListener> listeners = new Dictionary<IntPtr, IOverlayInitializationStateChangeListener>();

		// Token: 0x0400020F RID: 527
		private HandleRef swigCPtr;

		// Token: 0x04000210 RID: 528
		private IOverlayInitializationStateChangeListener.SwigDelegateIOverlayInitializationStateChangeListener_0 swigDelegate0;

		// Token: 0x04000211 RID: 529
		private static Type[] swigMethodTypes0 = new Type[] { typeof(OverlayState) };

		// Token: 0x02000130 RID: 304
		// (Invoke) Token: 0x06000B77 RID: 2935
		public delegate void SwigDelegateIOverlayInitializationStateChangeListener_0(IntPtr cPtr, int overlayState);
	}
}
