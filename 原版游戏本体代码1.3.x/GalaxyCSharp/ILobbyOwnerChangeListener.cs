using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200011D RID: 285
	public abstract class ILobbyOwnerChangeListener : GalaxyTypeAwareListenerLobbyOwnerChange
	{
		// Token: 0x06000AAC RID: 2732 RVA: 0x00011FE0 File Offset: 0x000101E0
		internal ILobbyOwnerChangeListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ILobbyOwnerChangeListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ILobbyOwnerChangeListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000AAD RID: 2733 RVA: 0x00012008 File Offset: 0x00010208
		public ILobbyOwnerChangeListener()
			: this(GalaxyInstancePINVOKE.new_ILobbyOwnerChangeListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000AAE RID: 2734 RVA: 0x0001202C File Offset: 0x0001022C
		internal static HandleRef getCPtr(ILobbyOwnerChangeListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000AAF RID: 2735 RVA: 0x0001204C File Offset: 0x0001024C
		~ILobbyOwnerChangeListener()
		{
			this.Dispose();
		}

		// Token: 0x06000AB0 RID: 2736 RVA: 0x0001207C File Offset: 0x0001027C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ILobbyOwnerChangeListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ILobbyOwnerChangeListener.listeners.ContainsKey(handle))
					{
						ILobbyOwnerChangeListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000AB1 RID: 2737
		public abstract void OnLobbyOwnerChanged(GalaxyID lobbyID, GalaxyID newOwnerID);

		// Token: 0x06000AB2 RID: 2738 RVA: 0x0001212C File Offset: 0x0001032C
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnLobbyOwnerChanged", ILobbyOwnerChangeListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ILobbyOwnerChangeListener.SwigDelegateILobbyOwnerChangeListener_0(ILobbyOwnerChangeListener.SwigDirectorOnLobbyOwnerChanged);
			}
			GalaxyInstancePINVOKE.ILobbyOwnerChangeListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000AB3 RID: 2739 RVA: 0x00012168 File Offset: 0x00010368
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ILobbyOwnerChangeListener));
		}

		// Token: 0x06000AB4 RID: 2740 RVA: 0x000121A0 File Offset: 0x000103A0
		[MonoPInvokeCallback(typeof(ILobbyOwnerChangeListener.SwigDelegateILobbyOwnerChangeListener_0))]
		private static void SwigDirectorOnLobbyOwnerChanged(IntPtr cPtr, IntPtr lobbyID, IntPtr newOwnerID)
		{
			if (ILobbyOwnerChangeListener.listeners.ContainsKey(cPtr))
			{
				ILobbyOwnerChangeListener.listeners[cPtr].OnLobbyOwnerChanged(new GalaxyID(new GalaxyID(lobbyID, false).ToUint64()), new GalaxyID(new GalaxyID(newOwnerID, false).ToUint64()));
			}
		}

		// Token: 0x040001E9 RID: 489
		private static Dictionary<IntPtr, ILobbyOwnerChangeListener> listeners = new Dictionary<IntPtr, ILobbyOwnerChangeListener>();

		// Token: 0x040001EA RID: 490
		private HandleRef swigCPtr;

		// Token: 0x040001EB RID: 491
		private ILobbyOwnerChangeListener.SwigDelegateILobbyOwnerChangeListener_0 swigDelegate0;

		// Token: 0x040001EC RID: 492
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(GalaxyID),
			typeof(GalaxyID)
		};

		// Token: 0x0200011E RID: 286
		// (Invoke) Token: 0x06000AB7 RID: 2743
		public delegate void SwigDelegateILobbyOwnerChangeListener_0(IntPtr cPtr, IntPtr lobbyID, IntPtr newOwnerID);
	}
}
