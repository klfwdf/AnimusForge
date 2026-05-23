using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000D1 RID: 209
	public abstract class IFriendDeleteListener : GalaxyTypeAwareListenerFriendDelete
	{
		// Token: 0x060008D1 RID: 2257 RVA: 0x0000E3B8 File Offset: 0x0000C5B8
		internal IFriendDeleteListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IFriendDeleteListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IFriendDeleteListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060008D2 RID: 2258 RVA: 0x0000E3E0 File Offset: 0x0000C5E0
		public IFriendDeleteListener()
			: this(GalaxyInstancePINVOKE.new_IFriendDeleteListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x060008D3 RID: 2259 RVA: 0x0000E404 File Offset: 0x0000C604
		internal static HandleRef getCPtr(IFriendDeleteListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060008D4 RID: 2260 RVA: 0x0000E424 File Offset: 0x0000C624
		~IFriendDeleteListener()
		{
			this.Dispose();
		}

		// Token: 0x060008D5 RID: 2261 RVA: 0x0000E454 File Offset: 0x0000C654
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IFriendDeleteListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IFriendDeleteListener.listeners.ContainsKey(handle))
					{
						IFriendDeleteListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060008D6 RID: 2262
		public abstract void OnFriendDeleteSuccess(GalaxyID userID);

		// Token: 0x060008D7 RID: 2263
		public abstract void OnFriendDeleteFailure(GalaxyID userID, IFriendDeleteListener.FailureReason failureReason);

		// Token: 0x060008D8 RID: 2264 RVA: 0x0000E504 File Offset: 0x0000C704
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnFriendDeleteSuccess", IFriendDeleteListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IFriendDeleteListener.SwigDelegateIFriendDeleteListener_0(IFriendDeleteListener.SwigDirectorOnFriendDeleteSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnFriendDeleteFailure", IFriendDeleteListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IFriendDeleteListener.SwigDelegateIFriendDeleteListener_1(IFriendDeleteListener.SwigDirectorOnFriendDeleteFailure);
			}
			GalaxyInstancePINVOKE.IFriendDeleteListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x060008D9 RID: 2265 RVA: 0x0000E578 File Offset: 0x0000C778
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IFriendDeleteListener));
		}

		// Token: 0x060008DA RID: 2266 RVA: 0x0000E5AE File Offset: 0x0000C7AE
		[MonoPInvokeCallback(typeof(IFriendDeleteListener.SwigDelegateIFriendDeleteListener_0))]
		private static void SwigDirectorOnFriendDeleteSuccess(IntPtr cPtr, IntPtr userID)
		{
			if (IFriendDeleteListener.listeners.ContainsKey(cPtr))
			{
				IFriendDeleteListener.listeners[cPtr].OnFriendDeleteSuccess(new GalaxyID(new GalaxyID(userID, false).ToUint64()));
			}
		}

		// Token: 0x060008DB RID: 2267 RVA: 0x0000E5E1 File Offset: 0x0000C7E1
		[MonoPInvokeCallback(typeof(IFriendDeleteListener.SwigDelegateIFriendDeleteListener_1))]
		private static void SwigDirectorOnFriendDeleteFailure(IntPtr cPtr, IntPtr userID, int failureReason)
		{
			if (IFriendDeleteListener.listeners.ContainsKey(cPtr))
			{
				IFriendDeleteListener.listeners[cPtr].OnFriendDeleteFailure(new GalaxyID(new GalaxyID(userID, false).ToUint64()), (IFriendDeleteListener.FailureReason)failureReason);
			}
		}

		// Token: 0x04000138 RID: 312
		private static Dictionary<IntPtr, IFriendDeleteListener> listeners = new Dictionary<IntPtr, IFriendDeleteListener>();

		// Token: 0x04000139 RID: 313
		private HandleRef swigCPtr;

		// Token: 0x0400013A RID: 314
		private IFriendDeleteListener.SwigDelegateIFriendDeleteListener_0 swigDelegate0;

		// Token: 0x0400013B RID: 315
		private IFriendDeleteListener.SwigDelegateIFriendDeleteListener_1 swigDelegate1;

		// Token: 0x0400013C RID: 316
		private static Type[] swigMethodTypes0 = new Type[] { typeof(GalaxyID) };

		// Token: 0x0400013D RID: 317
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(GalaxyID),
			typeof(IFriendDeleteListener.FailureReason)
		};

		// Token: 0x020000D2 RID: 210
		// (Invoke) Token: 0x060008DE RID: 2270
		public delegate void SwigDelegateIFriendDeleteListener_0(IntPtr cPtr, IntPtr userID);

		// Token: 0x020000D3 RID: 211
		// (Invoke) Token: 0x060008E2 RID: 2274
		public delegate void SwigDelegateIFriendDeleteListener_1(IntPtr cPtr, IntPtr userID, int failureReason);

		// Token: 0x020000D4 RID: 212
		public enum FailureReason
		{
			// Token: 0x0400013F RID: 319
			FAILURE_REASON_UNDEFINED,
			// Token: 0x04000140 RID: 320
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
