using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000B6 RID: 182
	public abstract class IChatRoomWithUserRetrieveListener : GalaxyTypeAwareListenerChatRoomWithUserRetrieve
	{
		// Token: 0x06000842 RID: 2114 RVA: 0x0000CD2C File Offset: 0x0000AF2C
		internal IChatRoomWithUserRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IChatRoomWithUserRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IChatRoomWithUserRetrieveListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000843 RID: 2115 RVA: 0x0000CD54 File Offset: 0x0000AF54
		public IChatRoomWithUserRetrieveListener()
			: this(GalaxyInstancePINVOKE.new_IChatRoomWithUserRetrieveListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000844 RID: 2116 RVA: 0x0000CD78 File Offset: 0x0000AF78
		internal static HandleRef getCPtr(IChatRoomWithUserRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000845 RID: 2117 RVA: 0x0000CD98 File Offset: 0x0000AF98
		~IChatRoomWithUserRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x06000846 RID: 2118 RVA: 0x0000CDC8 File Offset: 0x0000AFC8
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IChatRoomWithUserRetrieveListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IChatRoomWithUserRetrieveListener.listeners.ContainsKey(handle))
					{
						IChatRoomWithUserRetrieveListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000847 RID: 2119
		public abstract void OnChatRoomWithUserRetrieveSuccess(GalaxyID userID, ulong chatRoomID);

		// Token: 0x06000848 RID: 2120
		public abstract void OnChatRoomWithUserRetrieveFailure(GalaxyID userID, IChatRoomWithUserRetrieveListener.FailureReason failureReason);

		// Token: 0x06000849 RID: 2121 RVA: 0x0000CE78 File Offset: 0x0000B078
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnChatRoomWithUserRetrieveSuccess", IChatRoomWithUserRetrieveListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IChatRoomWithUserRetrieveListener.SwigDelegateIChatRoomWithUserRetrieveListener_0(IChatRoomWithUserRetrieveListener.SwigDirectorOnChatRoomWithUserRetrieveSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnChatRoomWithUserRetrieveFailure", IChatRoomWithUserRetrieveListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IChatRoomWithUserRetrieveListener.SwigDelegateIChatRoomWithUserRetrieveListener_1(IChatRoomWithUserRetrieveListener.SwigDirectorOnChatRoomWithUserRetrieveFailure);
			}
			GalaxyInstancePINVOKE.IChatRoomWithUserRetrieveListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x0600084A RID: 2122 RVA: 0x0000CEEC File Offset: 0x0000B0EC
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IChatRoomWithUserRetrieveListener));
		}

		// Token: 0x0600084B RID: 2123 RVA: 0x0000CF22 File Offset: 0x0000B122
		[MonoPInvokeCallback(typeof(IChatRoomWithUserRetrieveListener.SwigDelegateIChatRoomWithUserRetrieveListener_0))]
		private static void SwigDirectorOnChatRoomWithUserRetrieveSuccess(IntPtr cPtr, IntPtr userID, ulong chatRoomID)
		{
			if (IChatRoomWithUserRetrieveListener.listeners.ContainsKey(cPtr))
			{
				IChatRoomWithUserRetrieveListener.listeners[cPtr].OnChatRoomWithUserRetrieveSuccess(new GalaxyID(new GalaxyID(userID, false).ToUint64()), chatRoomID);
			}
		}

		// Token: 0x0600084C RID: 2124 RVA: 0x0000CF56 File Offset: 0x0000B156
		[MonoPInvokeCallback(typeof(IChatRoomWithUserRetrieveListener.SwigDelegateIChatRoomWithUserRetrieveListener_1))]
		private static void SwigDirectorOnChatRoomWithUserRetrieveFailure(IntPtr cPtr, IntPtr userID, int failureReason)
		{
			if (IChatRoomWithUserRetrieveListener.listeners.ContainsKey(cPtr))
			{
				IChatRoomWithUserRetrieveListener.listeners[cPtr].OnChatRoomWithUserRetrieveFailure(new GalaxyID(new GalaxyID(userID, false).ToUint64()), (IChatRoomWithUserRetrieveListener.FailureReason)failureReason);
			}
		}

		// Token: 0x040000F8 RID: 248
		private static Dictionary<IntPtr, IChatRoomWithUserRetrieveListener> listeners = new Dictionary<IntPtr, IChatRoomWithUserRetrieveListener>();

		// Token: 0x040000F9 RID: 249
		private HandleRef swigCPtr;

		// Token: 0x040000FA RID: 250
		private IChatRoomWithUserRetrieveListener.SwigDelegateIChatRoomWithUserRetrieveListener_0 swigDelegate0;

		// Token: 0x040000FB RID: 251
		private IChatRoomWithUserRetrieveListener.SwigDelegateIChatRoomWithUserRetrieveListener_1 swigDelegate1;

		// Token: 0x040000FC RID: 252
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(GalaxyID),
			typeof(ulong)
		};

		// Token: 0x040000FD RID: 253
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(GalaxyID),
			typeof(IChatRoomWithUserRetrieveListener.FailureReason)
		};

		// Token: 0x020000B7 RID: 183
		// (Invoke) Token: 0x0600084F RID: 2127
		public delegate void SwigDelegateIChatRoomWithUserRetrieveListener_0(IntPtr cPtr, IntPtr userID, ulong chatRoomID);

		// Token: 0x020000B8 RID: 184
		// (Invoke) Token: 0x06000853 RID: 2131
		public delegate void SwigDelegateIChatRoomWithUserRetrieveListener_1(IntPtr cPtr, IntPtr userID, int failureReason);

		// Token: 0x020000B9 RID: 185
		public enum FailureReason
		{
			// Token: 0x040000FF RID: 255
			FAILURE_REASON_UNDEFINED,
			// Token: 0x04000100 RID: 256
			FAILURE_REASON_FORBIDDEN,
			// Token: 0x04000101 RID: 257
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
