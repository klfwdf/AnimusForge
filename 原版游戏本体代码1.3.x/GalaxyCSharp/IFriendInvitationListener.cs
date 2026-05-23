using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000D5 RID: 213
	public abstract class IFriendInvitationListener : GalaxyTypeAwareListenerFriendInvitation
	{
		// Token: 0x060008E5 RID: 2277 RVA: 0x0000E798 File Offset: 0x0000C998
		internal IFriendInvitationListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IFriendInvitationListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IFriendInvitationListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060008E6 RID: 2278 RVA: 0x0000E7C0 File Offset: 0x0000C9C0
		public IFriendInvitationListener()
			: this(GalaxyInstancePINVOKE.new_IFriendInvitationListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x060008E7 RID: 2279 RVA: 0x0000E7E4 File Offset: 0x0000C9E4
		internal static HandleRef getCPtr(IFriendInvitationListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060008E8 RID: 2280 RVA: 0x0000E804 File Offset: 0x0000CA04
		~IFriendInvitationListener()
		{
			this.Dispose();
		}

		// Token: 0x060008E9 RID: 2281 RVA: 0x0000E834 File Offset: 0x0000CA34
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IFriendInvitationListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IFriendInvitationListener.listeners.ContainsKey(handle))
					{
						IFriendInvitationListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060008EA RID: 2282
		public abstract void OnFriendInvitationReceived(GalaxyID userID, uint sendTime);

		// Token: 0x060008EB RID: 2283 RVA: 0x0000E8E4 File Offset: 0x0000CAE4
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnFriendInvitationReceived", IFriendInvitationListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IFriendInvitationListener.SwigDelegateIFriendInvitationListener_0(IFriendInvitationListener.SwigDirectorOnFriendInvitationReceived);
			}
			GalaxyInstancePINVOKE.IFriendInvitationListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x060008EC RID: 2284 RVA: 0x0000E920 File Offset: 0x0000CB20
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IFriendInvitationListener));
		}

		// Token: 0x060008ED RID: 2285 RVA: 0x0000E956 File Offset: 0x0000CB56
		[MonoPInvokeCallback(typeof(IFriendInvitationListener.SwigDelegateIFriendInvitationListener_0))]
		private static void SwigDirectorOnFriendInvitationReceived(IntPtr cPtr, IntPtr userID, uint sendTime)
		{
			if (IFriendInvitationListener.listeners.ContainsKey(cPtr))
			{
				IFriendInvitationListener.listeners[cPtr].OnFriendInvitationReceived(new GalaxyID(new GalaxyID(userID, false).ToUint64()), sendTime);
			}
		}

		// Token: 0x04000141 RID: 321
		private static Dictionary<IntPtr, IFriendInvitationListener> listeners = new Dictionary<IntPtr, IFriendInvitationListener>();

		// Token: 0x04000142 RID: 322
		private HandleRef swigCPtr;

		// Token: 0x04000143 RID: 323
		private IFriendInvitationListener.SwigDelegateIFriendInvitationListener_0 swigDelegate0;

		// Token: 0x04000144 RID: 324
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(GalaxyID),
			typeof(uint)
		};

		// Token: 0x020000D6 RID: 214
		// (Invoke) Token: 0x060008F0 RID: 2288
		public delegate void SwigDelegateIFriendInvitationListener_0(IntPtr cPtr, IntPtr userID, uint sendTime);
	}
}
