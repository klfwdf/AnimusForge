using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000145 RID: 325
	public abstract class ISentFriendInvitationListRetrieveListener : GalaxyTypeAwareListenerSentFriendInvitationListRetrieve
	{
		// Token: 0x06000BE4 RID: 3044 RVA: 0x00014608 File Offset: 0x00012808
		internal ISentFriendInvitationListRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ISentFriendInvitationListRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ISentFriendInvitationListRetrieveListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000BE5 RID: 3045 RVA: 0x00014630 File Offset: 0x00012830
		public ISentFriendInvitationListRetrieveListener()
			: this(GalaxyInstancePINVOKE.new_ISentFriendInvitationListRetrieveListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000BE6 RID: 3046 RVA: 0x00014654 File Offset: 0x00012854
		internal static HandleRef getCPtr(ISentFriendInvitationListRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000BE7 RID: 3047 RVA: 0x00014674 File Offset: 0x00012874
		~ISentFriendInvitationListRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x06000BE8 RID: 3048 RVA: 0x000146A4 File Offset: 0x000128A4
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ISentFriendInvitationListRetrieveListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ISentFriendInvitationListRetrieveListener.listeners.ContainsKey(handle))
					{
						ISentFriendInvitationListRetrieveListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000BE9 RID: 3049
		public abstract void OnSentFriendInvitationListRetrieveSuccess();

		// Token: 0x06000BEA RID: 3050
		public abstract void OnSentFriendInvitationListRetrieveFailure(ISentFriendInvitationListRetrieveListener.FailureReason failureReason);

		// Token: 0x06000BEB RID: 3051 RVA: 0x00014754 File Offset: 0x00012954
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnSentFriendInvitationListRetrieveSuccess", ISentFriendInvitationListRetrieveListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ISentFriendInvitationListRetrieveListener.SwigDelegateISentFriendInvitationListRetrieveListener_0(ISentFriendInvitationListRetrieveListener.SwigDirectorOnSentFriendInvitationListRetrieveSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnSentFriendInvitationListRetrieveFailure", ISentFriendInvitationListRetrieveListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new ISentFriendInvitationListRetrieveListener.SwigDelegateISentFriendInvitationListRetrieveListener_1(ISentFriendInvitationListRetrieveListener.SwigDirectorOnSentFriendInvitationListRetrieveFailure);
			}
			GalaxyInstancePINVOKE.ISentFriendInvitationListRetrieveListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000BEC RID: 3052 RVA: 0x000147C8 File Offset: 0x000129C8
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ISentFriendInvitationListRetrieveListener));
		}

		// Token: 0x06000BED RID: 3053 RVA: 0x000147FE File Offset: 0x000129FE
		[MonoPInvokeCallback(typeof(ISentFriendInvitationListRetrieveListener.SwigDelegateISentFriendInvitationListRetrieveListener_0))]
		private static void SwigDirectorOnSentFriendInvitationListRetrieveSuccess(IntPtr cPtr)
		{
			if (ISentFriendInvitationListRetrieveListener.listeners.ContainsKey(cPtr))
			{
				ISentFriendInvitationListRetrieveListener.listeners[cPtr].OnSentFriendInvitationListRetrieveSuccess();
			}
		}

		// Token: 0x06000BEE RID: 3054 RVA: 0x00014820 File Offset: 0x00012A20
		[MonoPInvokeCallback(typeof(ISentFriendInvitationListRetrieveListener.SwigDelegateISentFriendInvitationListRetrieveListener_1))]
		private static void SwigDirectorOnSentFriendInvitationListRetrieveFailure(IntPtr cPtr, int failureReason)
		{
			if (ISentFriendInvitationListRetrieveListener.listeners.ContainsKey(cPtr))
			{
				ISentFriendInvitationListRetrieveListener.listeners[cPtr].OnSentFriendInvitationListRetrieveFailure((ISentFriendInvitationListRetrieveListener.FailureReason)failureReason);
			}
		}

		// Token: 0x04000247 RID: 583
		private static Dictionary<IntPtr, ISentFriendInvitationListRetrieveListener> listeners = new Dictionary<IntPtr, ISentFriendInvitationListRetrieveListener>();

		// Token: 0x04000248 RID: 584
		private HandleRef swigCPtr;

		// Token: 0x04000249 RID: 585
		private ISentFriendInvitationListRetrieveListener.SwigDelegateISentFriendInvitationListRetrieveListener_0 swigDelegate0;

		// Token: 0x0400024A RID: 586
		private ISentFriendInvitationListRetrieveListener.SwigDelegateISentFriendInvitationListRetrieveListener_1 swigDelegate1;

		// Token: 0x0400024B RID: 587
		private static Type[] swigMethodTypes0 = new Type[0];

		// Token: 0x0400024C RID: 588
		private static Type[] swigMethodTypes1 = new Type[] { typeof(ISentFriendInvitationListRetrieveListener.FailureReason) };

		// Token: 0x02000146 RID: 326
		// (Invoke) Token: 0x06000BF1 RID: 3057
		public delegate void SwigDelegateISentFriendInvitationListRetrieveListener_0(IntPtr cPtr);

		// Token: 0x02000147 RID: 327
		// (Invoke) Token: 0x06000BF5 RID: 3061
		public delegate void SwigDelegateISentFriendInvitationListRetrieveListener_1(IntPtr cPtr, int failureReason);

		// Token: 0x02000148 RID: 328
		public enum FailureReason
		{
			// Token: 0x0400024E RID: 590
			FAILURE_REASON_UNDEFINED,
			// Token: 0x0400024F RID: 591
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
