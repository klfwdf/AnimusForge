using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200010A RID: 266
	public abstract class ILobbyDataUpdateListener : GalaxyTypeAwareListenerLobbyDataUpdate
	{
		// Token: 0x06000A3E RID: 2622 RVA: 0x000090D8 File Offset: 0x000072D8
		internal ILobbyDataUpdateListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ILobbyDataUpdateListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ILobbyDataUpdateListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000A3F RID: 2623 RVA: 0x00009100 File Offset: 0x00007300
		public ILobbyDataUpdateListener()
			: this(GalaxyInstancePINVOKE.new_ILobbyDataUpdateListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000A40 RID: 2624 RVA: 0x00009124 File Offset: 0x00007324
		internal static HandleRef getCPtr(ILobbyDataUpdateListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000A41 RID: 2625 RVA: 0x00009144 File Offset: 0x00007344
		~ILobbyDataUpdateListener()
		{
			this.Dispose();
		}

		// Token: 0x06000A42 RID: 2626 RVA: 0x00009174 File Offset: 0x00007374
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ILobbyDataUpdateListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ILobbyDataUpdateListener.listeners.ContainsKey(handle))
					{
						ILobbyDataUpdateListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000A43 RID: 2627
		public abstract void OnLobbyDataUpdateSuccess(GalaxyID lobbyID);

		// Token: 0x06000A44 RID: 2628
		public abstract void OnLobbyDataUpdateFailure(GalaxyID lobbyID, ILobbyDataUpdateListener.FailureReason failureReason);

		// Token: 0x06000A45 RID: 2629 RVA: 0x00009224 File Offset: 0x00007424
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnLobbyDataUpdateSuccess", ILobbyDataUpdateListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ILobbyDataUpdateListener.SwigDelegateILobbyDataUpdateListener_0(ILobbyDataUpdateListener.SwigDirectorOnLobbyDataUpdateSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnLobbyDataUpdateFailure", ILobbyDataUpdateListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new ILobbyDataUpdateListener.SwigDelegateILobbyDataUpdateListener_1(ILobbyDataUpdateListener.SwigDirectorOnLobbyDataUpdateFailure);
			}
			GalaxyInstancePINVOKE.ILobbyDataUpdateListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000A46 RID: 2630 RVA: 0x00009298 File Offset: 0x00007498
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ILobbyDataUpdateListener));
		}

		// Token: 0x06000A47 RID: 2631 RVA: 0x000092CE File Offset: 0x000074CE
		[MonoPInvokeCallback(typeof(ILobbyDataUpdateListener.SwigDelegateILobbyDataUpdateListener_0))]
		private static void SwigDirectorOnLobbyDataUpdateSuccess(IntPtr cPtr, IntPtr lobbyID)
		{
			if (ILobbyDataUpdateListener.listeners.ContainsKey(cPtr))
			{
				ILobbyDataUpdateListener.listeners[cPtr].OnLobbyDataUpdateSuccess(new GalaxyID(new GalaxyID(lobbyID, false).ToUint64()));
			}
		}

		// Token: 0x06000A48 RID: 2632 RVA: 0x00009301 File Offset: 0x00007501
		[MonoPInvokeCallback(typeof(ILobbyDataUpdateListener.SwigDelegateILobbyDataUpdateListener_1))]
		private static void SwigDirectorOnLobbyDataUpdateFailure(IntPtr cPtr, IntPtr lobbyID, int failureReason)
		{
			if (ILobbyDataUpdateListener.listeners.ContainsKey(cPtr))
			{
				ILobbyDataUpdateListener.listeners[cPtr].OnLobbyDataUpdateFailure(new GalaxyID(new GalaxyID(lobbyID, false).ToUint64()), (ILobbyDataUpdateListener.FailureReason)failureReason);
			}
		}

		// Token: 0x040001BC RID: 444
		private static Dictionary<IntPtr, ILobbyDataUpdateListener> listeners = new Dictionary<IntPtr, ILobbyDataUpdateListener>();

		// Token: 0x040001BD RID: 445
		private HandleRef swigCPtr;

		// Token: 0x040001BE RID: 446
		private ILobbyDataUpdateListener.SwigDelegateILobbyDataUpdateListener_0 swigDelegate0;

		// Token: 0x040001BF RID: 447
		private ILobbyDataUpdateListener.SwigDelegateILobbyDataUpdateListener_1 swigDelegate1;

		// Token: 0x040001C0 RID: 448
		private static Type[] swigMethodTypes0 = new Type[] { typeof(GalaxyID) };

		// Token: 0x040001C1 RID: 449
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(GalaxyID),
			typeof(ILobbyDataUpdateListener.FailureReason)
		};

		// Token: 0x0200010B RID: 267
		// (Invoke) Token: 0x06000A4B RID: 2635
		public delegate void SwigDelegateILobbyDataUpdateListener_0(IntPtr cPtr, IntPtr lobbyID);

		// Token: 0x0200010C RID: 268
		// (Invoke) Token: 0x06000A4F RID: 2639
		public delegate void SwigDelegateILobbyDataUpdateListener_1(IntPtr cPtr, IntPtr lobbyID, int failureReason);

		// Token: 0x0200010D RID: 269
		public enum FailureReason
		{
			// Token: 0x040001C3 RID: 451
			FAILURE_REASON_UNDEFINED,
			// Token: 0x040001C4 RID: 452
			FAILURE_REASON_LOBBY_DOES_NOT_EXIST,
			// Token: 0x040001C5 RID: 453
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
