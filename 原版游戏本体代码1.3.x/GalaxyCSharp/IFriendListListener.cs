using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000E3 RID: 227
	public abstract class IFriendListListener : GalaxyTypeAwareListenerFriendList
	{
		// Token: 0x0600092F RID: 2351 RVA: 0x0000F750 File Offset: 0x0000D950
		internal IFriendListListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IFriendListListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IFriendListListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000930 RID: 2352 RVA: 0x0000F778 File Offset: 0x0000D978
		public IFriendListListener()
			: this(GalaxyInstancePINVOKE.new_IFriendListListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000931 RID: 2353 RVA: 0x0000F79C File Offset: 0x0000D99C
		internal static HandleRef getCPtr(IFriendListListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000932 RID: 2354 RVA: 0x0000F7BC File Offset: 0x0000D9BC
		~IFriendListListener()
		{
			this.Dispose();
		}

		// Token: 0x06000933 RID: 2355 RVA: 0x0000F7EC File Offset: 0x0000D9EC
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IFriendListListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IFriendListListener.listeners.ContainsKey(handle))
					{
						IFriendListListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000934 RID: 2356
		public abstract void OnFriendListRetrieveSuccess();

		// Token: 0x06000935 RID: 2357
		public abstract void OnFriendListRetrieveFailure(IFriendListListener.FailureReason failureReason);

		// Token: 0x06000936 RID: 2358 RVA: 0x0000F89C File Offset: 0x0000DA9C
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnFriendListRetrieveSuccess", IFriendListListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IFriendListListener.SwigDelegateIFriendListListener_0(IFriendListListener.SwigDirectorOnFriendListRetrieveSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnFriendListRetrieveFailure", IFriendListListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IFriendListListener.SwigDelegateIFriendListListener_1(IFriendListListener.SwigDirectorOnFriendListRetrieveFailure);
			}
			GalaxyInstancePINVOKE.IFriendListListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000937 RID: 2359 RVA: 0x0000F910 File Offset: 0x0000DB10
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IFriendListListener));
		}

		// Token: 0x06000938 RID: 2360 RVA: 0x0000F946 File Offset: 0x0000DB46
		[MonoPInvokeCallback(typeof(IFriendListListener.SwigDelegateIFriendListListener_0))]
		private static void SwigDirectorOnFriendListRetrieveSuccess(IntPtr cPtr)
		{
			if (IFriendListListener.listeners.ContainsKey(cPtr))
			{
				IFriendListListener.listeners[cPtr].OnFriendListRetrieveSuccess();
			}
		}

		// Token: 0x06000939 RID: 2361 RVA: 0x0000F968 File Offset: 0x0000DB68
		[MonoPInvokeCallback(typeof(IFriendListListener.SwigDelegateIFriendListListener_1))]
		private static void SwigDirectorOnFriendListRetrieveFailure(IntPtr cPtr, int failureReason)
		{
			if (IFriendListListener.listeners.ContainsKey(cPtr))
			{
				IFriendListListener.listeners[cPtr].OnFriendListRetrieveFailure((IFriendListListener.FailureReason)failureReason);
			}
		}

		// Token: 0x04000166 RID: 358
		private static Dictionary<IntPtr, IFriendListListener> listeners = new Dictionary<IntPtr, IFriendListListener>();

		// Token: 0x04000167 RID: 359
		private HandleRef swigCPtr;

		// Token: 0x04000168 RID: 360
		private IFriendListListener.SwigDelegateIFriendListListener_0 swigDelegate0;

		// Token: 0x04000169 RID: 361
		private IFriendListListener.SwigDelegateIFriendListListener_1 swigDelegate1;

		// Token: 0x0400016A RID: 362
		private static Type[] swigMethodTypes0 = new Type[0];

		// Token: 0x0400016B RID: 363
		private static Type[] swigMethodTypes1 = new Type[] { typeof(IFriendListListener.FailureReason) };

		// Token: 0x020000E4 RID: 228
		// (Invoke) Token: 0x0600093C RID: 2364
		public delegate void SwigDelegateIFriendListListener_0(IntPtr cPtr);

		// Token: 0x020000E5 RID: 229
		// (Invoke) Token: 0x06000940 RID: 2368
		public delegate void SwigDelegateIFriendListListener_1(IntPtr cPtr, int failureReason);

		// Token: 0x020000E6 RID: 230
		public enum FailureReason
		{
			// Token: 0x0400016D RID: 365
			FAILURE_REASON_UNDEFINED,
			// Token: 0x0400016E RID: 366
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
