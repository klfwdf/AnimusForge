using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200010E RID: 270
	public abstract class ILobbyEnteredListener : GalaxyTypeAwareListenerLobbyEntered
	{
		// Token: 0x06000A52 RID: 2642 RVA: 0x000094B8 File Offset: 0x000076B8
		internal ILobbyEnteredListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ILobbyEnteredListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ILobbyEnteredListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000A53 RID: 2643 RVA: 0x000094E0 File Offset: 0x000076E0
		public ILobbyEnteredListener()
			: this(GalaxyInstancePINVOKE.new_ILobbyEnteredListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000A54 RID: 2644 RVA: 0x00009504 File Offset: 0x00007704
		internal static HandleRef getCPtr(ILobbyEnteredListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000A55 RID: 2645 RVA: 0x00009524 File Offset: 0x00007724
		~ILobbyEnteredListener()
		{
			this.Dispose();
		}

		// Token: 0x06000A56 RID: 2646 RVA: 0x00009554 File Offset: 0x00007754
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ILobbyEnteredListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ILobbyEnteredListener.listeners.ContainsKey(handle))
					{
						ILobbyEnteredListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000A57 RID: 2647
		public abstract void OnLobbyEntered(GalaxyID lobbyID, LobbyEnterResult _result);

		// Token: 0x06000A58 RID: 2648 RVA: 0x00009604 File Offset: 0x00007804
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnLobbyEntered", ILobbyEnteredListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ILobbyEnteredListener.SwigDelegateILobbyEnteredListener_0(ILobbyEnteredListener.SwigDirectorOnLobbyEntered);
			}
			GalaxyInstancePINVOKE.ILobbyEnteredListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000A59 RID: 2649 RVA: 0x00009640 File Offset: 0x00007840
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ILobbyEnteredListener));
		}

		// Token: 0x06000A5A RID: 2650 RVA: 0x00009676 File Offset: 0x00007876
		[MonoPInvokeCallback(typeof(ILobbyEnteredListener.SwigDelegateILobbyEnteredListener_0))]
		private static void SwigDirectorOnLobbyEntered(IntPtr cPtr, IntPtr lobbyID, int _result)
		{
			if (ILobbyEnteredListener.listeners.ContainsKey(cPtr))
			{
				ILobbyEnteredListener.listeners[cPtr].OnLobbyEntered(new GalaxyID(new GalaxyID(lobbyID, false).ToUint64()), (LobbyEnterResult)_result);
			}
		}

		// Token: 0x040001C6 RID: 454
		private static Dictionary<IntPtr, ILobbyEnteredListener> listeners = new Dictionary<IntPtr, ILobbyEnteredListener>();

		// Token: 0x040001C7 RID: 455
		private HandleRef swigCPtr;

		// Token: 0x040001C8 RID: 456
		private ILobbyEnteredListener.SwigDelegateILobbyEnteredListener_0 swigDelegate0;

		// Token: 0x040001C9 RID: 457
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(GalaxyID),
			typeof(LobbyEnterResult)
		};

		// Token: 0x0200010F RID: 271
		// (Invoke) Token: 0x06000A5D RID: 2653
		public delegate void SwigDelegateILobbyEnteredListener_0(IntPtr cPtr, IntPtr lobbyID, int _result);
	}
}
