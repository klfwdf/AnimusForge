using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200015C RID: 348
	public abstract class IUserDataListener : GalaxyTypeAwareListenerUserData
	{
		// Token: 0x06000CEB RID: 3307 RVA: 0x000153A0 File Offset: 0x000135A0
		internal IUserDataListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IUserDataListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IUserDataListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000CEC RID: 3308 RVA: 0x000153C8 File Offset: 0x000135C8
		public IUserDataListener()
			: this(GalaxyInstancePINVOKE.new_IUserDataListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000CED RID: 3309 RVA: 0x000153EC File Offset: 0x000135EC
		internal static HandleRef getCPtr(IUserDataListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000CEE RID: 3310 RVA: 0x0001540C File Offset: 0x0001360C
		~IUserDataListener()
		{
			this.Dispose();
		}

		// Token: 0x06000CEF RID: 3311 RVA: 0x0001543C File Offset: 0x0001363C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IUserDataListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IUserDataListener.listeners.ContainsKey(handle))
					{
						IUserDataListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000CF0 RID: 3312
		public abstract void OnUserDataUpdated();

		// Token: 0x06000CF1 RID: 3313 RVA: 0x000154EC File Offset: 0x000136EC
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnUserDataUpdated", IUserDataListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IUserDataListener.SwigDelegateIUserDataListener_0(IUserDataListener.SwigDirectorOnUserDataUpdated);
			}
			GalaxyInstancePINVOKE.IUserDataListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000CF2 RID: 3314 RVA: 0x00015528 File Offset: 0x00013728
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IUserDataListener));
		}

		// Token: 0x06000CF3 RID: 3315 RVA: 0x0001555E File Offset: 0x0001375E
		[MonoPInvokeCallback(typeof(IUserDataListener.SwigDelegateIUserDataListener_0))]
		private static void SwigDirectorOnUserDataUpdated(IntPtr cPtr)
		{
			if (IUserDataListener.listeners.ContainsKey(cPtr))
			{
				IUserDataListener.listeners[cPtr].OnUserDataUpdated();
			}
		}

		// Token: 0x0400027F RID: 639
		private static Dictionary<IntPtr, IUserDataListener> listeners = new Dictionary<IntPtr, IUserDataListener>();

		// Token: 0x04000280 RID: 640
		private HandleRef swigCPtr;

		// Token: 0x04000281 RID: 641
		private IUserDataListener.SwigDelegateIUserDataListener_0 swigDelegate0;

		// Token: 0x04000282 RID: 642
		private static Type[] swigMethodTypes0 = new Type[0];

		// Token: 0x0200015D RID: 349
		// (Invoke) Token: 0x06000CF6 RID: 3318
		public delegate void SwigDelegateIUserDataListener_0(IntPtr cPtr);
	}
}
