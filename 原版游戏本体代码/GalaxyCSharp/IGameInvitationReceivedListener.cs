using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000E9 RID: 233
	public abstract class IGameInvitationReceivedListener : GalaxyTypeAwareListenerGameInvitationReceived
	{
		// Token: 0x06000986 RID: 2438 RVA: 0x0000FAE8 File Offset: 0x0000DCE8
		internal IGameInvitationReceivedListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IGameInvitationReceivedListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IGameInvitationReceivedListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000987 RID: 2439 RVA: 0x0000FB10 File Offset: 0x0000DD10
		public IGameInvitationReceivedListener()
			: this(GalaxyInstancePINVOKE.new_IGameInvitationReceivedListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000988 RID: 2440 RVA: 0x0000FB34 File Offset: 0x0000DD34
		internal static HandleRef getCPtr(IGameInvitationReceivedListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000989 RID: 2441 RVA: 0x0000FB54 File Offset: 0x0000DD54
		~IGameInvitationReceivedListener()
		{
			this.Dispose();
		}

		// Token: 0x0600098A RID: 2442 RVA: 0x0000FB84 File Offset: 0x0000DD84
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IGameInvitationReceivedListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IGameInvitationReceivedListener.listeners.ContainsKey(handle))
					{
						IGameInvitationReceivedListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0600098B RID: 2443
		public abstract void OnGameInvitationReceived(GalaxyID userID, string connectionString);

		// Token: 0x0600098C RID: 2444 RVA: 0x0000FC34 File Offset: 0x0000DE34
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnGameInvitationReceived", IGameInvitationReceivedListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IGameInvitationReceivedListener.SwigDelegateIGameInvitationReceivedListener_0(IGameInvitationReceivedListener.SwigDirectorOnGameInvitationReceived);
			}
			GalaxyInstancePINVOKE.IGameInvitationReceivedListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x0600098D RID: 2445 RVA: 0x0000FC70 File Offset: 0x0000DE70
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IGameInvitationReceivedListener));
		}

		// Token: 0x0600098E RID: 2446 RVA: 0x0000FCA6 File Offset: 0x0000DEA6
		[MonoPInvokeCallback(typeof(IGameInvitationReceivedListener.SwigDelegateIGameInvitationReceivedListener_0))]
		private static void SwigDirectorOnGameInvitationReceived(IntPtr cPtr, IntPtr userID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string connectionString)
		{
			if (IGameInvitationReceivedListener.listeners.ContainsKey(cPtr))
			{
				IGameInvitationReceivedListener.listeners[cPtr].OnGameInvitationReceived(new GalaxyID(new GalaxyID(userID, false).ToUint64()), connectionString);
			}
		}

		// Token: 0x04000174 RID: 372
		private static Dictionary<IntPtr, IGameInvitationReceivedListener> listeners = new Dictionary<IntPtr, IGameInvitationReceivedListener>();

		// Token: 0x04000175 RID: 373
		private HandleRef swigCPtr;

		// Token: 0x04000176 RID: 374
		private IGameInvitationReceivedListener.SwigDelegateIGameInvitationReceivedListener_0 swigDelegate0;

		// Token: 0x04000177 RID: 375
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(GalaxyID),
			typeof(string)
		};

		// Token: 0x020000EA RID: 234
		// (Invoke) Token: 0x06000991 RID: 2449
		public delegate void SwigDelegateIGameInvitationReceivedListener_0(IntPtr cPtr, IntPtr userID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string connectionString);
	}
}
