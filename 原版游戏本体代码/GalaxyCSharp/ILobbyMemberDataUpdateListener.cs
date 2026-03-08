using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000115 RID: 277
	public abstract class ILobbyMemberDataUpdateListener : GalaxyTypeAwareListenerLobbyMemberDataUpdate
	{
		// Token: 0x06000A7C RID: 2684 RVA: 0x00009B58 File Offset: 0x00007D58
		internal ILobbyMemberDataUpdateListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ILobbyMemberDataUpdateListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ILobbyMemberDataUpdateListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000A7D RID: 2685 RVA: 0x00009B80 File Offset: 0x00007D80
		public ILobbyMemberDataUpdateListener()
			: this(GalaxyInstancePINVOKE.new_ILobbyMemberDataUpdateListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000A7E RID: 2686 RVA: 0x00009BA4 File Offset: 0x00007DA4
		internal static HandleRef getCPtr(ILobbyMemberDataUpdateListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000A7F RID: 2687 RVA: 0x00009BC4 File Offset: 0x00007DC4
		~ILobbyMemberDataUpdateListener()
		{
			this.Dispose();
		}

		// Token: 0x06000A80 RID: 2688 RVA: 0x00009BF4 File Offset: 0x00007DF4
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ILobbyMemberDataUpdateListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ILobbyMemberDataUpdateListener.listeners.ContainsKey(handle))
					{
						ILobbyMemberDataUpdateListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000A81 RID: 2689
		public abstract void OnLobbyMemberDataUpdateSuccess(GalaxyID lobbyID, GalaxyID memberID);

		// Token: 0x06000A82 RID: 2690
		public abstract void OnLobbyMemberDataUpdateFailure(GalaxyID lobbyID, GalaxyID memberID, ILobbyMemberDataUpdateListener.FailureReason failureReason);

		// Token: 0x06000A83 RID: 2691 RVA: 0x00009CA4 File Offset: 0x00007EA4
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnLobbyMemberDataUpdateSuccess", ILobbyMemberDataUpdateListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ILobbyMemberDataUpdateListener.SwigDelegateILobbyMemberDataUpdateListener_0(ILobbyMemberDataUpdateListener.SwigDirectorOnLobbyMemberDataUpdateSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnLobbyMemberDataUpdateFailure", ILobbyMemberDataUpdateListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new ILobbyMemberDataUpdateListener.SwigDelegateILobbyMemberDataUpdateListener_1(ILobbyMemberDataUpdateListener.SwigDirectorOnLobbyMemberDataUpdateFailure);
			}
			GalaxyInstancePINVOKE.ILobbyMemberDataUpdateListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000A84 RID: 2692 RVA: 0x00009D18 File Offset: 0x00007F18
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ILobbyMemberDataUpdateListener));
		}

		// Token: 0x06000A85 RID: 2693 RVA: 0x00009D50 File Offset: 0x00007F50
		[MonoPInvokeCallback(typeof(ILobbyMemberDataUpdateListener.SwigDelegateILobbyMemberDataUpdateListener_0))]
		private static void SwigDirectorOnLobbyMemberDataUpdateSuccess(IntPtr cPtr, IntPtr lobbyID, IntPtr memberID)
		{
			if (ILobbyMemberDataUpdateListener.listeners.ContainsKey(cPtr))
			{
				ILobbyMemberDataUpdateListener.listeners[cPtr].OnLobbyMemberDataUpdateSuccess(new GalaxyID(new GalaxyID(lobbyID, false).ToUint64()), new GalaxyID(new GalaxyID(memberID, false).ToUint64()));
			}
		}

		// Token: 0x06000A86 RID: 2694 RVA: 0x00009DA0 File Offset: 0x00007FA0
		[MonoPInvokeCallback(typeof(ILobbyMemberDataUpdateListener.SwigDelegateILobbyMemberDataUpdateListener_1))]
		private static void SwigDirectorOnLobbyMemberDataUpdateFailure(IntPtr cPtr, IntPtr lobbyID, IntPtr memberID, int failureReason)
		{
			if (ILobbyMemberDataUpdateListener.listeners.ContainsKey(cPtr))
			{
				ILobbyMemberDataUpdateListener.listeners[cPtr].OnLobbyMemberDataUpdateFailure(new GalaxyID(new GalaxyID(lobbyID, false).ToUint64()), new GalaxyID(new GalaxyID(memberID, false).ToUint64()), (ILobbyMemberDataUpdateListener.FailureReason)failureReason);
			}
		}

		// Token: 0x040001D7 RID: 471
		private static Dictionary<IntPtr, ILobbyMemberDataUpdateListener> listeners = new Dictionary<IntPtr, ILobbyMemberDataUpdateListener>();

		// Token: 0x040001D8 RID: 472
		private HandleRef swigCPtr;

		// Token: 0x040001D9 RID: 473
		private ILobbyMemberDataUpdateListener.SwigDelegateILobbyMemberDataUpdateListener_0 swigDelegate0;

		// Token: 0x040001DA RID: 474
		private ILobbyMemberDataUpdateListener.SwigDelegateILobbyMemberDataUpdateListener_1 swigDelegate1;

		// Token: 0x040001DB RID: 475
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(GalaxyID),
			typeof(GalaxyID)
		};

		// Token: 0x040001DC RID: 476
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(GalaxyID),
			typeof(GalaxyID),
			typeof(ILobbyMemberDataUpdateListener.FailureReason)
		};

		// Token: 0x02000116 RID: 278
		// (Invoke) Token: 0x06000A89 RID: 2697
		public delegate void SwigDelegateILobbyMemberDataUpdateListener_0(IntPtr cPtr, IntPtr lobbyID, IntPtr memberID);

		// Token: 0x02000117 RID: 279
		// (Invoke) Token: 0x06000A8D RID: 2701
		public delegate void SwigDelegateILobbyMemberDataUpdateListener_1(IntPtr cPtr, IntPtr lobbyID, IntPtr memberID, int failureReason);

		// Token: 0x02000118 RID: 280
		public enum FailureReason
		{
			// Token: 0x040001DE RID: 478
			FAILURE_REASON_UNDEFINED,
			// Token: 0x040001DF RID: 479
			FAILURE_REASON_LOBBY_DOES_NOT_EXIST,
			// Token: 0x040001E0 RID: 480
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
