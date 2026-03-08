using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000F5 RID: 245
	public abstract class ILeaderboardRetrieveListener : GalaxyTypeAwareListenerLeaderboardRetrieve
	{
		// Token: 0x060009CC RID: 2508 RVA: 0x000106C4 File Offset: 0x0000E8C4
		internal ILeaderboardRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ILeaderboardRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ILeaderboardRetrieveListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060009CD RID: 2509 RVA: 0x000106EC File Offset: 0x0000E8EC
		public ILeaderboardRetrieveListener()
			: this(GalaxyInstancePINVOKE.new_ILeaderboardRetrieveListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x060009CE RID: 2510 RVA: 0x00010710 File Offset: 0x0000E910
		internal static HandleRef getCPtr(ILeaderboardRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060009CF RID: 2511 RVA: 0x00010730 File Offset: 0x0000E930
		~ILeaderboardRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x060009D0 RID: 2512 RVA: 0x00010760 File Offset: 0x0000E960
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ILeaderboardRetrieveListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ILeaderboardRetrieveListener.listeners.ContainsKey(handle))
					{
						ILeaderboardRetrieveListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060009D1 RID: 2513
		public abstract void OnLeaderboardRetrieveSuccess(string name);

		// Token: 0x060009D2 RID: 2514
		public abstract void OnLeaderboardRetrieveFailure(string name, ILeaderboardRetrieveListener.FailureReason failureReason);

		// Token: 0x060009D3 RID: 2515 RVA: 0x00010810 File Offset: 0x0000EA10
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnLeaderboardRetrieveSuccess", ILeaderboardRetrieveListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ILeaderboardRetrieveListener.SwigDelegateILeaderboardRetrieveListener_0(ILeaderboardRetrieveListener.SwigDirectorOnLeaderboardRetrieveSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnLeaderboardRetrieveFailure", ILeaderboardRetrieveListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new ILeaderboardRetrieveListener.SwigDelegateILeaderboardRetrieveListener_1(ILeaderboardRetrieveListener.SwigDirectorOnLeaderboardRetrieveFailure);
			}
			GalaxyInstancePINVOKE.ILeaderboardRetrieveListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x060009D4 RID: 2516 RVA: 0x00010884 File Offset: 0x0000EA84
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ILeaderboardRetrieveListener));
		}

		// Token: 0x060009D5 RID: 2517 RVA: 0x000108BA File Offset: 0x0000EABA
		[MonoPInvokeCallback(typeof(ILeaderboardRetrieveListener.SwigDelegateILeaderboardRetrieveListener_0))]
		private static void SwigDirectorOnLeaderboardRetrieveSuccess(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string name)
		{
			if (ILeaderboardRetrieveListener.listeners.ContainsKey(cPtr))
			{
				ILeaderboardRetrieveListener.listeners[cPtr].OnLeaderboardRetrieveSuccess(name);
			}
		}

		// Token: 0x060009D6 RID: 2518 RVA: 0x000108DD File Offset: 0x0000EADD
		[MonoPInvokeCallback(typeof(ILeaderboardRetrieveListener.SwigDelegateILeaderboardRetrieveListener_1))]
		private static void SwigDirectorOnLeaderboardRetrieveFailure(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string name, int failureReason)
		{
			if (ILeaderboardRetrieveListener.listeners.ContainsKey(cPtr))
			{
				ILeaderboardRetrieveListener.listeners[cPtr].OnLeaderboardRetrieveFailure(name, (ILeaderboardRetrieveListener.FailureReason)failureReason);
			}
		}

		// Token: 0x0400018C RID: 396
		private static Dictionary<IntPtr, ILeaderboardRetrieveListener> listeners = new Dictionary<IntPtr, ILeaderboardRetrieveListener>();

		// Token: 0x0400018D RID: 397
		private HandleRef swigCPtr;

		// Token: 0x0400018E RID: 398
		private ILeaderboardRetrieveListener.SwigDelegateILeaderboardRetrieveListener_0 swigDelegate0;

		// Token: 0x0400018F RID: 399
		private ILeaderboardRetrieveListener.SwigDelegateILeaderboardRetrieveListener_1 swigDelegate1;

		// Token: 0x04000190 RID: 400
		private static Type[] swigMethodTypes0 = new Type[] { typeof(string) };

		// Token: 0x04000191 RID: 401
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(string),
			typeof(ILeaderboardRetrieveListener.FailureReason)
		};

		// Token: 0x020000F6 RID: 246
		// (Invoke) Token: 0x060009D9 RID: 2521
		public delegate void SwigDelegateILeaderboardRetrieveListener_0(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string name);

		// Token: 0x020000F7 RID: 247
		// (Invoke) Token: 0x060009DD RID: 2525
		public delegate void SwigDelegateILeaderboardRetrieveListener_1(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string name, int failureReason);

		// Token: 0x020000F8 RID: 248
		public enum FailureReason
		{
			// Token: 0x04000193 RID: 403
			FAILURE_REASON_UNDEFINED,
			// Token: 0x04000194 RID: 404
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
