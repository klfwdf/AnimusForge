using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000B0 RID: 176
	public abstract class IChatRoomMessagesListener : GalaxyTypeAwareListenerChatRoomMessages
	{
		// Token: 0x06000820 RID: 2080 RVA: 0x0000C600 File Offset: 0x0000A800
		internal IChatRoomMessagesListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IChatRoomMessagesListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IChatRoomMessagesListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000821 RID: 2081 RVA: 0x0000C628 File Offset: 0x0000A828
		public IChatRoomMessagesListener()
			: this(GalaxyInstancePINVOKE.new_IChatRoomMessagesListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000822 RID: 2082 RVA: 0x0000C64C File Offset: 0x0000A84C
		internal static HandleRef getCPtr(IChatRoomMessagesListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000823 RID: 2083 RVA: 0x0000C66C File Offset: 0x0000A86C
		~IChatRoomMessagesListener()
		{
			this.Dispose();
		}

		// Token: 0x06000824 RID: 2084 RVA: 0x0000C69C File Offset: 0x0000A89C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IChatRoomMessagesListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IChatRoomMessagesListener.listeners.ContainsKey(handle))
					{
						IChatRoomMessagesListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000825 RID: 2085
		public abstract void OnChatRoomMessagesReceived(ulong chatRoomID, uint messageCount, uint longestMessageLenght);

		// Token: 0x06000826 RID: 2086 RVA: 0x0000C74C File Offset: 0x0000A94C
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnChatRoomMessagesReceived", IChatRoomMessagesListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IChatRoomMessagesListener.SwigDelegateIChatRoomMessagesListener_0(IChatRoomMessagesListener.SwigDirectorOnChatRoomMessagesReceived);
			}
			GalaxyInstancePINVOKE.IChatRoomMessagesListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000827 RID: 2087 RVA: 0x0000C788 File Offset: 0x0000A988
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IChatRoomMessagesListener));
		}

		// Token: 0x06000828 RID: 2088 RVA: 0x0000C7BE File Offset: 0x0000A9BE
		[MonoPInvokeCallback(typeof(IChatRoomMessagesListener.SwigDelegateIChatRoomMessagesListener_0))]
		private static void SwigDirectorOnChatRoomMessagesReceived(IntPtr cPtr, ulong chatRoomID, uint messageCount, uint longestMessageLenght)
		{
			if (IChatRoomMessagesListener.listeners.ContainsKey(cPtr))
			{
				IChatRoomMessagesListener.listeners[cPtr].OnChatRoomMessagesReceived(chatRoomID, messageCount, longestMessageLenght);
			}
		}

		// Token: 0x040000EA RID: 234
		private static Dictionary<IntPtr, IChatRoomMessagesListener> listeners = new Dictionary<IntPtr, IChatRoomMessagesListener>();

		// Token: 0x040000EB RID: 235
		private HandleRef swigCPtr;

		// Token: 0x040000EC RID: 236
		private IChatRoomMessagesListener.SwigDelegateIChatRoomMessagesListener_0 swigDelegate0;

		// Token: 0x040000ED RID: 237
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(ulong),
			typeof(uint),
			typeof(uint)
		};

		// Token: 0x020000B1 RID: 177
		// (Invoke) Token: 0x0600082B RID: 2091
		public delegate void SwigDelegateIChatRoomMessagesListener_0(IntPtr cPtr, ulong chatRoomID, uint messageCount, uint longestMessageLenght);
	}
}
