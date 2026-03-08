using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000D7 RID: 215
	public abstract class IFriendInvitationListRetrieveListener : GalaxyTypeAwareListenerFriendInvitationListRetrieve
	{
		// Token: 0x060008F3 RID: 2291 RVA: 0x0000EB28 File Offset: 0x0000CD28
		internal IFriendInvitationListRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IFriendInvitationListRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IFriendInvitationListRetrieveListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060008F4 RID: 2292 RVA: 0x0000EB50 File Offset: 0x0000CD50
		public IFriendInvitationListRetrieveListener()
			: this(GalaxyInstancePINVOKE.new_IFriendInvitationListRetrieveListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x060008F5 RID: 2293 RVA: 0x0000EB74 File Offset: 0x0000CD74
		internal static HandleRef getCPtr(IFriendInvitationListRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060008F6 RID: 2294 RVA: 0x0000EB94 File Offset: 0x0000CD94
		~IFriendInvitationListRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x060008F7 RID: 2295 RVA: 0x0000EBC4 File Offset: 0x0000CDC4
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IFriendInvitationListRetrieveListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IFriendInvitationListRetrieveListener.listeners.ContainsKey(handle))
					{
						IFriendInvitationListRetrieveListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060008F8 RID: 2296
		public abstract void OnFriendInvitationListRetrieveSuccess();

		// Token: 0x060008F9 RID: 2297
		public abstract void OnFriendInvitationListRetrieveFailure(IFriendInvitationListRetrieveListener.FailureReason failureReason);

		// Token: 0x060008FA RID: 2298 RVA: 0x0000EC74 File Offset: 0x0000CE74
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnFriendInvitationListRetrieveSuccess", IFriendInvitationListRetrieveListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IFriendInvitationListRetrieveListener.SwigDelegateIFriendInvitationListRetrieveListener_0(IFriendInvitationListRetrieveListener.SwigDirectorOnFriendInvitationListRetrieveSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnFriendInvitationListRetrieveFailure", IFriendInvitationListRetrieveListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IFriendInvitationListRetrieveListener.SwigDelegateIFriendInvitationListRetrieveListener_1(IFriendInvitationListRetrieveListener.SwigDirectorOnFriendInvitationListRetrieveFailure);
			}
			GalaxyInstancePINVOKE.IFriendInvitationListRetrieveListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x060008FB RID: 2299 RVA: 0x0000ECE8 File Offset: 0x0000CEE8
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IFriendInvitationListRetrieveListener));
		}

		// Token: 0x060008FC RID: 2300 RVA: 0x0000ED1E File Offset: 0x0000CF1E
		[MonoPInvokeCallback(typeof(IFriendInvitationListRetrieveListener.SwigDelegateIFriendInvitationListRetrieveListener_0))]
		private static void SwigDirectorOnFriendInvitationListRetrieveSuccess(IntPtr cPtr)
		{
			if (IFriendInvitationListRetrieveListener.listeners.ContainsKey(cPtr))
			{
				IFriendInvitationListRetrieveListener.listeners[cPtr].OnFriendInvitationListRetrieveSuccess();
			}
		}

		// Token: 0x060008FD RID: 2301 RVA: 0x0000ED40 File Offset: 0x0000CF40
		[MonoPInvokeCallback(typeof(IFriendInvitationListRetrieveListener.SwigDelegateIFriendInvitationListRetrieveListener_1))]
		private static void SwigDirectorOnFriendInvitationListRetrieveFailure(IntPtr cPtr, int failureReason)
		{
			if (IFriendInvitationListRetrieveListener.listeners.ContainsKey(cPtr))
			{
				IFriendInvitationListRetrieveListener.listeners[cPtr].OnFriendInvitationListRetrieveFailure((IFriendInvitationListRetrieveListener.FailureReason)failureReason);
			}
		}

		// Token: 0x04000145 RID: 325
		private static Dictionary<IntPtr, IFriendInvitationListRetrieveListener> listeners = new Dictionary<IntPtr, IFriendInvitationListRetrieveListener>();

		// Token: 0x04000146 RID: 326
		private HandleRef swigCPtr;

		// Token: 0x04000147 RID: 327
		private IFriendInvitationListRetrieveListener.SwigDelegateIFriendInvitationListRetrieveListener_0 swigDelegate0;

		// Token: 0x04000148 RID: 328
		private IFriendInvitationListRetrieveListener.SwigDelegateIFriendInvitationListRetrieveListener_1 swigDelegate1;

		// Token: 0x04000149 RID: 329
		private static Type[] swigMethodTypes0 = new Type[0];

		// Token: 0x0400014A RID: 330
		private static Type[] swigMethodTypes1 = new Type[] { typeof(IFriendInvitationListRetrieveListener.FailureReason) };

		// Token: 0x020000D8 RID: 216
		// (Invoke) Token: 0x06000900 RID: 2304
		public delegate void SwigDelegateIFriendInvitationListRetrieveListener_0(IntPtr cPtr);

		// Token: 0x020000D9 RID: 217
		// (Invoke) Token: 0x06000904 RID: 2308
		public delegate void SwigDelegateIFriendInvitationListRetrieveListener_1(IntPtr cPtr, int failureReason);

		// Token: 0x020000DA RID: 218
		public enum FailureReason
		{
			// Token: 0x0400014C RID: 332
			FAILURE_REASON_UNDEFINED,
			// Token: 0x0400014D RID: 333
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
