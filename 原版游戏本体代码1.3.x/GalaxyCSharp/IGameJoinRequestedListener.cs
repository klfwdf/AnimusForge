using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000EB RID: 235
	public abstract class IGameJoinRequestedListener : GalaxyTypeAwareListenerGameJoinRequested
	{
		// Token: 0x06000994 RID: 2452 RVA: 0x0000FE78 File Offset: 0x0000E078
		internal IGameJoinRequestedListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IGameJoinRequestedListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IGameJoinRequestedListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000995 RID: 2453 RVA: 0x0000FEA0 File Offset: 0x0000E0A0
		public IGameJoinRequestedListener()
			: this(GalaxyInstancePINVOKE.new_IGameJoinRequestedListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000996 RID: 2454 RVA: 0x0000FEC4 File Offset: 0x0000E0C4
		internal static HandleRef getCPtr(IGameJoinRequestedListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000997 RID: 2455 RVA: 0x0000FEE4 File Offset: 0x0000E0E4
		~IGameJoinRequestedListener()
		{
			this.Dispose();
		}

		// Token: 0x06000998 RID: 2456 RVA: 0x0000FF14 File Offset: 0x0000E114
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IGameJoinRequestedListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IGameJoinRequestedListener.listeners.ContainsKey(handle))
					{
						IGameJoinRequestedListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000999 RID: 2457
		public abstract void OnGameJoinRequested(GalaxyID userID, string connectionString);

		// Token: 0x0600099A RID: 2458 RVA: 0x0000FFC4 File Offset: 0x0000E1C4
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnGameJoinRequested", IGameJoinRequestedListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IGameJoinRequestedListener.SwigDelegateIGameJoinRequestedListener_0(IGameJoinRequestedListener.SwigDirectorOnGameJoinRequested);
			}
			GalaxyInstancePINVOKE.IGameJoinRequestedListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x0600099B RID: 2459 RVA: 0x00010000 File Offset: 0x0000E200
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IGameJoinRequestedListener));
		}

		// Token: 0x0600099C RID: 2460 RVA: 0x00010036 File Offset: 0x0000E236
		[MonoPInvokeCallback(typeof(IGameJoinRequestedListener.SwigDelegateIGameJoinRequestedListener_0))]
		private static void SwigDirectorOnGameJoinRequested(IntPtr cPtr, IntPtr userID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string connectionString)
		{
			if (IGameJoinRequestedListener.listeners.ContainsKey(cPtr))
			{
				IGameJoinRequestedListener.listeners[cPtr].OnGameJoinRequested(new GalaxyID(new GalaxyID(userID, false).ToUint64()), connectionString);
			}
		}

		// Token: 0x04000178 RID: 376
		private static Dictionary<IntPtr, IGameJoinRequestedListener> listeners = new Dictionary<IntPtr, IGameJoinRequestedListener>();

		// Token: 0x04000179 RID: 377
		private HandleRef swigCPtr;

		// Token: 0x0400017A RID: 378
		private IGameJoinRequestedListener.SwigDelegateIGameJoinRequestedListener_0 swigDelegate0;

		// Token: 0x0400017B RID: 379
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(GalaxyID),
			typeof(string)
		};

		// Token: 0x020000EC RID: 236
		// (Invoke) Token: 0x0600099F RID: 2463
		public delegate void SwigDelegateIGameJoinRequestedListener_0(IntPtr cPtr, IntPtr userID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string connectionString);
	}
}
