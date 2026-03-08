using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000119 RID: 281
	public abstract class ILobbyMemberStateListener : GalaxyTypeAwareListenerLobbyMemberState
	{
		// Token: 0x06000A90 RID: 2704 RVA: 0x00009F8C File Offset: 0x0000818C
		internal ILobbyMemberStateListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ILobbyMemberStateListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ILobbyMemberStateListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000A91 RID: 2705 RVA: 0x00009FB4 File Offset: 0x000081B4
		public ILobbyMemberStateListener()
			: this(GalaxyInstancePINVOKE.new_ILobbyMemberStateListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000A92 RID: 2706 RVA: 0x00009FD8 File Offset: 0x000081D8
		internal static HandleRef getCPtr(ILobbyMemberStateListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000A93 RID: 2707 RVA: 0x00009FF8 File Offset: 0x000081F8
		~ILobbyMemberStateListener()
		{
			this.Dispose();
		}

		// Token: 0x06000A94 RID: 2708 RVA: 0x0000A028 File Offset: 0x00008228
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ILobbyMemberStateListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ILobbyMemberStateListener.listeners.ContainsKey(handle))
					{
						ILobbyMemberStateListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000A95 RID: 2709
		public abstract void OnLobbyMemberStateChanged(GalaxyID lobbyID, GalaxyID memberID, LobbyMemberStateChange memberStateChange);

		// Token: 0x06000A96 RID: 2710 RVA: 0x0000A0D8 File Offset: 0x000082D8
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnLobbyMemberStateChanged", ILobbyMemberStateListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ILobbyMemberStateListener.SwigDelegateILobbyMemberStateListener_0(ILobbyMemberStateListener.SwigDirectorOnLobbyMemberStateChanged);
			}
			GalaxyInstancePINVOKE.ILobbyMemberStateListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000A97 RID: 2711 RVA: 0x0000A114 File Offset: 0x00008314
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ILobbyMemberStateListener));
		}

		// Token: 0x06000A98 RID: 2712 RVA: 0x0000A14C File Offset: 0x0000834C
		[MonoPInvokeCallback(typeof(ILobbyMemberStateListener.SwigDelegateILobbyMemberStateListener_0))]
		private static void SwigDirectorOnLobbyMemberStateChanged(IntPtr cPtr, IntPtr lobbyID, IntPtr memberID, int memberStateChange)
		{
			if (ILobbyMemberStateListener.listeners.ContainsKey(cPtr))
			{
				ILobbyMemberStateListener.listeners[cPtr].OnLobbyMemberStateChanged(new GalaxyID(new GalaxyID(lobbyID, false).ToUint64()), new GalaxyID(new GalaxyID(memberID, false).ToUint64()), (LobbyMemberStateChange)memberStateChange);
			}
		}

		// Token: 0x040001E1 RID: 481
		private static Dictionary<IntPtr, ILobbyMemberStateListener> listeners = new Dictionary<IntPtr, ILobbyMemberStateListener>();

		// Token: 0x040001E2 RID: 482
		private HandleRef swigCPtr;

		// Token: 0x040001E3 RID: 483
		private ILobbyMemberStateListener.SwigDelegateILobbyMemberStateListener_0 swigDelegate0;

		// Token: 0x040001E4 RID: 484
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(GalaxyID),
			typeof(GalaxyID),
			typeof(LobbyMemberStateChange)
		};

		// Token: 0x0200011A RID: 282
		// (Invoke) Token: 0x06000A9B RID: 2715
		public delegate void SwigDelegateILobbyMemberStateListener_0(IntPtr cPtr, IntPtr lobbyID, IntPtr memberID, int memberStateChange);
	}
}
