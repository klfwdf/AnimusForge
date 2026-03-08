using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000110 RID: 272
	public abstract class ILobbyLeftListener : GalaxyTypeAwareListenerLobbyLeft
	{
		// Token: 0x06000A60 RID: 2656 RVA: 0x00009808 File Offset: 0x00007A08
		internal ILobbyLeftListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ILobbyLeftListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ILobbyLeftListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000A61 RID: 2657 RVA: 0x00009830 File Offset: 0x00007A30
		public ILobbyLeftListener()
			: this(GalaxyInstancePINVOKE.new_ILobbyLeftListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000A62 RID: 2658 RVA: 0x00009854 File Offset: 0x00007A54
		internal static HandleRef getCPtr(ILobbyLeftListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000A63 RID: 2659 RVA: 0x00009874 File Offset: 0x00007A74
		~ILobbyLeftListener()
		{
			this.Dispose();
		}

		// Token: 0x06000A64 RID: 2660 RVA: 0x000098A4 File Offset: 0x00007AA4
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ILobbyLeftListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ILobbyLeftListener.listeners.ContainsKey(handle))
					{
						ILobbyLeftListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000A65 RID: 2661
		public abstract void OnLobbyLeft(GalaxyID lobbyID, ILobbyLeftListener.LobbyLeaveReason leaveReason);

		// Token: 0x06000A66 RID: 2662 RVA: 0x00009954 File Offset: 0x00007B54
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnLobbyLeft", ILobbyLeftListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ILobbyLeftListener.SwigDelegateILobbyLeftListener_0(ILobbyLeftListener.SwigDirectorOnLobbyLeft);
			}
			GalaxyInstancePINVOKE.ILobbyLeftListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000A67 RID: 2663 RVA: 0x00009990 File Offset: 0x00007B90
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ILobbyLeftListener));
		}

		// Token: 0x06000A68 RID: 2664 RVA: 0x000099C6 File Offset: 0x00007BC6
		[MonoPInvokeCallback(typeof(ILobbyLeftListener.SwigDelegateILobbyLeftListener_0))]
		private static void SwigDirectorOnLobbyLeft(IntPtr cPtr, IntPtr lobbyID, int leaveReason)
		{
			if (ILobbyLeftListener.listeners.ContainsKey(cPtr))
			{
				ILobbyLeftListener.listeners[cPtr].OnLobbyLeft(new GalaxyID(new GalaxyID(lobbyID, false).ToUint64()), (ILobbyLeftListener.LobbyLeaveReason)leaveReason);
			}
		}

		// Token: 0x040001CA RID: 458
		private static Dictionary<IntPtr, ILobbyLeftListener> listeners = new Dictionary<IntPtr, ILobbyLeftListener>();

		// Token: 0x040001CB RID: 459
		private HandleRef swigCPtr;

		// Token: 0x040001CC RID: 460
		private ILobbyLeftListener.SwigDelegateILobbyLeftListener_0 swigDelegate0;

		// Token: 0x040001CD RID: 461
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(GalaxyID),
			typeof(ILobbyLeftListener.LobbyLeaveReason)
		};

		// Token: 0x02000111 RID: 273
		// (Invoke) Token: 0x06000A6B RID: 2667
		public delegate void SwigDelegateILobbyLeftListener_0(IntPtr cPtr, IntPtr lobbyID, int leaveReason);

		// Token: 0x02000112 RID: 274
		public enum LobbyLeaveReason
		{
			// Token: 0x040001CF RID: 463
			LOBBY_LEAVE_REASON_UNDEFINED,
			// Token: 0x040001D0 RID: 464
			LOBBY_LEAVE_REASON_USER_LEFT,
			// Token: 0x040001D1 RID: 465
			LOBBY_LEAVE_REASON_LOBBY_CLOSED,
			// Token: 0x040001D2 RID: 466
			LOBBY_LEAVE_REASON_CONNECTION_LOST
		}
	}
}
