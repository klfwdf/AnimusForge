using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000104 RID: 260
	public abstract class ILobbyDataListener : GalaxyTypeAwareListenerLobbyData
	{
		// Token: 0x06000A1C RID: 2588 RVA: 0x0000898C File Offset: 0x00006B8C
		internal ILobbyDataListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ILobbyDataListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ILobbyDataListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000A1D RID: 2589 RVA: 0x000089B4 File Offset: 0x00006BB4
		public ILobbyDataListener()
			: this(GalaxyInstancePINVOKE.new_ILobbyDataListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000A1E RID: 2590 RVA: 0x000089D8 File Offset: 0x00006BD8
		internal static HandleRef getCPtr(ILobbyDataListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000A1F RID: 2591 RVA: 0x000089F8 File Offset: 0x00006BF8
		~ILobbyDataListener()
		{
			this.Dispose();
		}

		// Token: 0x06000A20 RID: 2592 RVA: 0x00008A28 File Offset: 0x00006C28
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ILobbyDataListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ILobbyDataListener.listeners.ContainsKey(handle))
					{
						ILobbyDataListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000A21 RID: 2593
		public abstract void OnLobbyDataUpdated(GalaxyID lobbyID, GalaxyID memberID);

		// Token: 0x06000A22 RID: 2594 RVA: 0x00008AD8 File Offset: 0x00006CD8
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnLobbyDataUpdated", ILobbyDataListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ILobbyDataListener.SwigDelegateILobbyDataListener_0(ILobbyDataListener.SwigDirectorOnLobbyDataUpdated);
			}
			GalaxyInstancePINVOKE.ILobbyDataListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000A23 RID: 2595 RVA: 0x00008B14 File Offset: 0x00006D14
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ILobbyDataListener));
		}

		// Token: 0x06000A24 RID: 2596 RVA: 0x00008B4C File Offset: 0x00006D4C
		[MonoPInvokeCallback(typeof(ILobbyDataListener.SwigDelegateILobbyDataListener_0))]
		private static void SwigDirectorOnLobbyDataUpdated(IntPtr cPtr, IntPtr lobbyID, IntPtr memberID)
		{
			if (ILobbyDataListener.listeners.ContainsKey(cPtr))
			{
				ILobbyDataListener.listeners[cPtr].OnLobbyDataUpdated(new GalaxyID(new GalaxyID(lobbyID, false).ToUint64()), new GalaxyID(new GalaxyID(memberID, false).ToUint64()));
			}
		}

		// Token: 0x040001AE RID: 430
		private static Dictionary<IntPtr, ILobbyDataListener> listeners = new Dictionary<IntPtr, ILobbyDataListener>();

		// Token: 0x040001AF RID: 431
		private HandleRef swigCPtr;

		// Token: 0x040001B0 RID: 432
		private ILobbyDataListener.SwigDelegateILobbyDataListener_0 swigDelegate0;

		// Token: 0x040001B1 RID: 433
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(GalaxyID),
			typeof(GalaxyID)
		};

		// Token: 0x02000105 RID: 261
		// (Invoke) Token: 0x06000A27 RID: 2599
		public delegate void SwigDelegateILobbyDataListener_0(IntPtr cPtr, IntPtr lobbyID, IntPtr memberID);
	}
}
