using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000FD RID: 253
	public abstract class ILeaderboardsRetrieveListener : GalaxyTypeAwareListenerLeaderboardsRetrieve
	{
		// Token: 0x060009F4 RID: 2548 RVA: 0x00010E7C File Offset: 0x0000F07C
		internal ILeaderboardsRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ILeaderboardsRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ILeaderboardsRetrieveListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060009F5 RID: 2549 RVA: 0x00010EA4 File Offset: 0x0000F0A4
		public ILeaderboardsRetrieveListener()
			: this(GalaxyInstancePINVOKE.new_ILeaderboardsRetrieveListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x060009F6 RID: 2550 RVA: 0x00010EC8 File Offset: 0x0000F0C8
		internal static HandleRef getCPtr(ILeaderboardsRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060009F7 RID: 2551 RVA: 0x00010EE8 File Offset: 0x0000F0E8
		~ILeaderboardsRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x060009F8 RID: 2552 RVA: 0x00010F18 File Offset: 0x0000F118
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ILeaderboardsRetrieveListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ILeaderboardsRetrieveListener.listeners.ContainsKey(handle))
					{
						ILeaderboardsRetrieveListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060009F9 RID: 2553
		public abstract void OnLeaderboardsRetrieveSuccess();

		// Token: 0x060009FA RID: 2554
		public abstract void OnLeaderboardsRetrieveFailure(ILeaderboardsRetrieveListener.FailureReason failureReason);

		// Token: 0x060009FB RID: 2555 RVA: 0x00010FC8 File Offset: 0x0000F1C8
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnLeaderboardsRetrieveSuccess", ILeaderboardsRetrieveListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ILeaderboardsRetrieveListener.SwigDelegateILeaderboardsRetrieveListener_0(ILeaderboardsRetrieveListener.SwigDirectorOnLeaderboardsRetrieveSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnLeaderboardsRetrieveFailure", ILeaderboardsRetrieveListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new ILeaderboardsRetrieveListener.SwigDelegateILeaderboardsRetrieveListener_1(ILeaderboardsRetrieveListener.SwigDirectorOnLeaderboardsRetrieveFailure);
			}
			GalaxyInstancePINVOKE.ILeaderboardsRetrieveListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x060009FC RID: 2556 RVA: 0x0001103C File Offset: 0x0000F23C
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ILeaderboardsRetrieveListener));
		}

		// Token: 0x060009FD RID: 2557 RVA: 0x00011072 File Offset: 0x0000F272
		[MonoPInvokeCallback(typeof(ILeaderboardsRetrieveListener.SwigDelegateILeaderboardsRetrieveListener_0))]
		private static void SwigDirectorOnLeaderboardsRetrieveSuccess(IntPtr cPtr)
		{
			if (ILeaderboardsRetrieveListener.listeners.ContainsKey(cPtr))
			{
				ILeaderboardsRetrieveListener.listeners[cPtr].OnLeaderboardsRetrieveSuccess();
			}
		}

		// Token: 0x060009FE RID: 2558 RVA: 0x00011094 File Offset: 0x0000F294
		[MonoPInvokeCallback(typeof(ILeaderboardsRetrieveListener.SwigDelegateILeaderboardsRetrieveListener_1))]
		private static void SwigDirectorOnLeaderboardsRetrieveFailure(IntPtr cPtr, int failureReason)
		{
			if (ILeaderboardsRetrieveListener.listeners.ContainsKey(cPtr))
			{
				ILeaderboardsRetrieveListener.listeners[cPtr].OnLeaderboardsRetrieveFailure((ILeaderboardsRetrieveListener.FailureReason)failureReason);
			}
		}

		// Token: 0x0400019F RID: 415
		private static Dictionary<IntPtr, ILeaderboardsRetrieveListener> listeners = new Dictionary<IntPtr, ILeaderboardsRetrieveListener>();

		// Token: 0x040001A0 RID: 416
		private HandleRef swigCPtr;

		// Token: 0x040001A1 RID: 417
		private ILeaderboardsRetrieveListener.SwigDelegateILeaderboardsRetrieveListener_0 swigDelegate0;

		// Token: 0x040001A2 RID: 418
		private ILeaderboardsRetrieveListener.SwigDelegateILeaderboardsRetrieveListener_1 swigDelegate1;

		// Token: 0x040001A3 RID: 419
		private static Type[] swigMethodTypes0 = new Type[0];

		// Token: 0x040001A4 RID: 420
		private static Type[] swigMethodTypes1 = new Type[] { typeof(ILeaderboardsRetrieveListener.FailureReason) };

		// Token: 0x020000FE RID: 254
		// (Invoke) Token: 0x06000A01 RID: 2561
		public delegate void SwigDelegateILeaderboardsRetrieveListener_0(IntPtr cPtr);

		// Token: 0x020000FF RID: 255
		// (Invoke) Token: 0x06000A05 RID: 2565
		public delegate void SwigDelegateILeaderboardsRetrieveListener_1(IntPtr cPtr, int failureReason);

		// Token: 0x02000100 RID: 256
		public enum FailureReason
		{
			// Token: 0x040001A6 RID: 422
			FAILURE_REASON_UNDEFINED,
			// Token: 0x040001A7 RID: 423
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
