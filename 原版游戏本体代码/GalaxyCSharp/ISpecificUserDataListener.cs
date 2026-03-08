using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200014D RID: 333
	public abstract class ISpecificUserDataListener : GalaxyTypeAwareListenerSpecificUserData
	{
		// Token: 0x06000C0C RID: 3084 RVA: 0x0000B13C File Offset: 0x0000933C
		internal ISpecificUserDataListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ISpecificUserDataListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ISpecificUserDataListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000C0D RID: 3085 RVA: 0x0000B164 File Offset: 0x00009364
		public ISpecificUserDataListener()
			: this(GalaxyInstancePINVOKE.new_ISpecificUserDataListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000C0E RID: 3086 RVA: 0x0000B188 File Offset: 0x00009388
		internal static HandleRef getCPtr(ISpecificUserDataListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000C0F RID: 3087 RVA: 0x0000B1A8 File Offset: 0x000093A8
		~ISpecificUserDataListener()
		{
			this.Dispose();
		}

		// Token: 0x06000C10 RID: 3088 RVA: 0x0000B1D8 File Offset: 0x000093D8
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ISpecificUserDataListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ISpecificUserDataListener.listeners.ContainsKey(handle))
					{
						ISpecificUserDataListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000C11 RID: 3089
		public abstract void OnSpecificUserDataUpdated(GalaxyID userID);

		// Token: 0x06000C12 RID: 3090 RVA: 0x0000B288 File Offset: 0x00009488
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnSpecificUserDataUpdated", ISpecificUserDataListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ISpecificUserDataListener.SwigDelegateISpecificUserDataListener_0(ISpecificUserDataListener.SwigDirectorOnSpecificUserDataUpdated);
			}
			GalaxyInstancePINVOKE.ISpecificUserDataListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000C13 RID: 3091 RVA: 0x0000B2C4 File Offset: 0x000094C4
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ISpecificUserDataListener));
		}

		// Token: 0x06000C14 RID: 3092 RVA: 0x0000B2FA File Offset: 0x000094FA
		[MonoPInvokeCallback(typeof(ISpecificUserDataListener.SwigDelegateISpecificUserDataListener_0))]
		private static void SwigDirectorOnSpecificUserDataUpdated(IntPtr cPtr, IntPtr userID)
		{
			if (ISpecificUserDataListener.listeners.ContainsKey(cPtr))
			{
				ISpecificUserDataListener.listeners[cPtr].OnSpecificUserDataUpdated(new GalaxyID(new GalaxyID(userID, false).ToUint64()));
			}
		}

		// Token: 0x04000259 RID: 601
		private static Dictionary<IntPtr, ISpecificUserDataListener> listeners = new Dictionary<IntPtr, ISpecificUserDataListener>();

		// Token: 0x0400025A RID: 602
		private HandleRef swigCPtr;

		// Token: 0x0400025B RID: 603
		private ISpecificUserDataListener.SwigDelegateISpecificUserDataListener_0 swigDelegate0;

		// Token: 0x0400025C RID: 604
		private static Type[] swigMethodTypes0 = new Type[] { typeof(GalaxyID) };

		// Token: 0x0200014E RID: 334
		// (Invoke) Token: 0x06000C17 RID: 3095
		public delegate void SwigDelegateISpecificUserDataListener_0(IntPtr cPtr, IntPtr userID);
	}
}
