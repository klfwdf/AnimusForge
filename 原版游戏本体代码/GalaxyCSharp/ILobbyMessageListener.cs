using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200011B RID: 283
	public abstract class ILobbyMessageListener : GalaxyTypeAwareListenerLobbyMessage
	{
		// Token: 0x06000A9E RID: 2718 RVA: 0x0000A308 File Offset: 0x00008508
		internal ILobbyMessageListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ILobbyMessageListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ILobbyMessageListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000A9F RID: 2719 RVA: 0x0000A330 File Offset: 0x00008530
		public ILobbyMessageListener()
			: this(GalaxyInstancePINVOKE.new_ILobbyMessageListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000AA0 RID: 2720 RVA: 0x0000A354 File Offset: 0x00008554
		internal static HandleRef getCPtr(ILobbyMessageListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000AA1 RID: 2721 RVA: 0x0000A374 File Offset: 0x00008574
		~ILobbyMessageListener()
		{
			this.Dispose();
		}

		// Token: 0x06000AA2 RID: 2722 RVA: 0x0000A3A4 File Offset: 0x000085A4
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ILobbyMessageListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ILobbyMessageListener.listeners.ContainsKey(handle))
					{
						ILobbyMessageListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000AA3 RID: 2723
		public abstract void OnLobbyMessageReceived(GalaxyID lobbyID, GalaxyID senderID, uint messageID, uint messageLength);

		// Token: 0x06000AA4 RID: 2724 RVA: 0x0000A454 File Offset: 0x00008654
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnLobbyMessageReceived", ILobbyMessageListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ILobbyMessageListener.SwigDelegateILobbyMessageListener_0(ILobbyMessageListener.SwigDirectorOnLobbyMessageReceived);
			}
			GalaxyInstancePINVOKE.ILobbyMessageListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000AA5 RID: 2725 RVA: 0x0000A490 File Offset: 0x00008690
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ILobbyMessageListener));
		}

		// Token: 0x06000AA6 RID: 2726 RVA: 0x0000A4C8 File Offset: 0x000086C8
		[MonoPInvokeCallback(typeof(ILobbyMessageListener.SwigDelegateILobbyMessageListener_0))]
		private static void SwigDirectorOnLobbyMessageReceived(IntPtr cPtr, IntPtr lobbyID, IntPtr senderID, uint messageID, uint messageLength)
		{
			if (ILobbyMessageListener.listeners.ContainsKey(cPtr))
			{
				ILobbyMessageListener.listeners[cPtr].OnLobbyMessageReceived(new GalaxyID(new GalaxyID(lobbyID, false).ToUint64()), new GalaxyID(new GalaxyID(senderID, false).ToUint64()), messageID, messageLength);
			}
		}

		// Token: 0x040001E5 RID: 485
		private static Dictionary<IntPtr, ILobbyMessageListener> listeners = new Dictionary<IntPtr, ILobbyMessageListener>();

		// Token: 0x040001E6 RID: 486
		private HandleRef swigCPtr;

		// Token: 0x040001E7 RID: 487
		private ILobbyMessageListener.SwigDelegateILobbyMessageListener_0 swigDelegate0;

		// Token: 0x040001E8 RID: 488
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(GalaxyID),
			typeof(GalaxyID),
			typeof(uint),
			typeof(uint)
		};

		// Token: 0x0200011C RID: 284
		// (Invoke) Token: 0x06000AA9 RID: 2729
		public delegate void SwigDelegateILobbyMessageListener_0(IntPtr cPtr, IntPtr lobbyID, IntPtr senderID, uint messageID, uint messageLength);
	}
}
