using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000DB RID: 219
	public abstract class IFriendInvitationRespondToListener : GalaxyTypeAwareListenerFriendInvitationRespondTo
	{
		// Token: 0x06000907 RID: 2311 RVA: 0x0000EF00 File Offset: 0x0000D100
		internal IFriendInvitationRespondToListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IFriendInvitationRespondToListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IFriendInvitationRespondToListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000908 RID: 2312 RVA: 0x0000EF28 File Offset: 0x0000D128
		public IFriendInvitationRespondToListener()
			: this(GalaxyInstancePINVOKE.new_IFriendInvitationRespondToListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000909 RID: 2313 RVA: 0x0000EF4C File Offset: 0x0000D14C
		internal static HandleRef getCPtr(IFriendInvitationRespondToListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600090A RID: 2314 RVA: 0x0000EF6C File Offset: 0x0000D16C
		~IFriendInvitationRespondToListener()
		{
			this.Dispose();
		}

		// Token: 0x0600090B RID: 2315 RVA: 0x0000EF9C File Offset: 0x0000D19C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IFriendInvitationRespondToListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IFriendInvitationRespondToListener.listeners.ContainsKey(handle))
					{
						IFriendInvitationRespondToListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0600090C RID: 2316
		public abstract void OnFriendInvitationRespondToSuccess(GalaxyID userID, bool accept);

		// Token: 0x0600090D RID: 2317
		public abstract void OnFriendInvitationRespondToFailure(GalaxyID userID, IFriendInvitationRespondToListener.FailureReason failureReason);

		// Token: 0x0600090E RID: 2318 RVA: 0x0000F04C File Offset: 0x0000D24C
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnFriendInvitationRespondToSuccess", IFriendInvitationRespondToListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IFriendInvitationRespondToListener.SwigDelegateIFriendInvitationRespondToListener_0(IFriendInvitationRespondToListener.SwigDirectorOnFriendInvitationRespondToSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnFriendInvitationRespondToFailure", IFriendInvitationRespondToListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IFriendInvitationRespondToListener.SwigDelegateIFriendInvitationRespondToListener_1(IFriendInvitationRespondToListener.SwigDirectorOnFriendInvitationRespondToFailure);
			}
			GalaxyInstancePINVOKE.IFriendInvitationRespondToListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x0600090F RID: 2319 RVA: 0x0000F0C0 File Offset: 0x0000D2C0
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IFriendInvitationRespondToListener));
		}

		// Token: 0x06000910 RID: 2320 RVA: 0x0000F0F6 File Offset: 0x0000D2F6
		[MonoPInvokeCallback(typeof(IFriendInvitationRespondToListener.SwigDelegateIFriendInvitationRespondToListener_0))]
		private static void SwigDirectorOnFriendInvitationRespondToSuccess(IntPtr cPtr, IntPtr userID, bool accept)
		{
			if (IFriendInvitationRespondToListener.listeners.ContainsKey(cPtr))
			{
				IFriendInvitationRespondToListener.listeners[cPtr].OnFriendInvitationRespondToSuccess(new GalaxyID(new GalaxyID(userID, false).ToUint64()), accept);
			}
		}

		// Token: 0x06000911 RID: 2321 RVA: 0x0000F12A File Offset: 0x0000D32A
		[MonoPInvokeCallback(typeof(IFriendInvitationRespondToListener.SwigDelegateIFriendInvitationRespondToListener_1))]
		private static void SwigDirectorOnFriendInvitationRespondToFailure(IntPtr cPtr, IntPtr userID, int failureReason)
		{
			if (IFriendInvitationRespondToListener.listeners.ContainsKey(cPtr))
			{
				IFriendInvitationRespondToListener.listeners[cPtr].OnFriendInvitationRespondToFailure(new GalaxyID(new GalaxyID(userID, false).ToUint64()), (IFriendInvitationRespondToListener.FailureReason)failureReason);
			}
		}

		// Token: 0x0400014E RID: 334
		private static Dictionary<IntPtr, IFriendInvitationRespondToListener> listeners = new Dictionary<IntPtr, IFriendInvitationRespondToListener>();

		// Token: 0x0400014F RID: 335
		private HandleRef swigCPtr;

		// Token: 0x04000150 RID: 336
		private IFriendInvitationRespondToListener.SwigDelegateIFriendInvitationRespondToListener_0 swigDelegate0;

		// Token: 0x04000151 RID: 337
		private IFriendInvitationRespondToListener.SwigDelegateIFriendInvitationRespondToListener_1 swigDelegate1;

		// Token: 0x04000152 RID: 338
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(GalaxyID),
			typeof(bool)
		};

		// Token: 0x04000153 RID: 339
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(GalaxyID),
			typeof(IFriendInvitationRespondToListener.FailureReason)
		};

		// Token: 0x020000DC RID: 220
		// (Invoke) Token: 0x06000914 RID: 2324
		public delegate void SwigDelegateIFriendInvitationRespondToListener_0(IntPtr cPtr, IntPtr userID, bool accept);

		// Token: 0x020000DD RID: 221
		// (Invoke) Token: 0x06000918 RID: 2328
		public delegate void SwigDelegateIFriendInvitationRespondToListener_1(IntPtr cPtr, IntPtr userID, int failureReason);

		// Token: 0x020000DE RID: 222
		public enum FailureReason
		{
			// Token: 0x04000155 RID: 341
			FAILURE_REASON_UNDEFINED,
			// Token: 0x04000156 RID: 342
			FAILURE_REASON_USER_DOES_NOT_EXIST,
			// Token: 0x04000157 RID: 343
			FAILURE_REASON_FRIEND_INVITATION_DOES_NOT_EXIST,
			// Token: 0x04000158 RID: 344
			FAILURE_REASON_USER_ALREADY_FRIEND,
			// Token: 0x04000159 RID: 345
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
