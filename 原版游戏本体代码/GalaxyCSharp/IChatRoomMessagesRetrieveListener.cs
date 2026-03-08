using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000B2 RID: 178
	public abstract class IChatRoomMessagesRetrieveListener : GalaxyTypeAwareListenerChatRoomMessagesRetrieve
	{
		// Token: 0x0600082E RID: 2094 RVA: 0x0000C950 File Offset: 0x0000AB50
		internal IChatRoomMessagesRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IChatRoomMessagesRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IChatRoomMessagesRetrieveListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x0600082F RID: 2095 RVA: 0x0000C978 File Offset: 0x0000AB78
		public IChatRoomMessagesRetrieveListener()
			: this(GalaxyInstancePINVOKE.new_IChatRoomMessagesRetrieveListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000830 RID: 2096 RVA: 0x0000C99C File Offset: 0x0000AB9C
		internal static HandleRef getCPtr(IChatRoomMessagesRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000831 RID: 2097 RVA: 0x0000C9BC File Offset: 0x0000ABBC
		~IChatRoomMessagesRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x06000832 RID: 2098 RVA: 0x0000C9EC File Offset: 0x0000ABEC
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IChatRoomMessagesRetrieveListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IChatRoomMessagesRetrieveListener.listeners.ContainsKey(handle))
					{
						IChatRoomMessagesRetrieveListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000833 RID: 2099
		public abstract void OnChatRoomMessagesRetrieveSuccess(ulong chatRoomID, uint messageCount, uint longestMessageLenght);

		// Token: 0x06000834 RID: 2100
		public abstract void OnChatRoomMessagesRetrieveFailure(ulong chatRoomID, IChatRoomMessagesRetrieveListener.FailureReason failureReason);

		// Token: 0x06000835 RID: 2101 RVA: 0x0000CA9C File Offset: 0x0000AC9C
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnChatRoomMessagesRetrieveSuccess", IChatRoomMessagesRetrieveListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IChatRoomMessagesRetrieveListener.SwigDelegateIChatRoomMessagesRetrieveListener_0(IChatRoomMessagesRetrieveListener.SwigDirectorOnChatRoomMessagesRetrieveSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnChatRoomMessagesRetrieveFailure", IChatRoomMessagesRetrieveListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IChatRoomMessagesRetrieveListener.SwigDelegateIChatRoomMessagesRetrieveListener_1(IChatRoomMessagesRetrieveListener.SwigDirectorOnChatRoomMessagesRetrieveFailure);
			}
			GalaxyInstancePINVOKE.IChatRoomMessagesRetrieveListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000836 RID: 2102 RVA: 0x0000CB10 File Offset: 0x0000AD10
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IChatRoomMessagesRetrieveListener));
		}

		// Token: 0x06000837 RID: 2103 RVA: 0x0000CB46 File Offset: 0x0000AD46
		[MonoPInvokeCallback(typeof(IChatRoomMessagesRetrieveListener.SwigDelegateIChatRoomMessagesRetrieveListener_0))]
		private static void SwigDirectorOnChatRoomMessagesRetrieveSuccess(IntPtr cPtr, ulong chatRoomID, uint messageCount, uint longestMessageLenght)
		{
			if (IChatRoomMessagesRetrieveListener.listeners.ContainsKey(cPtr))
			{
				IChatRoomMessagesRetrieveListener.listeners[cPtr].OnChatRoomMessagesRetrieveSuccess(chatRoomID, messageCount, longestMessageLenght);
			}
		}

		// Token: 0x06000838 RID: 2104 RVA: 0x0000CB6B File Offset: 0x0000AD6B
		[MonoPInvokeCallback(typeof(IChatRoomMessagesRetrieveListener.SwigDelegateIChatRoomMessagesRetrieveListener_1))]
		private static void SwigDirectorOnChatRoomMessagesRetrieveFailure(IntPtr cPtr, ulong chatRoomID, int failureReason)
		{
			if (IChatRoomMessagesRetrieveListener.listeners.ContainsKey(cPtr))
			{
				IChatRoomMessagesRetrieveListener.listeners[cPtr].OnChatRoomMessagesRetrieveFailure(chatRoomID, (IChatRoomMessagesRetrieveListener.FailureReason)failureReason);
			}
		}

		// Token: 0x040000EE RID: 238
		private static Dictionary<IntPtr, IChatRoomMessagesRetrieveListener> listeners = new Dictionary<IntPtr, IChatRoomMessagesRetrieveListener>();

		// Token: 0x040000EF RID: 239
		private HandleRef swigCPtr;

		// Token: 0x040000F0 RID: 240
		private IChatRoomMessagesRetrieveListener.SwigDelegateIChatRoomMessagesRetrieveListener_0 swigDelegate0;

		// Token: 0x040000F1 RID: 241
		private IChatRoomMessagesRetrieveListener.SwigDelegateIChatRoomMessagesRetrieveListener_1 swigDelegate1;

		// Token: 0x040000F2 RID: 242
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(ulong),
			typeof(uint),
			typeof(uint)
		};

		// Token: 0x040000F3 RID: 243
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(ulong),
			typeof(IChatRoomMessagesRetrieveListener.FailureReason)
		};

		// Token: 0x020000B3 RID: 179
		// (Invoke) Token: 0x0600083B RID: 2107
		public delegate void SwigDelegateIChatRoomMessagesRetrieveListener_0(IntPtr cPtr, ulong chatRoomID, uint messageCount, uint longestMessageLenght);

		// Token: 0x020000B4 RID: 180
		// (Invoke) Token: 0x0600083F RID: 2111
		public delegate void SwigDelegateIChatRoomMessagesRetrieveListener_1(IntPtr cPtr, ulong chatRoomID, int failureReason);

		// Token: 0x020000B5 RID: 181
		public enum FailureReason
		{
			// Token: 0x040000F5 RID: 245
			FAILURE_REASON_UNDEFINED,
			// Token: 0x040000F6 RID: 246
			FAILURE_REASON_FORBIDDEN,
			// Token: 0x040000F7 RID: 247
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
