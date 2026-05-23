using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000DF RID: 223
	public abstract class IFriendInvitationSendListener : GalaxyTypeAwareListenerFriendInvitationSend
	{
		// Token: 0x0600091B RID: 2331 RVA: 0x0000F330 File Offset: 0x0000D530
		internal IFriendInvitationSendListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IFriendInvitationSendListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IFriendInvitationSendListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600091C RID: 2332 RVA: 0x0000F358 File Offset: 0x0000D558
		public IFriendInvitationSendListener()
			: this(GalaxyInstancePINVOKE.new_IFriendInvitationSendListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x0600091D RID: 2333 RVA: 0x0000F37C File Offset: 0x0000D57C
		internal static HandleRef getCPtr(IFriendInvitationSendListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600091E RID: 2334 RVA: 0x0000F39C File Offset: 0x0000D59C
		~IFriendInvitationSendListener()
		{
			this.Dispose();
		}

		// Token: 0x0600091F RID: 2335 RVA: 0x0000F3CC File Offset: 0x0000D5CC
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IFriendInvitationSendListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IFriendInvitationSendListener.listeners.ContainsKey(handle))
					{
						IFriendInvitationSendListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000920 RID: 2336
		public abstract void OnFriendInvitationSendSuccess(GalaxyID userID);

		// Token: 0x06000921 RID: 2337
		public abstract void OnFriendInvitationSendFailure(GalaxyID userID, IFriendInvitationSendListener.FailureReason failureReason);

		// Token: 0x06000922 RID: 2338 RVA: 0x0000F47C File Offset: 0x0000D67C
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnFriendInvitationSendSuccess", IFriendInvitationSendListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IFriendInvitationSendListener.SwigDelegateIFriendInvitationSendListener_0(IFriendInvitationSendListener.SwigDirectorOnFriendInvitationSendSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnFriendInvitationSendFailure", IFriendInvitationSendListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IFriendInvitationSendListener.SwigDelegateIFriendInvitationSendListener_1(IFriendInvitationSendListener.SwigDirectorOnFriendInvitationSendFailure);
			}
			GalaxyInstancePINVOKE.IFriendInvitationSendListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000923 RID: 2339 RVA: 0x0000F4F0 File Offset: 0x0000D6F0
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IFriendInvitationSendListener));
		}

		// Token: 0x06000924 RID: 2340 RVA: 0x0000F526 File Offset: 0x0000D726
		[MonoPInvokeCallback(typeof(IFriendInvitationSendListener.SwigDelegateIFriendInvitationSendListener_0))]
		private static void SwigDirectorOnFriendInvitationSendSuccess(IntPtr cPtr, IntPtr userID)
		{
			if (IFriendInvitationSendListener.listeners.ContainsKey(cPtr))
			{
				IFriendInvitationSendListener.listeners[cPtr].OnFriendInvitationSendSuccess(new GalaxyID(new GalaxyID(userID, false).ToUint64()));
			}
		}

		// Token: 0x06000925 RID: 2341 RVA: 0x0000F559 File Offset: 0x0000D759
		[MonoPInvokeCallback(typeof(IFriendInvitationSendListener.SwigDelegateIFriendInvitationSendListener_1))]
		private static void SwigDirectorOnFriendInvitationSendFailure(IntPtr cPtr, IntPtr userID, int failureReason)
		{
			if (IFriendInvitationSendListener.listeners.ContainsKey(cPtr))
			{
				IFriendInvitationSendListener.listeners[cPtr].OnFriendInvitationSendFailure(new GalaxyID(new GalaxyID(userID, false).ToUint64()), (IFriendInvitationSendListener.FailureReason)failureReason);
			}
		}

		// Token: 0x0400015A RID: 346
		private static Dictionary<IntPtr, IFriendInvitationSendListener> listeners = new Dictionary<IntPtr, IFriendInvitationSendListener>();

		// Token: 0x0400015B RID: 347
		private HandleRef swigCPtr;

		// Token: 0x0400015C RID: 348
		private IFriendInvitationSendListener.SwigDelegateIFriendInvitationSendListener_0 swigDelegate0;

		// Token: 0x0400015D RID: 349
		private IFriendInvitationSendListener.SwigDelegateIFriendInvitationSendListener_1 swigDelegate1;

		// Token: 0x0400015E RID: 350
		private static Type[] swigMethodTypes0 = new Type[] { typeof(GalaxyID) };

		// Token: 0x0400015F RID: 351
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(GalaxyID),
			typeof(IFriendInvitationSendListener.FailureReason)
		};

		// Token: 0x020000E0 RID: 224
		// (Invoke) Token: 0x06000928 RID: 2344
		public delegate void SwigDelegateIFriendInvitationSendListener_0(IntPtr cPtr, IntPtr userID);

		// Token: 0x020000E1 RID: 225
		// (Invoke) Token: 0x0600092C RID: 2348
		public delegate void SwigDelegateIFriendInvitationSendListener_1(IntPtr cPtr, IntPtr userID, int failureReason);

		// Token: 0x020000E2 RID: 226
		public enum FailureReason
		{
			// Token: 0x04000161 RID: 353
			FAILURE_REASON_UNDEFINED,
			// Token: 0x04000162 RID: 354
			FAILURE_REASON_USER_DOES_NOT_EXIST,
			// Token: 0x04000163 RID: 355
			FAILURE_REASON_USER_ALREADY_INVITED,
			// Token: 0x04000164 RID: 356
			FAILURE_REASON_USER_ALREADY_FRIEND,
			// Token: 0x04000165 RID: 357
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
