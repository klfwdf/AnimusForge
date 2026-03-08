using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000CE RID: 206
	public abstract class IFriendAddListener : GalaxyTypeAwareListenerFriendAdd
	{
		// Token: 0x060008C3 RID: 2243 RVA: 0x0000E068 File Offset: 0x0000C268
		internal IFriendAddListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IFriendAddListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IFriendAddListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060008C4 RID: 2244 RVA: 0x0000E090 File Offset: 0x0000C290
		public IFriendAddListener()
			: this(GalaxyInstancePINVOKE.new_IFriendAddListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x060008C5 RID: 2245 RVA: 0x0000E0B4 File Offset: 0x0000C2B4
		internal static HandleRef getCPtr(IFriendAddListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060008C6 RID: 2246 RVA: 0x0000E0D4 File Offset: 0x0000C2D4
		~IFriendAddListener()
		{
			this.Dispose();
		}

		// Token: 0x060008C7 RID: 2247 RVA: 0x0000E104 File Offset: 0x0000C304
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IFriendAddListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IFriendAddListener.listeners.ContainsKey(handle))
					{
						IFriendAddListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060008C8 RID: 2248
		public abstract void OnFriendAdded(GalaxyID userID, IFriendAddListener.InvitationDirection invitationDirection);

		// Token: 0x060008C9 RID: 2249 RVA: 0x0000E1B4 File Offset: 0x0000C3B4
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnFriendAdded", IFriendAddListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IFriendAddListener.SwigDelegateIFriendAddListener_0(IFriendAddListener.SwigDirectorOnFriendAdded);
			}
			GalaxyInstancePINVOKE.IFriendAddListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x060008CA RID: 2250 RVA: 0x0000E1F0 File Offset: 0x0000C3F0
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IFriendAddListener));
		}

		// Token: 0x060008CB RID: 2251 RVA: 0x0000E226 File Offset: 0x0000C426
		[MonoPInvokeCallback(typeof(IFriendAddListener.SwigDelegateIFriendAddListener_0))]
		private static void SwigDirectorOnFriendAdded(IntPtr cPtr, IntPtr userID, int invitationDirection)
		{
			if (IFriendAddListener.listeners.ContainsKey(cPtr))
			{
				IFriendAddListener.listeners[cPtr].OnFriendAdded(new GalaxyID(new GalaxyID(userID, false).ToUint64()), (IFriendAddListener.InvitationDirection)invitationDirection);
			}
		}

		// Token: 0x04000131 RID: 305
		private static Dictionary<IntPtr, IFriendAddListener> listeners = new Dictionary<IntPtr, IFriendAddListener>();

		// Token: 0x04000132 RID: 306
		private HandleRef swigCPtr;

		// Token: 0x04000133 RID: 307
		private IFriendAddListener.SwigDelegateIFriendAddListener_0 swigDelegate0;

		// Token: 0x04000134 RID: 308
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(GalaxyID),
			typeof(IFriendAddListener.InvitationDirection)
		};

		// Token: 0x020000CF RID: 207
		// (Invoke) Token: 0x060008CE RID: 2254
		public delegate void SwigDelegateIFriendAddListener_0(IntPtr cPtr, IntPtr userID, int invitationDirection);

		// Token: 0x020000D0 RID: 208
		public enum InvitationDirection
		{
			// Token: 0x04000136 RID: 310
			INVITATION_DIRECTION_INCOMING,
			// Token: 0x04000137 RID: 311
			INVITATION_DIRECTION_OUTGOING
		}
	}
}
