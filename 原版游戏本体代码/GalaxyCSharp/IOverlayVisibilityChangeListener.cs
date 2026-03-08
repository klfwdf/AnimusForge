using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000131 RID: 305
	public abstract class IOverlayVisibilityChangeListener : GalaxyTypeAwareListenerOverlayVisibilityChange
	{
		// Token: 0x06000B7A RID: 2938 RVA: 0x00013324 File Offset: 0x00011524
		internal IOverlayVisibilityChangeListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IOverlayVisibilityChangeListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IOverlayVisibilityChangeListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000B7B RID: 2939 RVA: 0x0001334C File Offset: 0x0001154C
		public IOverlayVisibilityChangeListener()
			: this(GalaxyInstancePINVOKE.new_IOverlayVisibilityChangeListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000B7C RID: 2940 RVA: 0x00013370 File Offset: 0x00011570
		internal static HandleRef getCPtr(IOverlayVisibilityChangeListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000B7D RID: 2941 RVA: 0x00013390 File Offset: 0x00011590
		~IOverlayVisibilityChangeListener()
		{
			this.Dispose();
		}

		// Token: 0x06000B7E RID: 2942 RVA: 0x000133C0 File Offset: 0x000115C0
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IOverlayVisibilityChangeListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IOverlayVisibilityChangeListener.listeners.ContainsKey(handle))
					{
						IOverlayVisibilityChangeListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000B7F RID: 2943 RVA: 0x00013470 File Offset: 0x00011670
		public virtual void OnOverlayVisibilityChanged(bool overlayVisible)
		{
			GalaxyInstancePINVOKE.IOverlayVisibilityChangeListener_OnOverlayVisibilityChanged(this.swigCPtr, overlayVisible);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000B80 RID: 2944 RVA: 0x0001348E File Offset: 0x0001168E
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnOverlayVisibilityChanged", IOverlayVisibilityChangeListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IOverlayVisibilityChangeListener.SwigDelegateIOverlayVisibilityChangeListener_0(IOverlayVisibilityChangeListener.SwigDirectorOnOverlayVisibilityChanged);
			}
			GalaxyInstancePINVOKE.IOverlayVisibilityChangeListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000B81 RID: 2945 RVA: 0x000134C8 File Offset: 0x000116C8
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IOverlayVisibilityChangeListener));
		}

		// Token: 0x06000B82 RID: 2946 RVA: 0x000134FE File Offset: 0x000116FE
		[MonoPInvokeCallback(typeof(IOverlayVisibilityChangeListener.SwigDelegateIOverlayVisibilityChangeListener_0))]
		private static void SwigDirectorOnOverlayVisibilityChanged(IntPtr cPtr, bool overlayVisible)
		{
			if (IOverlayVisibilityChangeListener.listeners.ContainsKey(cPtr))
			{
				IOverlayVisibilityChangeListener.listeners[cPtr].OnOverlayVisibilityChanged(overlayVisible);
			}
		}

		// Token: 0x04000212 RID: 530
		private static Dictionary<IntPtr, IOverlayVisibilityChangeListener> listeners = new Dictionary<IntPtr, IOverlayVisibilityChangeListener>();

		// Token: 0x04000213 RID: 531
		private HandleRef swigCPtr;

		// Token: 0x04000214 RID: 532
		private IOverlayVisibilityChangeListener.SwigDelegateIOverlayVisibilityChangeListener_0 swigDelegate0;

		// Token: 0x04000215 RID: 533
		private static Type[] swigMethodTypes0 = new Type[] { typeof(bool) };

		// Token: 0x02000132 RID: 306
		// (Invoke) Token: 0x06000B85 RID: 2949
		public delegate void SwigDelegateIOverlayVisibilityChangeListener_0(IntPtr cPtr, bool overlayVisible);
	}
}
