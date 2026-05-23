using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000106 RID: 262
	public abstract class ILobbyDataRetrieveListener : GalaxyTypeAwareListenerLobbyDataRetrieve
	{
		// Token: 0x06000A2A RID: 2602 RVA: 0x00008CF8 File Offset: 0x00006EF8
		internal ILobbyDataRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ILobbyDataRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ILobbyDataRetrieveListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000A2B RID: 2603 RVA: 0x00008D20 File Offset: 0x00006F20
		public ILobbyDataRetrieveListener()
			: this(GalaxyInstancePINVOKE.new_ILobbyDataRetrieveListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000A2C RID: 2604 RVA: 0x00008D44 File Offset: 0x00006F44
		internal static HandleRef getCPtr(ILobbyDataRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000A2D RID: 2605 RVA: 0x00008D64 File Offset: 0x00006F64
		~ILobbyDataRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x06000A2E RID: 2606 RVA: 0x00008D94 File Offset: 0x00006F94
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ILobbyDataRetrieveListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ILobbyDataRetrieveListener.listeners.ContainsKey(handle))
					{
						ILobbyDataRetrieveListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000A2F RID: 2607
		public abstract void OnLobbyDataRetrieveSuccess(GalaxyID lobbyID);

		// Token: 0x06000A30 RID: 2608
		public abstract void OnLobbyDataRetrieveFailure(GalaxyID lobbyID, ILobbyDataRetrieveListener.FailureReason failureReason);

		// Token: 0x06000A31 RID: 2609 RVA: 0x00008E44 File Offset: 0x00007044
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnLobbyDataRetrieveSuccess", ILobbyDataRetrieveListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ILobbyDataRetrieveListener.SwigDelegateILobbyDataRetrieveListener_0(ILobbyDataRetrieveListener.SwigDirectorOnLobbyDataRetrieveSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnLobbyDataRetrieveFailure", ILobbyDataRetrieveListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new ILobbyDataRetrieveListener.SwigDelegateILobbyDataRetrieveListener_1(ILobbyDataRetrieveListener.SwigDirectorOnLobbyDataRetrieveFailure);
			}
			GalaxyInstancePINVOKE.ILobbyDataRetrieveListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000A32 RID: 2610 RVA: 0x00008EB8 File Offset: 0x000070B8
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ILobbyDataRetrieveListener));
		}

		// Token: 0x06000A33 RID: 2611 RVA: 0x00008EEE File Offset: 0x000070EE
		[MonoPInvokeCallback(typeof(ILobbyDataRetrieveListener.SwigDelegateILobbyDataRetrieveListener_0))]
		private static void SwigDirectorOnLobbyDataRetrieveSuccess(IntPtr cPtr, IntPtr lobbyID)
		{
			if (ILobbyDataRetrieveListener.listeners.ContainsKey(cPtr))
			{
				ILobbyDataRetrieveListener.listeners[cPtr].OnLobbyDataRetrieveSuccess(new GalaxyID(new GalaxyID(lobbyID, false).ToUint64()));
			}
		}

		// Token: 0x06000A34 RID: 2612 RVA: 0x00008F21 File Offset: 0x00007121
		[MonoPInvokeCallback(typeof(ILobbyDataRetrieveListener.SwigDelegateILobbyDataRetrieveListener_1))]
		private static void SwigDirectorOnLobbyDataRetrieveFailure(IntPtr cPtr, IntPtr lobbyID, int failureReason)
		{
			if (ILobbyDataRetrieveListener.listeners.ContainsKey(cPtr))
			{
				ILobbyDataRetrieveListener.listeners[cPtr].OnLobbyDataRetrieveFailure(new GalaxyID(new GalaxyID(lobbyID, false).ToUint64()), (ILobbyDataRetrieveListener.FailureReason)failureReason);
			}
		}

		// Token: 0x040001B2 RID: 434
		private static Dictionary<IntPtr, ILobbyDataRetrieveListener> listeners = new Dictionary<IntPtr, ILobbyDataRetrieveListener>();

		// Token: 0x040001B3 RID: 435
		private HandleRef swigCPtr;

		// Token: 0x040001B4 RID: 436
		private ILobbyDataRetrieveListener.SwigDelegateILobbyDataRetrieveListener_0 swigDelegate0;

		// Token: 0x040001B5 RID: 437
		private ILobbyDataRetrieveListener.SwigDelegateILobbyDataRetrieveListener_1 swigDelegate1;

		// Token: 0x040001B6 RID: 438
		private static Type[] swigMethodTypes0 = new Type[] { typeof(GalaxyID) };

		// Token: 0x040001B7 RID: 439
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(GalaxyID),
			typeof(ILobbyDataRetrieveListener.FailureReason)
		};

		// Token: 0x02000107 RID: 263
		// (Invoke) Token: 0x06000A37 RID: 2615
		public delegate void SwigDelegateILobbyDataRetrieveListener_0(IntPtr cPtr, IntPtr lobbyID);

		// Token: 0x02000108 RID: 264
		// (Invoke) Token: 0x06000A3B RID: 2619
		public delegate void SwigDelegateILobbyDataRetrieveListener_1(IntPtr cPtr, IntPtr lobbyID, int failureReason);

		// Token: 0x02000109 RID: 265
		public enum FailureReason
		{
			// Token: 0x040001B9 RID: 441
			FAILURE_REASON_UNDEFINED,
			// Token: 0x040001BA RID: 442
			FAILURE_REASON_LOBBY_DOES_NOT_EXIST,
			// Token: 0x040001BB RID: 443
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
