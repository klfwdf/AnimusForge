using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000AC RID: 172
	public abstract class IChatRoomMessageSendListener : GalaxyTypeAwareListenerChatRoomMessageSend
	{
		// Token: 0x0600080C RID: 2060 RVA: 0x0000C208 File Offset: 0x0000A408
		internal IChatRoomMessageSendListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IChatRoomMessageSendListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IChatRoomMessageSendListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600080D RID: 2061 RVA: 0x0000C230 File Offset: 0x0000A430
		public IChatRoomMessageSendListener()
			: this(GalaxyInstancePINVOKE.new_IChatRoomMessageSendListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x0600080E RID: 2062 RVA: 0x0000C254 File Offset: 0x0000A454
		internal static HandleRef getCPtr(IChatRoomMessageSendListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600080F RID: 2063 RVA: 0x0000C274 File Offset: 0x0000A474
		~IChatRoomMessageSendListener()
		{
			this.Dispose();
		}

		// Token: 0x06000810 RID: 2064 RVA: 0x0000C2A4 File Offset: 0x0000A4A4
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IChatRoomMessageSendListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IChatRoomMessageSendListener.listeners.ContainsKey(handle))
					{
						IChatRoomMessageSendListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000811 RID: 2065
		public abstract void OnChatRoomMessageSendSuccess(ulong chatRoomID, uint sentMessageIndex, ulong messageID, uint sendTime);

		// Token: 0x06000812 RID: 2066
		public abstract void OnChatRoomMessageSendFailure(ulong chatRoomID, uint sentMessageIndex, IChatRoomMessageSendListener.FailureReason failureReason);

		// Token: 0x06000813 RID: 2067 RVA: 0x0000C354 File Offset: 0x0000A554
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnChatRoomMessageSendSuccess", IChatRoomMessageSendListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IChatRoomMessageSendListener.SwigDelegateIChatRoomMessageSendListener_0(IChatRoomMessageSendListener.SwigDirectorOnChatRoomMessageSendSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnChatRoomMessageSendFailure", IChatRoomMessageSendListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IChatRoomMessageSendListener.SwigDelegateIChatRoomMessageSendListener_1(IChatRoomMessageSendListener.SwigDirectorOnChatRoomMessageSendFailure);
			}
			GalaxyInstancePINVOKE.IChatRoomMessageSendListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000814 RID: 2068 RVA: 0x0000C3C8 File Offset: 0x0000A5C8
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IChatRoomMessageSendListener));
		}

		// Token: 0x06000815 RID: 2069 RVA: 0x0000C3FE File Offset: 0x0000A5FE
		[MonoPInvokeCallback(typeof(IChatRoomMessageSendListener.SwigDelegateIChatRoomMessageSendListener_0))]
		private static void SwigDirectorOnChatRoomMessageSendSuccess(IntPtr cPtr, ulong chatRoomID, uint sentMessageIndex, ulong messageID, uint sendTime)
		{
			if (IChatRoomMessageSendListener.listeners.ContainsKey(cPtr))
			{
				IChatRoomMessageSendListener.listeners[cPtr].OnChatRoomMessageSendSuccess(chatRoomID, sentMessageIndex, messageID, sendTime);
			}
		}

		// Token: 0x06000816 RID: 2070 RVA: 0x0000C425 File Offset: 0x0000A625
		[MonoPInvokeCallback(typeof(IChatRoomMessageSendListener.SwigDelegateIChatRoomMessageSendListener_1))]
		private static void SwigDirectorOnChatRoomMessageSendFailure(IntPtr cPtr, ulong chatRoomID, uint sentMessageIndex, int failureReason)
		{
			if (IChatRoomMessageSendListener.listeners.ContainsKey(cPtr))
			{
				IChatRoomMessageSendListener.listeners[cPtr].OnChatRoomMessageSendFailure(chatRoomID, sentMessageIndex, (IChatRoomMessageSendListener.FailureReason)failureReason);
			}
		}

		// Token: 0x040000E0 RID: 224
		private static Dictionary<IntPtr, IChatRoomMessageSendListener> listeners = new Dictionary<IntPtr, IChatRoomMessageSendListener>();

		// Token: 0x040000E1 RID: 225
		private HandleRef swigCPtr;

		// Token: 0x040000E2 RID: 226
		private IChatRoomMessageSendListener.SwigDelegateIChatRoomMessageSendListener_0 swigDelegate0;

		// Token: 0x040000E3 RID: 227
		private IChatRoomMessageSendListener.SwigDelegateIChatRoomMessageSendListener_1 swigDelegate1;

		// Token: 0x040000E4 RID: 228
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(ulong),
			typeof(uint),
			typeof(ulong),
			typeof(uint)
		};

		// Token: 0x040000E5 RID: 229
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(ulong),
			typeof(uint),
			typeof(IChatRoomMessageSendListener.FailureReason)
		};

		// Token: 0x020000AD RID: 173
		// (Invoke) Token: 0x06000819 RID: 2073
		public delegate void SwigDelegateIChatRoomMessageSendListener_0(IntPtr cPtr, ulong chatRoomID, uint sentMessageIndex, ulong messageID, uint sendTime);

		// Token: 0x020000AE RID: 174
		// (Invoke) Token: 0x0600081D RID: 2077
		public delegate void SwigDelegateIChatRoomMessageSendListener_1(IntPtr cPtr, ulong chatRoomID, uint sentMessageIndex, int failureReason);

		// Token: 0x020000AF RID: 175
		public enum FailureReason
		{
			// Token: 0x040000E7 RID: 231
			FAILURE_REASON_UNDEFINED,
			// Token: 0x040000E8 RID: 232
			FAILURE_REASON_FORBIDDEN,
			// Token: 0x040000E9 RID: 233
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
