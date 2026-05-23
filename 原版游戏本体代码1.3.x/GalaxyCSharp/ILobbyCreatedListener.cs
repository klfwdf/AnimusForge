using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000102 RID: 258
	public abstract class ILobbyCreatedListener : GalaxyTypeAwareListenerLobbyCreated
	{
		// Token: 0x06000A0E RID: 2574 RVA: 0x0000863C File Offset: 0x0000683C
		internal ILobbyCreatedListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ILobbyCreatedListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ILobbyCreatedListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000A0F RID: 2575 RVA: 0x00008664 File Offset: 0x00006864
		public ILobbyCreatedListener()
			: this(GalaxyInstancePINVOKE.new_ILobbyCreatedListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000A10 RID: 2576 RVA: 0x00008688 File Offset: 0x00006888
		internal static HandleRef getCPtr(ILobbyCreatedListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000A11 RID: 2577 RVA: 0x000086A8 File Offset: 0x000068A8
		~ILobbyCreatedListener()
		{
			this.Dispose();
		}

		// Token: 0x06000A12 RID: 2578 RVA: 0x000086D8 File Offset: 0x000068D8
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ILobbyCreatedListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ILobbyCreatedListener.listeners.ContainsKey(handle))
					{
						ILobbyCreatedListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000A13 RID: 2579
		public abstract void OnLobbyCreated(GalaxyID lobbyID, LobbyCreateResult _result);

		// Token: 0x06000A14 RID: 2580 RVA: 0x00008788 File Offset: 0x00006988
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnLobbyCreated", ILobbyCreatedListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ILobbyCreatedListener.SwigDelegateILobbyCreatedListener_0(ILobbyCreatedListener.SwigDirectorOnLobbyCreated);
			}
			GalaxyInstancePINVOKE.ILobbyCreatedListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000A15 RID: 2581 RVA: 0x000087C4 File Offset: 0x000069C4
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ILobbyCreatedListener));
		}

		// Token: 0x06000A16 RID: 2582 RVA: 0x000087FA File Offset: 0x000069FA
		[MonoPInvokeCallback(typeof(ILobbyCreatedListener.SwigDelegateILobbyCreatedListener_0))]
		private static void SwigDirectorOnLobbyCreated(IntPtr cPtr, IntPtr lobbyID, int _result)
		{
			if (ILobbyCreatedListener.listeners.ContainsKey(cPtr))
			{
				ILobbyCreatedListener.listeners[cPtr].OnLobbyCreated(new GalaxyID(new GalaxyID(lobbyID, false).ToUint64()), (LobbyCreateResult)_result);
			}
		}

		// Token: 0x040001AA RID: 426
		private static Dictionary<IntPtr, ILobbyCreatedListener> listeners = new Dictionary<IntPtr, ILobbyCreatedListener>();

		// Token: 0x040001AB RID: 427
		private HandleRef swigCPtr;

		// Token: 0x040001AC RID: 428
		private ILobbyCreatedListener.SwigDelegateILobbyCreatedListener_0 swigDelegate0;

		// Token: 0x040001AD RID: 429
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(GalaxyID),
			typeof(LobbyCreateResult)
		};

		// Token: 0x02000103 RID: 259
		// (Invoke) Token: 0x06000A19 RID: 2585
		public delegate void SwigDelegateILobbyCreatedListener_0(IntPtr cPtr, IntPtr lobbyID, int _result);
	}
}
