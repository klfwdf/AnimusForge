using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000ED RID: 237
	public abstract class IGogServicesConnectionStateListener : GalaxyTypeAwareListenerGogServicesConnectionState
	{
		// Token: 0x060009A2 RID: 2466 RVA: 0x00008308 File Offset: 0x00006508
		internal IGogServicesConnectionStateListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IGogServicesConnectionStateListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IGogServicesConnectionStateListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060009A3 RID: 2467 RVA: 0x00008330 File Offset: 0x00006530
		public IGogServicesConnectionStateListener()
			: this(GalaxyInstancePINVOKE.new_IGogServicesConnectionStateListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x060009A4 RID: 2468 RVA: 0x00008354 File Offset: 0x00006554
		internal static HandleRef getCPtr(IGogServicesConnectionStateListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060009A5 RID: 2469 RVA: 0x00008374 File Offset: 0x00006574
		~IGogServicesConnectionStateListener()
		{
			this.Dispose();
		}

		// Token: 0x060009A6 RID: 2470 RVA: 0x000083A4 File Offset: 0x000065A4
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IGogServicesConnectionStateListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IGogServicesConnectionStateListener.listeners.ContainsKey(handle))
					{
						IGogServicesConnectionStateListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060009A7 RID: 2471
		public abstract void OnConnectionStateChange(GogServicesConnectionState connectionState);

		// Token: 0x060009A8 RID: 2472 RVA: 0x00008454 File Offset: 0x00006654
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnConnectionStateChange", IGogServicesConnectionStateListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IGogServicesConnectionStateListener.SwigDelegateIGogServicesConnectionStateListener_0(IGogServicesConnectionStateListener.SwigDirectorOnConnectionStateChange);
			}
			GalaxyInstancePINVOKE.IGogServicesConnectionStateListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x060009A9 RID: 2473 RVA: 0x00008490 File Offset: 0x00006690
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IGogServicesConnectionStateListener));
		}

		// Token: 0x060009AA RID: 2474 RVA: 0x000084C6 File Offset: 0x000066C6
		[MonoPInvokeCallback(typeof(IGogServicesConnectionStateListener.SwigDelegateIGogServicesConnectionStateListener_0))]
		private static void SwigDirectorOnConnectionStateChange(IntPtr cPtr, int connectionState)
		{
			if (IGogServicesConnectionStateListener.listeners.ContainsKey(cPtr))
			{
				IGogServicesConnectionStateListener.listeners[cPtr].OnConnectionStateChange((GogServicesConnectionState)connectionState);
			}
		}

		// Token: 0x0400017C RID: 380
		private static Dictionary<IntPtr, IGogServicesConnectionStateListener> listeners = new Dictionary<IntPtr, IGogServicesConnectionStateListener>();

		// Token: 0x0400017D RID: 381
		private HandleRef swigCPtr;

		// Token: 0x0400017E RID: 382
		private IGogServicesConnectionStateListener.SwigDelegateIGogServicesConnectionStateListener_0 swigDelegate0;

		// Token: 0x0400017F RID: 383
		private static Type[] swigMethodTypes0 = new Type[] { typeof(GogServicesConnectionState) };

		// Token: 0x020000EE RID: 238
		// (Invoke) Token: 0x060009AD RID: 2477
		public delegate void SwigDelegateIGogServicesConnectionStateListener_0(IntPtr cPtr, int connectionState);
	}
}
