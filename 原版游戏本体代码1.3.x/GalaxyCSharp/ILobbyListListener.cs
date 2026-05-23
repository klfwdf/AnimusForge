using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000113 RID: 275
	public abstract class ILobbyListListener : GalaxyTypeAwareListenerLobbyList
	{
		// Token: 0x06000A6E RID: 2670 RVA: 0x0001191C File Offset: 0x0000FB1C
		internal ILobbyListListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ILobbyListListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ILobbyListListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000A6F RID: 2671 RVA: 0x00011944 File Offset: 0x0000FB44
		public ILobbyListListener()
			: this(GalaxyInstancePINVOKE.new_ILobbyListListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000A70 RID: 2672 RVA: 0x00011968 File Offset: 0x0000FB68
		internal static HandleRef getCPtr(ILobbyListListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000A71 RID: 2673 RVA: 0x00011988 File Offset: 0x0000FB88
		~ILobbyListListener()
		{
			this.Dispose();
		}

		// Token: 0x06000A72 RID: 2674 RVA: 0x000119B8 File Offset: 0x0000FBB8
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ILobbyListListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ILobbyListListener.listeners.ContainsKey(handle))
					{
						ILobbyListListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000A73 RID: 2675
		public abstract void OnLobbyList(uint lobbyCount, LobbyListResult _result);

		// Token: 0x06000A74 RID: 2676 RVA: 0x00011A68 File Offset: 0x0000FC68
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnLobbyList", ILobbyListListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ILobbyListListener.SwigDelegateILobbyListListener_0(ILobbyListListener.SwigDirectorOnLobbyList);
			}
			GalaxyInstancePINVOKE.ILobbyListListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000A75 RID: 2677 RVA: 0x00011AA4 File Offset: 0x0000FCA4
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ILobbyListListener));
		}

		// Token: 0x06000A76 RID: 2678 RVA: 0x00011ADA File Offset: 0x0000FCDA
		[MonoPInvokeCallback(typeof(ILobbyListListener.SwigDelegateILobbyListListener_0))]
		private static void SwigDirectorOnLobbyList(IntPtr cPtr, uint lobbyCount, int _result)
		{
			if (ILobbyListListener.listeners.ContainsKey(cPtr))
			{
				ILobbyListListener.listeners[cPtr].OnLobbyList(lobbyCount, (LobbyListResult)_result);
			}
		}

		// Token: 0x040001D3 RID: 467
		private static Dictionary<IntPtr, ILobbyListListener> listeners = new Dictionary<IntPtr, ILobbyListListener>();

		// Token: 0x040001D4 RID: 468
		private HandleRef swigCPtr;

		// Token: 0x040001D5 RID: 469
		private ILobbyListListener.SwigDelegateILobbyListListener_0 swigDelegate0;

		// Token: 0x040001D6 RID: 470
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(uint),
			typeof(LobbyListResult)
		};

		// Token: 0x02000114 RID: 276
		// (Invoke) Token: 0x06000A79 RID: 2681
		public delegate void SwigDelegateILobbyListListener_0(IntPtr cPtr, uint lobbyCount, int _result);
	}
}
